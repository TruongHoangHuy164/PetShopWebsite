namespace DACS_PetShop.Models
{
    public class ProductEditViewModel
    {
        public Product  ?Product { get; set; } // Product details
        public List<ProductSize>? ProductSizes { get; set; }
        public List<int>? SizeIds { get; set; }
        public List<int>? StockQuantities { get; set; }
        public List<decimal>? Prices { get; set; }
        public List<int>? ProductSizeIds { get; set; }
        public List<ProductImage>? ProductImages { get; set; }
    }
}
