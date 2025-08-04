using Microsoft.EntityFrameworkCore;
using Todo_List_API.DTOs;
using Todo_List_API.Interfaces;
using Todo_List_API.Models;

namespace Todo_List_API.Services
{
    public class ToDoService : IToDoService
    {
        private readonly ApplicationDbContext _context;

        public ToDoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateTaskAsync(int userID, ToDoDTO toDoDto)
        {
            await ValidateUser(userID);
            ValidateToDoDTO(toDoDto);
            ToDo toDoItem = CreateToDoEntity(userID, toDoDto);
            await AssignTagsToToDoAsync(toDoDto, toDoItem);
            await AddToDoAsync(toDoItem);
        }

        private async Task AddToDoAsync(ToDo toDoItem)
        {
            // Add the ToDo item to the context
            await _context.ToDos.AddRangeAsync(toDoItem);
            // Save changes to the database
            await _context.SaveChangesAsync();
        }

        private async Task AssignTagsToToDoAsync(ToDoDTO toDoDto, ToDo toDoItem)
        {
            // add tags if any
            if (!toDoDto.Tags.Any())
                return;

            // Initialize the ToDoTags collection
            toDoItem.ToDoTags = new List<ToDoTag>();
            foreach (var tag in toDoDto.Tags)
            {
                if (tag is not null && tag.Trim().Length > 0)
                {
                    // Trim the tag to remove any leading or trailing whitespace and lowercase it
                    var trimmedTag = tag.Trim().ToLowerInvariant();

                    // Check if the tag already exists in the database
                    int existingTagId = await _context.Tags
                        .Where(t => t.Name == trimmedTag)
                        .Select(t => t.Id)
                        .FirstOrDefaultAsync();

                    // If the tag exists, use its ID; otherwise, create a new Tag entity
                    if (existingTagId != 0)
                    {
                        toDoItem.ToDoTags.Add(new ToDoTag
                        {
                            ToDo = toDoItem,
                            TagId = existingTagId
                        });
                    }
                    else
                    {
                        toDoItem.ToDoTags.Add(new ToDoTag
                        {
                            ToDo = toDoItem,
                            Tag = new Tag
                            {
                                Name = trimmedTag
                            }
                        });
                    }
                }
            }
        }

        private static ToDo CreateToDoEntity(int UserId, ToDoDTO toDoDto)
        {
            // Map DTO to Model
            return new ToDo
            {
                UserId = UserId,
                Title = toDoDto.Title,
                Description = toDoDto.Description,
            };
        }

        private static void ValidateToDoDTO(ToDoDTO toDoDto)
        {
            // Validate the DTO
            if (string.IsNullOrWhiteSpace(toDoDto.Title) || string.IsNullOrEmpty(toDoDto.Description))
            {
                throw new ArgumentException("Title or Description cannot be empty.");
            }
        }

        private async Task ValidateUser(int userID)
        {
            // Validate the user ID
            if (!await _context.Users.AnyAsync(u => u.Id == userID))
                throw new ArgumentException("User not found.");
        }
    }
}
