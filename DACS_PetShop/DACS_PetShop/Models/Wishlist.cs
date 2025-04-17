using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Wishlist    
    public class Wishlist
    {
        [Key]
        public int WishlistId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WishlistItem> WishlistItems { get; set; }
    }
}
