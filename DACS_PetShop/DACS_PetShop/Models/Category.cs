using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Category
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public ICollection<Product>? Products { get; set; }
    }

}
