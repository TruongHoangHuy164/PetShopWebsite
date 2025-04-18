using Microsoft.AspNetCore.Mvc;
using DACS_PetShop.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace DACS_PetShop.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Order/Checkout
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.Identity.Name;
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.ProductSizes)
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Size)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = "", // Sẽ được cập nhật qua form
                PhoneNumber = "", // Sẽ được cập nhật qua form
                TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.ProductSizes.First(ps => ps.SizeId == ci.SizeId).Price),
                OrderDetails = cart.CartItems.Select(ci => new OrderDetail
                {
                    ProductId = ci.ProductId,
                    SizeId = ci.Size.SizeId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.ProductSizes.First(ps => ps.SizeId == ci.SizeId).Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return View(order);
        }

        // POST: /Order/UpdateShippingInfo
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingInfo(int orderId, string shippingAddress, string phoneNumber)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != User.Identity.Name)
            {
                return NotFound();
            }

            order.ShippingAddress = shippingAddress;
            order.PhoneNumber = phoneNumber;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin giao hàng.";
            return RedirectToAction("Checkout", new { orderId });
        }

        // POST: /Order/ProcessPayment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId, string paymentMethod)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == User.Identity.Name);

            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra tồn kho
            foreach (var detail in order.OrderDetails)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.SizeId == detail.SizeId);
                if (productSize == null || productSize.StockQuantity < detail.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {detail.Product.Name} (kích thước {detail.Size.Name}) không đủ tồn kho.";
                    return RedirectToAction("Checkout", new { orderId });
                }
            }

            // Tạo Payment
            var payment = new Payment
            {
                OrderId = orderId,
                Amount = order.TotalAmount,
                PaymentMethod = paymentMethod,
                PaymentStatus = "Pending",
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (paymentMethod == "COD")
            {
                payment.PaymentStatus = "Completed";
                order.Status = "Processing";
                // Giảm tồn kho
                foreach (var detail in order.OrderDetails)
                {
                    var productSize = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.SizeId == detail.SizeId);
                    productSize.StockQuantity -= detail.Quantity;
                }
                // Xóa giỏ hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == User.Identity.Name);
                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    _context.Carts.Remove(cart);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đơn hàng đã được đặt thành công!";
                return RedirectToAction("OrderConfirmation", new { orderId });
            }
            else if (paymentMethod == "MoMo")
            {
                return await ProcessMoMoPayment(order, payment);
            }
            else if (paymentMethod == "VNPay")
            {
                return await ProcessVNPayPayment(order, payment);
            }

            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Checkout", new { orderId });
        }

        // Thanh toán MoMo
        private async Task<IActionResult> ProcessMoMoPayment(Order order, Payment payment)
        {
            // Thông tin cấu hình MoMo (thay bằng thông tin thật từ MoMo)
            string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
            string partnerCode = "YOUR_MOMO_PARTNER_CODE";
            string accessKey = "YOUR_MOMO_ACCESS_KEY";
            string secretKey = "YOUR_MOMO_SECRET_KEY";
            string returnUrl = Url.Action("MoMoCallback", "Order", null, Request.Scheme);
            string notifyUrl = Url.Action("MoMoNotify", "Order", null, Request.Scheme);

            // Tạo yêu cầu thanh toán
            string orderInfo = $"Thanh toán đơn hàng #{order.OrderId}";
            string requestId = Guid.NewGuid().ToString();
            string orderId = order.OrderId.ToString();
            string amount = ((long)order.TotalAmount).ToString();

            // Tạo chữ ký
            string rawData = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            string signature = ComputeHmacSha256(rawData, secretKey);

            var requestData = new
            {
                partnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                extraData = "",
                requestType = "captureWallet",
                signature,
                lang = "vi"
            };

            // Gửi yêu cầu đến MoMo
            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync(endpoint, requestData);
            var result = await response.Content.ReadAsStringAsync();

            // Phân tích phản hồi
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(result);
            if (json.ContainsKey("payUrl"))
            {
                return Redirect(json["payUrl"]);
            }

            TempData["Error"] = "Không thể khởi tạo thanh toán MoMo.";
            return RedirectToAction("Checkout", new { orderId = order.OrderId });
        }

        // Thanh toán VNPay
        private async Task<IActionResult> ProcessVNPayPayment(Order order, Payment payment)
        {
            // Thông tin cấu hình VNPay (thay bằng thông tin thật từ VNPay)
            string vnp_TmnCode = "YOUR_VNPAY_TMN_CODE";
            string vnp_HashSecret = "YOUR_VNPAY_HASH_SECRET";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_ReturnUrl = Url.Action("VNPayCallback", "Order", null, Request.Scheme);

            // Tạo yêu cầu thanh toán
            string vnp_TxnRef = order.OrderId.ToString();
            string vnp_Amount = ((long)(order.TotalAmount * 100)).ToString();
            string vnp_OrderInfo = $"Thanh toán đơn hàng #{order.OrderId}";
            string vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string vnp_IpAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Tạo danh sách tham số
            var vnp_Params = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnp_TmnCode },
                { "vnp_Amount", vnp_Amount },
                { "vnp_CreateDate", vnp_CreateDate },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", vnp_IpAddr },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", vnp_OrderInfo },
                { "vnp_OrderType", "billpayment" },
                { "vnp_ReturnUrl", vnp_ReturnUrl },
                { "vnp_TxnRef", vnp_TxnRef }
            };

            // Tạo chữ ký
            string queryString = string.Join("&", vnp_Params.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            string signData = queryString;
            string vnp_SecureHash = ComputeHmacSha512(signData, vnp_HashSecret);
            vnp_Params["vnp_SecureHash"] = vnp_SecureHash;

            // Tạo URL thanh toán
            string paymentUrl = vnp_Url + "?" + string.Join("&", vnp_Params.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

            return Redirect(paymentUrl);
        }

        // Callback từ MoMo
        [AllowAnonymous]
        public async Task<IActionResult> MoMoCallback()
        {
            var response = Request.Query;
            string orderId = response["orderId"];
            string resultCode = response["resultCode"];
            string transactionId = response["transId"];

            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.OrderId.ToString() == orderId);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (resultCode == "0")
            {
                payment.PaymentStatus = "Completed";
                payment.TransactionId = transactionId;
                payment.Order.Status = "Processing";
                // Giảm tồn kho
                foreach (var detail in payment.Order.OrderDetails)
                {
                    var productSize = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.SizeId == detail.SizeId);
                    productSize.StockQuantity -= detail.Quantity;
                }
                // Xóa giỏ hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == payment.Order.UserId);
                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    _context.Carts.Remove(cart);
                }
            }
            else
            {
                payment.PaymentStatus = "Failed";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = resultCode == "0" ? "Thanh toán thành công!" : "Thanh toán thất bại.";
            return RedirectToAction("OrderConfirmation", new { orderId = payment.OrderId });
        }

        // Callback từ VNPay
        [AllowAnonymous]
        public async Task<IActionResult> VNPayCallback()
        {
            var response = Request.Query;
            string vnp_TxnRef = response["vnp_TxnRef"];
            string vnp_ResponseCode = response["vnp_ResponseCode"];
            string vnp_TransactionNo = response["vnp_TransactionNo"];
            string vnp_SecureHash = response["vnp_SecureHash"];

            // Xác minh chữ ký
            string vnp_HashSecret = "YOUR_VNPAY_HASH_SECRET";
            var vnp_Params = response.ToDictionary(k => k.Key, v => v.Value.ToString());
            vnp_Params.Remove("vnp_SecureHash");
            vnp_Params.Remove("vnp_SecureHashType");
            var signData = string.Join("&", vnp_Params.OrderBy(k => k.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            string computedHash = ComputeHmacSha512(signData, vnp_HashSecret);

            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.OrderId.ToString() == vnp_TxnRef);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (vnp_SecureHash == computedHash && vnp_ResponseCode == "00")
            {
                payment.PaymentStatus = "Completed";
                payment.TransactionId = vnp_TransactionNo;
                payment.Order.Status = "Processing";
                // Giảm tồn kho
                foreach (var detail in payment.Order.OrderDetails)
                {
                    var productSize = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.SizeId == detail.SizeId);
                    productSize.StockQuantity -= detail.Quantity;
                }
                // Xóa giỏ hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == payment.Order.UserId);
                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    _context.Carts.Remove(cart);
                }
            }
            else
            {
                payment.PaymentStatus = "Failed";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = vnp_ResponseCode == "00" ? "Thanh toán thành công!" : "Thanh toán thất bại.";
            return RedirectToAction("OrderConfirmation", new { orderId = payment.OrderId });
        }

        // GET: /Order/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == User.Identity.Name);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Hàm tính HMAC SHA256 cho MoMo
        private string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // Hàm tính HMAC SHA512 cho VNPay
        private string ComputeHmacSha512(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA512(keyBytes);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}