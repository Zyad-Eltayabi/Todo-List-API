namespace Todo_List_API.DTOs;

public class ToDoDetailsDTO
{
   public IReadOnlyList<ToDoResponseDTO> ToDoResponseDTOs { get; set; }
    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
}