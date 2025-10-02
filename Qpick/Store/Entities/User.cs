using System.ComponentModel.DataAnnotations.Schema;

namespace Qpick.Store.Entities;

public class User
{
    public Guid Id { get; set; }
    
    public List<Product> SavedProducts { get; set; } = [];
    public List<CartProduct> CartProducts { get; set; } = [];
    
    [NotMapped]
    public decimal CartPrice { get; set; }
}