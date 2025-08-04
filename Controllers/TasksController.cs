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
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
    public class TasksController : ControllerBase
    {
        private readonly IToDoService _toDoService;

        public TasksController(IToDoService toDoService)
        {
            _toDoService = toDoService;
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
        public async Task<ActionResult> CreateTask([FromBody] ToDoDTO toDoDto)
        {
            var userId = GetUserId();
            await _toDoService.CreateTaskAsync(userId, toDoDto);
            return Ok();
        }
    }
}
