using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick.Store.Endpoints;

public class UiEndpointMapper
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        var productsGroup = builder.MapGroup("/api/ui");

        productsGroup.MapGet("/accordion", GetAccordion);
    }

    private static async Task<Results<NotFound<string>, Ok<Dictionary<string, List<Category>>>>> GetAccordion(
        AppDbContext context
    )
    {
        var brandsWithCategories = await context.Brands
            .Include(b => b.Products)
            .ThenInclude(p => p.Category)
            .ToListAsync();

        var dict = brandsWithCategories.ToDictionary(
            b => b.Name, 
            b => b.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category!)
                .Distinct()
                .ToList()
        );

        return TypedResults.Ok(dict);
    }
    
}
