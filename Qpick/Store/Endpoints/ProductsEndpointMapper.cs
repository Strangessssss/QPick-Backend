using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick.Store.Endpoints;

public class ProductsEndpointMapper
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        var productsGroup = builder.MapGroup("/api/products");

        productsGroup.MapGet("/", GetProducts);
        productsGroup.MapGet("/{id:guid}", GetProduct);
        productsGroup.MapPost("/", AddProduct).DisableAntiforgery();
        productsGroup.MapDelete("/{id:guid}", RemoveProduct);
    }

    private static async Task<Results<NotFound<string>, Ok<List<Product>>>> GetProducts(
        AppDbContext context,
        [FromQuery] string? category,   
        [FromQuery] string? brand)
    {
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category != null && p.Category.Name == category);

        if (!string.IsNullOrEmpty(brand))
            query = query.Where(p => p.Brand != null && p.Brand.Name == brand);

        var products = await query.ToListAsync();

        return TypedResults.Ok(products);
    }
    
    private static async Task<Results<NotFound<string>, Ok<Product>>> GetProduct(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var product = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        return TypedResults.Ok(product);
    }
    
    private static async Task<Results<ForbidHttpResult, NotFound<string>, Ok>> RemoveProduct(
        AppDbContext context,
        [FromRoute] Guid id,
        [FromBody] DeleteBrandRequest request
    )
    {
        if (!context.Admins.Any(a => a.Id == request.Token))
        {
            return TypedResults.Forbid();
        }
        
        var product = context.Products.FirstOrDefault(p => p.Id == id);

        if (product is null)
            return TypedResults.NotFound("Product not found");
        
        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return TypedResults.Ok();
    }

    private static async Task<Results<ForbidHttpResult, NotFound<string>, Ok>> AddProduct(
        HttpContext httpContext,
        AppDbContext context
        )
    {
        try
        {
            var form = await httpContext.Request.ReadFormAsync();

            var name = form["name"];
            var price = form["price"];
            var description = form["description"];
            var category = form["category"];
            var brand = form["brand"];
            var images = form.Files.GetFiles("images");
            var token = form["token"];

            if (Guid.TryParse(token, out var tokenGuid))
            {
                if (!context.Admins.Any(a => a.Id == tokenGuid))
                {
                    return TypedResults.Forbid();
                }
            }
            
            var imagePaths = new List<string>();
            if (images.Count > 0)
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                foreach (var file in images)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(imagesDir, uniqueFileName);

                        await using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        imagePaths.Add($"/images/products/{uniqueFileName}");
                    }
                }
            }
            
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || imagePaths.Count == 0)
            {
                return TypedResults.NotFound("Invalid input data");
            }

            if (!Guid.TryParse(brand, out var brandId) || brandId == Guid.Empty)
            {
                return TypedResults.NotFound("Invalid brand"); 
            }
            
            if (!Guid.TryParse(category, out var categoryId) || categoryId == Guid.Empty)
            {
                return TypedResults.NotFound("Invalid category"); 
            }

            if (!decimal.TryParse(price, out decimal priceInt))
            {
                return TypedResults.NotFound("Invalid price");
            }
            
            var product = new Product
            {
                Name = name!,
                Description = description!,
                Price = priceInt,
                Rating = 4.5f,
                ProductImages = imagePaths,
                CategoryId = categoryId,
                BrandId = brandId,
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}
