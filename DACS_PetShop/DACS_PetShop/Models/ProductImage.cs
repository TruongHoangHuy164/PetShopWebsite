using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Product Image
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Range(1, 3)]
        public int DisplayOrder { get; set; }
    }
}
