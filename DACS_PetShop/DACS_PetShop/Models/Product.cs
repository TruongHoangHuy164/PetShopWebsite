using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        public string Name { get; set; }

        public string Description { get; set; }

        //[Required(ErrorMessage = "Price is required.")]
        //[Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
        //public decimal Price { get; set; }

        //[Required(ErrorMessage = "Stock quantity is required.")]
        //[Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative.")]
        //public int StockQuantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        // Liên kết với ProductSize
        public ICollection<ProductSize>? ProductSizes { get; set; }
        public ICollection<ProductImage>? ProductImages { get; set; }
        public ICollection<Review>? Reviews { get; set; } // Đảm bảo có thuộc tính này
    }
}
