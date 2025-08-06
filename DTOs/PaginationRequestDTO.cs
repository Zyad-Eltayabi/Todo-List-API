namespace Todo_List_API.DTOs;

public class PaginationRequestDTO
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? FilterByTag { get; set; }
    public string? FilterByTitle { get; set; }
    public string? FilterByDescription { get; set; }

    public string? SortBy { get; set; }
    public bool IsAscending { get; set; } = true;
}