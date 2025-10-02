using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick.Store.Endpoints;

public class CategoriesEndpointMapper
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        var categoriesGroup = builder.MapGroup("/api/categories");

        categoriesGroup.MapGet("/", GetCategories);
        categoriesGroup.MapGet("/{id:guid}", GetCategory);
        categoriesGroup.MapPost("/", AddCategory).DisableAntiforgery();
        categoriesGroup.MapDelete("/{id:guid}", RemoveCategory);
    }
    private static Task<Results<NotFound<string>, Ok<List<Category>>>> GetCategories(
        AppDbContext context,
        ClaimsPrincipal userPrincipal)
    {
        var categories = context.Categories.ToList();
        return Task.FromResult<Results<NotFound<string>, Ok<List<Category>>>>(TypedResults.Ok(categories));
    }
    
    private static async Task<Results<NotFound<string>, Ok<Category>>> GetCategory(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var category = await context.Categories.FirstOrDefaultAsync();
        return TypedResults.Ok(category);
    }
    
    private static async Task<Results<NotFound<string>, Ok>> RemoveCategory(
        AppDbContext context,
        [FromRoute] Guid id)
    {
        var category = context.Categories.FirstOrDefault(p => p.Id == id);

        if (category is null)
            return TypedResults.NotFound("Category not found");
        
        if (context.Products.Any(p => p.CategoryId == id))
        {
            return TypedResults.NotFound("Impossible to delete category with assigned products");
        }
        
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return TypedResults.Ok();
    }

    private static async Task<Results<NotFound<string>, Ok>> AddCategory(
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
            
            var category = new Category
            {
                Name = name!
            };

            context.Categories.Add(category);
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