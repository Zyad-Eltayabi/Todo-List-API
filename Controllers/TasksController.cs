using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Todo_List_API.DTOs;
using Todo_List_API.Interfaces;

namespace Todo_List_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class TasksController : ControllerBase
    {
        private readonly IToDoService _toDoService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(IToDoService toDoService, ILogger<TasksController> logger)
        {
            _toDoService = toDoService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            return userIdClaim > 0 ? userIdClaim : throw new UnauthorizedAccessException("You are not authorized");
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateTask([FromBody] CreateToDoDTO createToDoDto)
        {
            _logger.LogInformation(
                "Creating task with details - Title: {Title}, Description: {Description}, TagCount: {TagCount}",
                createToDoDto.Title, createToDoDto.Description, createToDoDto.Tags.Count);
            var userId = GetUserId();
            await _toDoService.CreateTaskAsync(userId, createToDoDto);
            return Ok();
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> UpdateTask([FromBody] UpdateToDoDTO updateToDoDto)
        {
            _logger.LogInformation(
                "Updating task with details - TaskId: {TaskId}, Title: {Title}, Description: {Description}, TagCount: {TagCount}",
                updateToDoDto.TaskId, updateToDoDto.Title, updateToDoDto.Description, updateToDoDto.Tags.Count);
            var userId = GetUserId();
            await _toDoService.UpdateTaskAsync(userId, updateToDoDto);
            return Ok();
        }

        [HttpGet]
        [Route("{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ToDoResponseDTO>> GetTask([FromRoute] int taskId)
        {
            _logger.LogInformation("Retrieving task with details - TaskId: {TaskId}", taskId);
            var userId = GetUserId();
            return await _toDoService.GetTaskAsync(userId, taskId);
        }
    }
}