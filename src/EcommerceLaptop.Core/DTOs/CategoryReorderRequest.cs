namespace EcommerceLaptop.Core.DTOs;

public class CategoryReorderRequest
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public int? ParentId { get; set; }
}