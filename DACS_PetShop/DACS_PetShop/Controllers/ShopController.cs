using DACS_PetShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace DACS_PetShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Shop
        public async Task<IActionResult> Index(
            int? categoryId,
            int? brandId,
            int? sizeId,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1,
            string sort = "name-asc")
        {
            const int pageSize = 12; // Số sản phẩm mỗi trang
            ViewBag.Categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.Brands = await _context.Brands.AsNoTracking().ToListAsync();
            ViewBag.Sizes = await _context.Sizes.AsNoTracking().ToListAsync();

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages.Where(img => img.IsMainImage)) // Chỉ lấy ảnh chính
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .AsNoTracking()
                .AsQueryable();

            // Bộ lọc
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
            }

            if (sizeId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductSizes.Any(ps => ps.SizeId == sizeId.Value));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductSizes.Any(ps => ps.Price >= minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductSizes.Any(ps => ps.Price <= maxPrice.Value));
            }

            // Sắp xếp
            switch (sort)
            {
                case "name-asc":
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
                case "name-desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Name);
                    break;
                case "price-asc":
                    productsQuery = productsQuery.OrderBy(p => p.ProductSizes.Min(ps => ps.Price));
                    break;
                case "price-desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.ProductSizes.Max(ps => ps.Price));
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
            }

            // Phân trang
            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedSizeId = sizeId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            ViewBag.CurrentSort = sort;

            return View(products);
        }

        // GET: /Shop/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Include(p => p.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.ProductSizes == null || !product.ProductSizes.Any())
            {
                TempData["Warning"] = "Sản phẩm hiện không có kích thước nào để chọn.";
            }

            return View(product);
        }

        // POST: /Shop/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            if (!ModelState.IsValid || rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var review = new Review
            {
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                UserId = User.Identity.Name, // Lấy từ người dùng đăng nhập
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đánh giá của bạn đã được gửi thành công!",
                review = new
                {
                    rating,
                    comment,
                    userId = review.UserId ?? "Ẩn danh",
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy")
                }
            });
        }
    }
}