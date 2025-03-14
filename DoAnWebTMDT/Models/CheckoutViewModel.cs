public class CheckoutViewModel
{
    public List<CheckoutItemViewModel> CartItems { get; set; } = new List<CheckoutItemViewModel>();
    public List<AddressViewModel> Addresses { get; set; } = new List<AddressViewModel>();
    public int? SelectedAddressId { get; set; }
    public string NewAddressDetail { get; set; }
}

public class CheckoutItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal NewPrice { get; set; }
}

public class AddressViewModel
{
    public int AddressId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string AddressDetail { get; set; }
}
