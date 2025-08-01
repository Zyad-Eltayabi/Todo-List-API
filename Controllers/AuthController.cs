using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Todo_List_API.DTOs;
using Todo_List_API.Interfaces;

namespace Todo_List_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDTO registerDto)
        {
            var response = await _authService.RegisterAsync(registerDto);
            if (!response.IsAuthenticated)
                return BadRequest(response);

            SetTokenCookie(response.RefreshToken, response.RefreshTokenExpiresOn);
            return Ok(response);
        }

        private void SetTokenCookie(string token, DateTime expirationDate)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expirationDate.ToLocalTime()
            };
            Response.Cookies.Append("RefreshToken", token, cookieOptions);
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDTO loginDto)
        {
            var response = await _authService.LoginAsync(loginDto);
            SetTokenCookie(response.RefreshToken, response.RefreshTokenExpiresOn);
            return Ok(response);
        }

        [HttpGet]
        [Route("refresh-token")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token is missing.");

            var response = await _authService.RefreshTokenAsync(refreshToken);
            SetTokenCookie(response.RefreshToken, response.RefreshTokenExpiresOn);
            return Ok(response);
        }

        [HttpPut]
        [Route("revoke-token")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RevokeToken()
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token is missing.");

            var response = await _authService.RevokeTokenAsync(refreshToken);
            return Ok();
        }
    }
}
