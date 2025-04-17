using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACS_PetShop.Models
{
    // Model for Product Image
    public class ProductImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Tự động tăng
        public int ImageId { get; set; }

        [Required]
        public string? ImageUrl { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Range(1, 3)]
        public int DisplayOrder { get; set; }

        // Thêm thuộc tính IsMainImage để chọn ảnh chính
        [Required]
        public bool IsMainImage { get; set; }
    }
}
