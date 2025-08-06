using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Todo_List_API.DTOs;
using Todo_List_API.Extensions;
using Todo_List_API.Interfaces;
using Todo_List_API.Models;
using Todo_List_API.Pagination;

namespace Todo_List_API.Services
{
    public class ToDoService : IToDoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ToDoService> _logger;

        public ToDoService(ApplicationDbContext context, ILogger<ToDoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateTaskAsync(int userID, CreateToDoDTO createToDoDto)
        {
            await ValidateUser(userID);
            ValidateToDoDTO(createToDoDto);
            ToDo toDoItem = CreateToDoEntity(userID, createToDoDto);
            await AssignTagsToToDoAsync(createToDoDto, toDoItem);
            await AddToDoAsync(toDoItem);
            _logger.LogInformation("Task created successfully. TaskId: {TaskId}", toDoItem.Id);
        }

        private async Task AddToDoAsync(ToDo toDoItem)
        {
            // Add the ToDo item to the context
            await _context.ToDos.AddRangeAsync(toDoItem);
            // Save changes to the database
            await _context.SaveChangesAsync();
        }

        private async Task AssignTagsToToDoAsync(CreateToDoDTO createToDoDto, ToDo toDoItem)
        {
            // add tags if any
            if (!createToDoDto.Tags.Any())
                return;

            // Initialize the ToDoTags collection
            toDoItem.ToDoTags = new List<ToDoTag>();
            foreach (var tag in createToDoDto.Tags)
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

        private static ToDo CreateToDoEntity(int UserId, CreateToDoDTO createToDoDto)
        {
            // Map DTO to Model
            return new ToDo
            {
                UserId = UserId,
                Title = createToDoDto.Title,
                Description = createToDoDto.Description,
            };
        }

        private void ValidateToDoDTO(CreateToDoDTO createToDoDto)
        {
            // Validate the DTO
            if (string.IsNullOrWhiteSpace(createToDoDto.Title) || string.IsNullOrEmpty(createToDoDto.Description))
            {
                _logger.LogError("Title or Description cannot be empty.");
                throw new ArgumentException("Title or Description cannot be empty.");
            }
        }

        private async Task ValidateUser(int userID)
        {
            // Validate the user ID
            if (!await _context.Users.AnyAsync(u => u.Id == userID))
            {
                _logger.LogError("User not found. UserId: {UserId}", userID);
                throw new ArgumentException("User not found.");
            }
        }

        public async Task UpdateTaskAsync(int userID, UpdateToDoDTO updateToDoDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ValidateUpdateToDoDto(updateToDoDto);
                var updateToDoItem = await RetrieveToDoItemForUpdate(userID, updateToDoDto);
                MapUpdatedValuesFromDto(updateToDoDto, updateToDoItem);
                // update tags if any
                await UpdateTags(updateToDoDto, updateToDoItem);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Task updated successfully. TaskId: {TaskId}", updateToDoDto.TaskId);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e, "Error updating task. TaskId: {TaskId}", updateToDoDto.TaskId);
                throw new Exception(e.Message);
            }
        }

        private async Task UpdateTags(UpdateToDoDTO updateToDoDto, ToDo updateToDoItem)
        {
            if (!updateToDoDto.Tags.Any())
            {
                // this means that the user wants to remove all tags
                await RemoveAllTagsAsync(updateToDoDto.TaskId);
                return;
            }

            updateToDoDto.Tags = updateToDoDto.Tags
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList();

            var existingTagsNames = await FetchExistingTagNames(updateToDoDto);

            await RemoveUnwantedTags(updateToDoDto, existingTagsNames);

            await AddNewTags(updateToDoDto, existingTagsNames);
        }

        private async Task<List<string>> FetchExistingTagNames(UpdateToDoDTO updateToDoDto)
            => await _context.ToDoTags
                .Where(t => t.ToDoId == updateToDoDto.TaskId)
                .Select(t => t.Tag.Name)
                .ToListAsync();

        private async Task AddNewTags(UpdateToDoDTO updateToDoDto, List<string> existingTagsNames)
        {
            var tagsToAdd = updateToDoDto.Tags?
                .Except(existingTagsNames ?? Enumerable.Empty<string>())
                .ToList();

            if (tagsToAdd.Any())
            {
                foreach (var tagToAdd in tagsToAdd)
                {
                    if (string.IsNullOrWhiteSpace(tagToAdd))
                        continue;

                    // check if the tag already exists
                    var existingTagId = await _context.Tags
                        .Where(t => t.Name == tagToAdd)
                        .Select(t => t.Id)
                        .FirstOrDefaultAsync();

                    var newToDoTag = new ToDoTag()
                    {
                        ToDoId = updateToDoDto.TaskId,
                    };

                    if (existingTagId != 0)
                    {
                        newToDoTag.TagId = existingTagId;
                    }
                    else
                    {
                        newToDoTag.Tag = new Tag
                        {
                            Name = tagToAdd
                        };
                    }

                    await _context.ToDoTags.AddAsync(newToDoTag);
                }
            }
        }

        private async Task RemoveUnwantedTags(UpdateToDoDTO updateToDoDto, List<string> existingTagsNames)
        {
            var tagsToRemove = (existingTagsNames ?? Enumerable.Empty<string>())
                .Except(updateToDoDto.Tags ?? Enumerable.Empty<string>())
                .ToList();

            if (tagsToRemove.Any())
            {
                var tagIdsToRemove = await _context.Tags
                    .Where(t => tagsToRemove.Contains(t.Name))
                    .Select(t => t.Id)
                    .ToListAsync();

                await _context.ToDoTags
                    .Where(t => tagIdsToRemove.Contains(t.TagId) && t.ToDoId == updateToDoDto.TaskId)
                    .ExecuteDeleteAsync();
            }
        }

        private async Task RemoveAllTagsAsync(int TaskId)
        {
            // check if there are any tags
            var isToDoTagsExist = await _context.ToDoTags.AnyAsync(t => t.ToDoId == TaskId);

            // if there are no tags, this means that this task has no tags
            if (!isToDoTagsExist)
                return;

            // remove all tags
            var result = await _context.ToDoTags
                .Where(t => t.ToDoId == TaskId)
                .ExecuteDeleteAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to remove tags from task. TaskId: {TaskId}", TaskId);
                throw new DbUpdateException("Failed to remove tags from task.");
            }
        }

        private void MapUpdatedValuesFromDto(UpdateToDoDTO updateToDoDto, ToDo? updateToDoItem)
        {
            // Map the updated values from the DTO to the entity
            updateToDoItem.UpdatedDate = DateTime.UtcNow;
            updateToDoItem.Title = updateToDoDto.Title;
            updateToDoItem.Description = updateToDoDto.Description;
        }

        private async Task<ToDo?> RetrieveToDoItemForUpdate(int userID, UpdateToDoDTO updateToDoDto)
        {
            var updateToDoItem = await _context.ToDos
                .Where(t => t.Id == updateToDoDto.TaskId && t.UserId == userID)
                .FirstOrDefaultAsync();

            if (updateToDoItem == null)
            {
                _logger.LogError("Task not found. TaskId: {TaskId}", updateToDoDto.TaskId);
                throw new ArgumentException("Task not found.");
            }

            return updateToDoItem;
        }

        private void ValidateUpdateToDoDto(UpdateToDoDTO updateToDoDto)
        {
            if (updateToDoDto.TaskId <= 0)
            {
                _logger.LogError("TaskId must be greater than zero.");
                throw new ArgumentException("TaskId must be greater than zero.");
            }

            // Validate the DTO
            if (string.IsNullOrWhiteSpace(updateToDoDto.Title) || string.IsNullOrEmpty(updateToDoDto.Description))
            {
                _logger.LogError("Title or Description cannot be empty.");
                throw new ArgumentException("Title or Description cannot be empty.");
            }
        }

        public async Task<ToDoResponseDTO> GetTaskAsync(int userID, int taskId)
        {
            await ValidateUser(userID);
            return await RetrieveTaskDetails(userID, taskId);
        }

        private async Task<ToDoResponseDTO> RetrieveTaskDetails(int userID, int taskId)
        {
            var item = await _context.ToDos
                .Where(t => t.UserId == userID && t.Id == taskId)
                .Select(t => new ToDoResponseDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    UpdatedDate = t.UpdatedDate,
                    Tags = t.ToDoTags
                        .Select(tt => tt.Tag.Name)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return item ?? throw new KeyNotFoundException("Task not found.");
        }

        public async Task<ToDoDetailsDTO> GetAllTasksAsync(int userID, PaginationRequestDTO paginationRequestDto)
        {
            var items = await RetrieveUserToDoItems(userID, paginationRequestDto);

            var mappedItems = MapPaginatedToDoToDetailsDTO(items);

            return mappedItems;
        }

        private static ToDoDetailsDTO MapPaginatedToDoToDetailsDTO(PaginatedResult<ToDoResponseDTO> items)
        {
            var mappedItems = new ToDoDetailsDTO();
            if (items is not null)
            {
                mappedItems.ToDoResponseDTOs = items.Data.Select(t => new ToDoResponseDTO()
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    UpdatedDate = t.UpdatedDate,
                    Tags = t.Tags
                }).ToList();

                mappedItems.TotalCount = items.TotalCount;
                mappedItems.TotalPages = items.TotalPages;
                mappedItems.CurrentPage = items.CurrentPage;
                mappedItems.PageSize = items.PageSize;
            }

            return mappedItems;
        }

        private async Task<PaginatedResult<ToDoResponseDTO>> RetrieveUserToDoItems(int userID,
            PaginationRequestDTO paginationRequestDto)
        {
            var query = ApplyToDoFilters(userID, paginationRequestDto);

            query = ApplySortingToDoItems(paginationRequestDto, query);

            var items = await query
                .Select(t => new ToDoResponseDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    UpdatedDate = t.UpdatedDate,
                    Tags = t.ToDoTags
                        .Select(tt => tt.Tag.Name)
                        .ToList()
                })
                .ToPaginatedListAsync(paginationRequestDto.PageNumber, paginationRequestDto.PageSize);

            return items;
        }

        private IQueryable<ToDo> ApplySortingToDoItems(PaginationRequestDTO paginationRequestDto,
            IQueryable<ToDo> query)
        {
            Expression<Func<ToDo, object>> keySelector = paginationRequestDto.SortBy switch
            {
                "title" => t => t.Title,
                "tags" => t => t.ToDoTags.Count(),
                _ => t => t.Id
            };

            query = paginationRequestDto.IsAscending
                ? query.OrderBy(keySelector)
                : query.OrderByDescending(keySelector);
            return query;
        }

        private IQueryable<ToDo> ApplyToDoFilters(int userID, PaginationRequestDTO paginationRequestDto)
        {
            var query = _context.ToDos
                .Where(u => u.UserId == userID);

            if (!string.IsNullOrWhiteSpace(paginationRequestDto.FilterByTag))
                query = query.Where(t =>
                    t.ToDoTags.Any(b => b.Tag.Name.Contains(paginationRequestDto.FilterByTag.ToLower())));

            if (!string.IsNullOrWhiteSpace(paginationRequestDto.FilterByTitle))
                query = query.Where(t => t.Title.Contains(paginationRequestDto.FilterByTitle));

            if (!string.IsNullOrWhiteSpace(paginationRequestDto.FilterByDescription))
                query = query.Where(t => t.Description.Contains(paginationRequestDto.FilterByDescription));
            return query;
        }

        public async Task DeleteTaskAsync(int userID, int taskId)
        {
            await ValidateUser(userID);
            var item = await _context.ToDos
                .Where(t => t.Id == taskId && t.UserId == userID)
                .FirstOrDefaultAsync();
            if (item is null)
            {
                _logger.LogError("Task not found. TaskId: {TaskId}", taskId);
                throw new KeyNotFoundException("Task not found.");
            }

            _context.ToDos.Remove(item);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task deleted successfully. TaskId: {TaskId}", taskId);
        }
    }
}