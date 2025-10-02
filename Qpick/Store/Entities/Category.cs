using System.Text.Json.Serialization;

namespace Qpick.Store.Entities;

public class Category
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    [JsonIgnore]
    public List<Product> Products { get; set; } = [];
}