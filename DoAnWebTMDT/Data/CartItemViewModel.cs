namespace DoAnWebTMDT.Models
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }        // Mã sản phẩm
        public string ProductName { get; set; }   // Tên sản phẩm
        public string ProductImage { get; set; }  // Ảnh sản phẩm
        public decimal NewPrice { get; set; }      // Giá sản phẩm
        public int Quantity { get; set; }         // Số lượng sản phẩm
    }
}
