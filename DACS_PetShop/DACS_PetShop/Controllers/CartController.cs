using Microsoft.AspNetCore.Mvc;
using DACS_PetShop.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace DACS_PetShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: /Cart/Index
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.ProductSizes)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Size)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            await SyncSessionCart(userId, cart);
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int sizeId, int quantity)
        {
            // Kiểm tra sản phẩm và kích thước
            var productSize = await _context.ProductSizes
                .Include(ps => ps.Size)
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.SizeId == sizeId);

            if (productSize == null)
            {
                return Json(new { success = false, message = "Sản phẩm hoặc kích thước không tồn tại." });
            }

            // Kiểm tra số lượng tồn kho
            if (quantity <= 0 || quantity > productSize.StockQuantity)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ hoặc vượt quá tồn kho." });
            }

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                    _context.Carts.Add(cart);
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId && ci.SizeId == sizeId);
                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                    if (cartItem.Quantity > productSize.StockQuantity)
                    {
                        return Json(new { success = false, message = "Tổng số lượng vượt quá tồn kho." });
                    }
                }
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        SizeId = sizeId,
                        Quantity = quantity
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, cartCount = cart.CartItems.Sum(ci => ci.Quantity) });
            }
            else
            {
                var sessionCart = GetSessionCart();
                var sessionItem = sessionCart.FirstOrDefault(ci => ci.ProductId == productId && ci.SizeId == sizeId);
                if (sessionItem != null)
                {
                    sessionItem.Quantity += quantity;
                    if (sessionItem.Quantity > productSize.StockQuantity)
                    {
                        return Json(new { success = false, message = "Tổng số lượng vượt quá tồn kho." });
                    }
                }
                else
                {
                    sessionCart.Add(new CartItem
                    {
                        ProductId = productId,
                        SizeId = sizeId,
                        Quantity = quantity
                    });
                }

                SaveSessionCart(sessionCart);
                return Json(new { success = true, cartCount = sessionCart.Sum(ci => ci.Quantity) });
            }
        }

        // POST: /Cart/UpdateCartItem
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .ThenInclude(p => p.ProductSizes)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Mục không tồn tại trong giỏ hàng." });
            }

            var productSize = cartItem.Product.ProductSizes.FirstOrDefault(ps => ps.SizeId == cartItem.SizeId);
            if (productSize == null || quantity <= 0 || quantity > productSize.StockQuantity)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ hoặc kích thước không tồn tại." });
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.ProductSizes)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return Json(new
            {
                success = true,
                cartCount = cart.CartItems.Sum(ci => ci.Quantity),
                itemTotal = cartItem.Quantity * productSize.Price,
                cartTotal = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.ProductSizes.First(ps => ps.SizeId == ci.SizeId).Price)
            });
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Mục không tồn tại trong giỏ hàng." });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.ProductSizes)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return Json(new
            {
                success = true,
                cartCount = cart?.CartItems.Sum(ci => ci.Quantity) ?? 0,
                cartTotal = cart?.CartItems.Sum(ci => ci.Quantity * ci.Product.ProductSizes.First(ps => ps.SizeId == ci.SizeId).Price) ?? 0
            });
        }

        // GET: /Cart/GetCartCount
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                var sessionCart = GetSessionCart();
                return Json(new { cartCount = sessionCart.Sum(ci => ci.Quantity) });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var cartCount = cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;
            return Json(new { cartCount });
        }

        private List<CartItem> GetSessionCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return new List<CartItem>();
            }

            var cartJson = session.GetString("SessionCart");
            return string.IsNullOrEmpty(cartJson) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartJson);
        }

        private void SaveSessionCart(List<CartItem> cart)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString("SessionCart", JsonSerializer.Serialize(cart));
            }
        }

        private async Task SyncSessionCart(string userId, Cart cart)
        {
            var sessionCart = GetSessionCart();
            if (sessionCart.Any())
            {
                foreach (var sessionItem in sessionCart)
                {
                    var productSize = await _context.ProductSizes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ps => ps.ProductId == sessionItem.ProductId && ps.SizeId == sessionItem.SizeId);

                    if (productSize != null && sessionItem.Quantity <= productSize.StockQuantity)
                    {
                        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == sessionItem.ProductId && ci.SizeId == sessionItem.SizeId);
                        if (cartItem != null)
                        {
                            cartItem.Quantity += sessionItem.Quantity;
                            if (cartItem.Quantity > productSize.StockQuantity)
                            {
                                cartItem.Quantity = productSize.StockQuantity;
                            }
                        }
                        else
                        {
                            cart.CartItems.Add(new CartItem
                            {
                                ProductId = sessionItem.ProductId,
                                SizeId = sessionItem.SizeId,
                                Quantity = sessionItem.Quantity
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
                SaveSessionCart(new List<CartItem>());
            }
        }
    }
}