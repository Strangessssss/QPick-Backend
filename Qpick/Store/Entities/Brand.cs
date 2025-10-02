using System.Text.Json.Serialization;

namespace Qpick.Store.Entities;

public class Brand
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public List<Product> Products { get; set; } = new();
}