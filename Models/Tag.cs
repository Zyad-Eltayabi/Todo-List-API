namespace Todo_List_API.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public ICollection<ToDoTag>? ToDoTags  { get; set; }
}