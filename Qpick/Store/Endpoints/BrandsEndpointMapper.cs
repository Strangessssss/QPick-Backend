using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick.Store.Endpoints;

public class BrandsEndpointMapper
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        var brandsGroup = builder.MapGroup("/api/brands");

        brandsGroup.MapGet("/", GetBrands);
        brandsGroup.MapGet("/{id:guid}", GetBrand);
        brandsGroup.MapPost("/", AddBrand).DisableAntiforgery();
        brandsGroup.MapDelete("/{id:guid}", RemoveBrand);
    }
    private static Task<Results<NotFound<string>, Ok<List<Brand>>>> GetBrands(
        AppDbContext context,
        ClaimsPrincipal userPrincipal)
    {
        var brands = context.Brands.ToList();
        return Task.FromResult<Results<NotFound<string>, Ok<List<Brand>>>>(TypedResults.Ok(brands));
    }
    
    private static async Task<Results<NotFound<string>, Ok<Brand>>> GetBrand(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var brand = await context.Brands.FirstOrDefaultAsync();
        return TypedResults.Ok(brand);
    }
    
    private static async Task<Results<NotFound<string>, Ok>> RemoveBrand(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var brand = context.Brands.FirstOrDefault(p => p.Id == id);

        if (brand is null)
            return TypedResults.NotFound("Category not found");

        if (context.Products.Any(p => p.BrandId == id))
        {
            return TypedResults.NotFound("Impossible to delete brand with assigned products");
        }
        
        context.Brands.Remove(brand);
        await context.SaveChangesAsync();
        return TypedResults.Ok();
    }

    private static async Task<Results<NotFound<string>, Ok>> AddBrand(
        HttpContext httpContext,
        AppDbContext context)
    {
        try
        {
            var form = await httpContext.Request.ReadFormAsync();

            var name = form["name"];
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return TypedResults.NotFound("Invalid input data");
            }
            
            var brand = new Brand
            {
                Name = name!
            };

            context.Brands.Add(brand);
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