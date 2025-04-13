using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Wishlist Item
    public class WishlistItem
    {
        [Key]
        public int WishlistItemId { get; set; }

        public int WishlistId { get; set; }
        public Wishlist Wishlist { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
