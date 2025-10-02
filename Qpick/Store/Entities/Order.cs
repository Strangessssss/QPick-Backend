namespace Qpick.Store.Entities;

public class Order
{
    public Guid Id { get; set; }
    
    public string Phone { get; set; } = string.Empty;
    
    public double? ShippingLat { get; set; }
    public double? ShippingLng { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    
    public List<Product> Products { get; set; } = new();
    public decimal TotalPrice { get; set; }
    
    public DateTime Created { get; set; }
    public string Status { get; set; } = "Pending";
    
}