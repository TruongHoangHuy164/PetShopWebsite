using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Cart
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem> CartItems { get; set; }
    }

}
