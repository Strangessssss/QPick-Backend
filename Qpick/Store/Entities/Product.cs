namespace Qpick.Store.Entities;

public class Product
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public List<string> ProductImages { get; set; } = [];
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public float Rating { get; set; }
    
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
}