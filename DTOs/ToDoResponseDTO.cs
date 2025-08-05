namespace Todo_List_API.DTOs;

public class ToDoResponseDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; } 
    public List<string> Tags { get; set; } = new List<string>();
}