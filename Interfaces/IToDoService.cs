using Todo_List_API.DTOs;
using Todo_List_API.Models;
using Todo_List_API.Pagination;

namespace Todo_List_API.Interfaces
{
    public interface IToDoService
    {
        Task CreateTaskAsync(int userID, CreateToDoDTO createToDoDto);
        Task UpdateTaskAsync(int userID, UpdateToDoDTO updateToDoDto);
        Task<ToDoResponseDTO> GetTaskAsync(int userID, int taskId);
        Task<ToDoDetailsDTO> GetAllTasksAsync(int userID, PaginationRequestDTO paginationRequestDto);
        Task DeleteTaskAsync(int userID, int taskId);
    }
}