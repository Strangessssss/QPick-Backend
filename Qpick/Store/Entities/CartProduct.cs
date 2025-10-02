namespace Qpick.Store.Entities;

public class CartProduct
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    
    public int Quantity { get; set; } = 1;
}