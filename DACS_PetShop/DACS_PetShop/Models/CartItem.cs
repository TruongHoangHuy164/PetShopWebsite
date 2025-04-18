using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? SizeId { get; set; } // Nullable để đồng bộ với migration
        public Size Size { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}