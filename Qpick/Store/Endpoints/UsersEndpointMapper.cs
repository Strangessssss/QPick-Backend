using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick.Store.Endpoints;

public class UsersEndpointMapper
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        
        var usersGroup = builder.MapGroup("/api/users");
        
        usersGroup.MapGet("/", GetFirstUser);
        usersGroup.MapGet("/{id:guid}", GetUser);
        
        usersGroup.MapPut("/{userId:guid}/cart/{productId:guid}", SetCartProduct);
        
        usersGroup.MapPost("/{userId:guid}/saved/{productId:guid}", AddSavedProduct);
        
        usersGroup.MapPost("/{userId:guid}/checkout", CheckOut);
    }
    private static async Task<Ok<User>> GetUser(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var user = await context.Users
            .Include(u => u.SavedProducts)
            .ThenInclude(p => p.Category)
            .Include(u => u.SavedProducts)
            .ThenInclude(p => p.Brand)
            .Include(u => u.CartProducts)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user is null)
        {
            user = (await context.Users.AddAsync(new User())).Entity;
            await context.SaveChangesAsync();
        }
        
        user.CartPrice = user.CartProducts.Select(c => c.Product?.Price * c.Quantity).Sum() ?? 0;
        
        return TypedResults.Ok(user);
    }
    
    private static async Task<Ok<User>> GetFirstUser(
        AppDbContext context)
    {
        var user = await context.Users.AddAsync(new User());
        await context.SaveChangesAsync();
        
        return TypedResults.Ok(user.Entity);
    }
    
    private static async Task<Results<NotFound<string>, Ok<CartProduct>>> SetCartProduct(
    AppDbContext context,
    [FromRoute] Guid userId,
    [FromRoute] Guid productId,
    [FromQuery] int quantity)
{
    if (!await context.Products.AnyAsync(p => p.Id == productId))
        return TypedResults.NotFound("Product not found");

    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    
    if (user is null)
    {
        user = new User { Id = userId };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    var cartProduct = await context.CartProducts
        .Include(cp => cp.Product)
        .FirstOrDefaultAsync(cp => cp.UserId == user.Id && cp.ProductId == productId);

    if (cartProduct is null)
    {
        if (quantity <= 0)
        {
            return TypedResults.Ok(new CartProduct 
            { 
                UserId = user.Id, 
                ProductId = productId, 
                Quantity = 0 
            });
        }

        cartProduct = new CartProduct
        {
            UserId = user.Id,
            ProductId = productId,
            Quantity = quantity
        };

        await context.CartProducts.AddAsync(cartProduct);
        await context.SaveChangesAsync();

        await context.Entry(cartProduct).Reference(cp => cp.Product).LoadAsync();

        return TypedResults.Ok(cartProduct);
    }

    if (quantity <= 0)
    {
        try
        {
            context.CartProducts.Remove(cartProduct);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            
        }

        return TypedResults.Ok(new CartProduct 
        { 
            UserId = user.Id, 
            ProductId = productId, 
            Quantity = 0 
        });
    }

    cartProduct.Quantity = quantity;

    try
    {
        context.CartProducts.Update(cartProduct);
        await context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return TypedResults.NotFound("Cart product no longer exists");
    }

    return TypedResults.Ok(cartProduct);
}
    
    private static async Task<Results<NotFound<string>, Ok<bool>>> AddSavedProduct(
        AppDbContext context, 
        [FromRoute] Guid userId,
        [FromRoute] Guid productId)
    {
        var user = await context.Users
            .Include(user => user.SavedProducts)
            .Include(user => user.CartProducts)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user is null)
        {
            user = (await context.Users.AddAsync(new User())).Entity;
            await context.SaveChangesAsync();
        }
        
        var product = await context.Products.FirstOrDefaultAsync(u => u.Id == productId);

        if (product is null)
        {
            return TypedResults.NotFound("Product not found");
        }

        bool saved;
        if (user.SavedProducts.Exists(p => p.Id == productId))
        {
            user.SavedProducts.Remove(product);
            saved = false;
        }
        else
        {
            user.SavedProducts.Add(product);
            saved = true;
        }
        
        await context.SaveChangesAsync();
        return TypedResults.Ok(saved);
    }
    
    private static async Task<Results<BadRequest<string>, Ok<Guid>>> CheckOut(
    HttpContext httpContext,
    [FromRoute] Guid userId,
    AppDbContext context)
{
    try
    {
        var form = await httpContext.Request.ReadFormAsync();

        var phone = form["phone"].ToString();
        var paymentMethod = form["paymentMethod"].ToString();
        // var promoCode = form["promoCode"].ToString(); // TODO: implement promo codes
        var deliveryType = form["deliveryType"].ToString();
        var location = form["location"].ToString();

        Console.WriteLine(JsonSerializer.Serialize(form));
        
        if (string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(paymentMethod) ||
            string.IsNullOrWhiteSpace(deliveryType))
        {
            return TypedResults.BadRequest("Phone, payment method, or delivery type missing.");
        }

        var user = await context.Users
            .Include(u => u.CartProducts)
            .ThenInclude(cp => cp.Product)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return TypedResults.BadRequest("User not found.");

        if (!user.CartProducts.Any())
            return TypedResults.BadRequest("Cart is empty.");

        LatLng? shippingAddress = null;
        if (deliveryType == "delivery")
        {
            if (string.IsNullOrWhiteSpace(location))
                return TypedResults.BadRequest("Delivery requires a valid location.");

            shippingAddress = JsonSerializer.Deserialize<LatLng>(
                location,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (shippingAddress is null || shippingAddress.Lat == 0 || shippingAddress.Lng == 0)
                return TypedResults.BadRequest("Invalid location data.");
        }

        var order = new Order
        {
            Phone = phone,
            PaymentMethod = paymentMethod,
            ShippingLat = shippingAddress?.Lat,
            ShippingLng = shippingAddress?.Lng,
            Created = DateTime.UtcNow,
            Status = "Pending",
            Products = user.CartProducts.Select(cp => cp.Product!).ToList(),
            TotalPrice = user.CartProducts.Sum(cp => cp.Product!.Price * cp.Quantity)
        };

        user.CartProducts.Clear();
        var entity = context.Orders.Add(order);
        await context.SaveChangesAsync();

        return TypedResults.Ok(entity.Entity.Id);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        throw;
    }
}
}