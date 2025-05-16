namespace TakeServus.Application.DTOs.Common;

public class SortableQuery : PaginationQuery
{
  public string? SortBy { get; set; } // e.g., "CreatedAt"
  public bool Desc { get; set; } = true;
}