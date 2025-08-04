namespace Todo_List_API.DTOs
{
    public class UpdateToDoDTO
    {
        public int TaskId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
