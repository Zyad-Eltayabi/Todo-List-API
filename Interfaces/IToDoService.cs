using Todo_List_API.DTOs;

namespace Todo_List_API.Interfaces
{
    public interface IToDoService
    {
        Task CreateTaskAsync(int userID, ToDoDTO toDoDto);
    }
}
