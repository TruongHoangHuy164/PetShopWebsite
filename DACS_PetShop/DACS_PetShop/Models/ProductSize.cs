using System.ComponentModel.DataAnnotations;
using DACS_PetShop.Models;

public class ProductSize
{
    public int ProductSizeId { get; set; }

    // Liên kết với Product
    public int ProductId { get; set; }
    public Product Product { get; set; }

    // Liên kết với Size
    public int SizeId { get; set; }
    public Size Size { get; set; }

    // Số lượng tồn kho của sản phẩm theo kích thước
    [Required]
    public int StockQuantity { get; set; }

    // Giá của sản phẩm theo kích thước
    [Required]
    public decimal Price { get; set; }
}
