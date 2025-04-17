using DACS_PetShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        decimal? maxPrice)
    {
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Brands = await _context.Brands.ToListAsync();
        ViewBag.Sizes = await _context.Sizes.ToListAsync();

        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductSizes)
            .ThenInclude(ps => ps.Size)
            .AsQueryable();

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

        var products = await productsQuery.ToListAsync();

        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SelectedBrandId = brandId;
        ViewBag.SelectedSizeId = sizeId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

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
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: /Shop/AddReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int productId, int rating, string comment, string userId)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Details", new { id = productId });
        }

        // Kiểm tra sản phẩm tồn tại
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        // Tạo đánh giá mới
        var review = new Review
        {
            ProductId = productId,
            Rating = rating,
            Comment = comment,
            UserId = string.IsNullOrEmpty(userId) ? null : userId,
            CreatedAt = DateTime.UtcNow
        };

        // Lưu vào cơ sở dữ liệu
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đánh giá của bạn đã được gửi thành công!";
        return RedirectToAction("Details", new { id = productId });
    }
}