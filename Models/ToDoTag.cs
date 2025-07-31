namespace Todo_List_API.Models;

public class ToDoTag
{
    public int Id { get; set; }
    
    public int ToDoId { get; set; }
    public ToDo ToDo { get; set; }

    public int TagId { get; set; }
    public Tag Tag { get; set; }
}