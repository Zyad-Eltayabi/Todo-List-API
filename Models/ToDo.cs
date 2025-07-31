namespace Todo_List_API.Models;

public class ToDo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }
    
    public ICollection<ToDoTag>? ToDoTags  { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}