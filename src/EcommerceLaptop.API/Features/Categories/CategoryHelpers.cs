using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace EcommerceLaptop.API.Features.Categories;

internal static class CategoryHelpers
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize and remove diacritics
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

        // Replace non-alphanumeric characters with hyphens
        var result = Regex.Replace(cleaned, "[^A-Za-z0-9]+", "-").Trim('-');

        return result.ToLowerInvariant();
    }

    public static async Task<string> GenerateUniqueSlug(ApplicationDbContext context, string baseSlug)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "category";

        var slug = baseSlug;
        var index = 1;
        while (await context.ProductCategories.AnyAsync(c => c.Slug == slug))
        {
            slug = $"{baseSlug}-{index}";
            index++;
        }

        return slug;
    }

    public static async Task<bool> IsCircularReference(ApplicationDbContext context, int categoryId, int parentId)
    {
        var current = await context.ProductCategories.FindAsync(parentId);
        while (current != null)
        {
            if (current.Id == categoryId)
                return true;

            if (current.ParentId.HasValue)
                current = await context.ProductCategories.FindAsync(current.ParentId.Value);
            else
                break;
        }
        return false;
    }
}
