using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACS_PetShop.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DACS_PetShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products
        .Include(p => p.Brand)
        .Include(p => p.Category)
        .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size) // Bao gồm ProductSizes và Size
        .Include(p => p.ProductImages); // Bao gồm ProductImages
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size) // Bao gồm ProductSizes và Size
                .Include(p => p.ProductImages) // Bao gồm ProductImages
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "Name");
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name");
            ViewBag.Sizes = new SelectList(_context.Sizes, "SizeId", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile> images, List<int> sizeIds, List<int> stockQuantities, List<decimal> prices)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.UtcNow;
                _context.Add(product);
                await _context.SaveChangesAsync(); // cần để lấy ProductId

                // Xử lý ảnh
                int displayOrder = 1;
                bool isMainImageSet = false;

                foreach (var image in images)
                {
                    if (displayOrder > 3) break;  // Giới hạn số lượng ảnh

                    string imageUrl = await SaveImageAsync(image); // Lưu ảnh

                    var productImage = new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = imageUrl,
                        DisplayOrder = displayOrder,
                        IsMainImage = !isMainImageSet
                    };

                    if (!isMainImageSet) isMainImageSet = true;

                    _context.Add(productImage);
                    displayOrder++;
                }

                // Lưu thông tin Size, StockQuantity và Price cho từng kích thước
                for (int i = 0; i < sizeIds.Count; i++)
                {
                    var productSize = new ProductSize
                    {
                        ProductId = product.ProductId,
                        SizeId = sizeIds[i],
                        StockQuantity = stockQuantities[i],
                        Price = prices[i]
                    };
                    _context.Add(productSize);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // ✅ Load lại dropdown nếu form lỗi
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewBag.Sizes = new SelectList(_context.Sizes, "SizeId", "Name");
            return View(product);
        }
        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.ProductImages) // Tải ProductImages
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size) // Tải ProductSizes và Size
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var productSizes = await _context.ProductSizes
                .Where(ps => ps.ProductId == product.ProductId)
                .ToListAsync();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                ProductSizes = productSizes,
                ProductImages = product.ProductImages.ToList(),
                SizeIds = productSizes.Select(ps => ps.SizeId).ToList(),
                StockQuantities = productSizes.Select(ps => ps.StockQuantity).ToList(),
                Prices = productSizes.Select(ps => ps.Price).ToList(),
                ProductSizeIds = productSizes.Select(ps => ps.ProductSizeId).ToList()
            };

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewBag.Sizes = new MultiSelectList(_context.Sizes, "SizeId", "Name", viewModel.SizeIds);
            return View(viewModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model, List<IFormFile> images, int? MainImageId, int? MainImageIndex, int? DeleteImageId)
        {
            if (id != model.Product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật thông tin sản phẩm
                    var product = await _context.Products
                        .Include(p => p.ProductImages)
                        .Include(p => p.ProductSizes)
                        .FirstOrDefaultAsync(p => p.ProductId == model.Product.ProductId);

                    if (product == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật các thuộc tính sản phẩm
                    product.Name = model.Product.Name;
                    product.Description = model.Product.Description;
                    product.CategoryId = model.Product.CategoryId;
                    product.BrandId = model.Product.BrandId;
                    _context.Update(product);

                    // Xử lý xóa ảnh
                    if (DeleteImageId.HasValue)
                    {
                        var imageToDelete = product.ProductImages.FirstOrDefault(pi => pi.ImageId == DeleteImageId.Value);
                        if (imageToDelete != null)
                        {
                            _context.ProductImages.Remove(imageToDelete);
                            await _context.SaveChangesAsync();
                            // Tải lại View với dữ liệu mới
                            model.Product = product;
                            model.ProductImages = product.ProductImages.ToList();
                            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", model.Product.CategoryId);
                            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "Name", model.Product.BrandId);
                            ViewBag.Sizes = new MultiSelectList(_context.Sizes, "SizeId", "Name", model.SizeIds);
                            return View(model);
                        }
                    }

                    // Xử lý ProductSize
                    var existingProductSizes = product.ProductSizes.ToList();
                    var selectedSizeIds = model.SizeIds ?? new List<int>();

                    // Xóa các ProductSize không còn được chọn
                    var productSizesToRemove = existingProductSizes
                        .Where(ps => !selectedSizeIds.Contains(ps.SizeId))
                        .ToList();
                    _context.ProductSizes.RemoveRange(productSizesToRemove);

                    // Cập nhật hoặc thêm ProductSize
                    for (int i = 0; i < selectedSizeIds.Count; i++)
                    {
                        // Tìm ProductSize hiện có dựa trên SizeId
                        var existingProductSize = existingProductSizes
                            .FirstOrDefault(ps => ps.SizeId == model.SizeIds[i]);

                        if (existingProductSize != null)
                        {
                            // Cập nhật giá trị hiện có
                            existingProductSize.StockQuantity = model.StockQuantities[i];
                            existingProductSize.Price = model.Prices[i];
                            _context.Update(existingProductSize);
                        }
                        else
                        {
                            // Thêm ProductSize mới
                            var newProductSize = new ProductSize
                            {
                                ProductId = model.Product.ProductId,
                                SizeId = model.SizeIds[i],
                                StockQuantity = model.StockQuantities[i],
                                Price = model.Prices[i]
                            };
                            _context.Add(newProductSize);
                        }
                    }

                    // Xử lý ảnh mới
                    int displayOrder = product.ProductImages.Any() ? product.ProductImages.Max(pi => pi.DisplayOrder) + 1 : 1;
                    for (int i = 0; i < images.Count; i++)
                    {
                        if (displayOrder > 3) break; // Giới hạn 3 ảnh

                        string imageUrl = await SaveImageAsync(images[i]);
                        var productImage = new ProductImage
                        {
                            ProductId = model.Product.ProductId,
                            ImageUrl = imageUrl,
                            DisplayOrder = displayOrder,
                            IsMainImage = MainImageIndex.HasValue && MainImageIndex.Value == i
                        };

                        _context.Add(productImage);
                        displayOrder++;
                    }

                    // Cập nhật ảnh chính
                    if (MainImageId.HasValue)
                    {
                        foreach (var image in product.ProductImages)
                        {
                            image.IsMainImage = image.ImageId == MainImageId.Value;
                            _context.Update(image);
                        }
                    }
                    else if (MainImageIndex.HasValue && images.Any())
                    {
                        foreach (var image in product.ProductImages)
                        {
                            image.IsMainImage = false;
                            _context.Update(image);
                        }
                    }

                    // Đảm bảo có ít nhất một ảnh chính
                    if (!_context.ProductImages.Any(pi => pi.ProductId == model.Product.ProductId && pi.IsMainImage) && _context.ProductImages.Any(pi => pi.ProductId == model.Product.ProductId))
                    {
                        var firstImage = _context.ProductImages
                            .Where(pi => pi.ProductId == model.Product.ProductId)
                            .OrderBy(pi => pi.DisplayOrder)
                            .First();
                        firstImage.IsMainImage = true;
                        _context.Update(firstImage);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.Product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu ModelState không hợp lệ, tải lại dropdown và giữ kích thước đã chọn
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", model.Product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "Name", model.Product.BrandId);
            ViewBag.Sizes = new MultiSelectList(_context.Sizes, "SizeId", "Name", model.SizeIds);
            return View(model);
        }


        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        // Phương thức hỗ trợ lưu ảnh
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/images/{fileName}";
        }
    }
}
