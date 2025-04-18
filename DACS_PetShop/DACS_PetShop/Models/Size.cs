using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    public class Size
    {
        [Key]
        public int SizeId { get; set; }

        [Required]
        [StringLength(20)]
        public string ?Name { get; set; }  

        // Một Size có thể thuộc nhiều sản phẩm
        public ICollection<ProductSize>? ProductSizes { get; set; }
    }
}
