using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Todo_List_API.DTOs;
using Todo_List_API.Helpers;
using Todo_List_API.Interfaces;
using Todo_List_API.Models;
using Todo_List_API.Validations;

namespace Todo_List_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly JwtOptions _jwt;


        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, ApplicationDbContext context, IOptions<JwtOptions> jwt)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _jwt = jwt.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDTO registerDto)
        {
            await ValidateUserRegistrationAsync(registerDto);
            // if validation does not throw, proceed with registration 
            var registeredUser = CreateUserEntity(registerDto);
            SaveUserToDatabase(registeredUser);
            return GenerateToken(registeredUser);
        }

        // create a JWT token for the registered user
        private AuthResponseDto GenerateToken(User user)
        {
            string jwtToken = GenerateJwtSecurityToken(user);
            RefreshToken refereshToken = GenerateRefreshToken();
            SaveNewRefreshToken(user.Id, refereshToken);
            return new AuthResponseDto
            {
                Message = "User registered successfully",
                Token = jwtToken,
                IsAuthenticated = true,
                Email = user.Email,
                RefreshToken = refereshToken.Token,
                RefreshTokenExpiresOn = refereshToken.ExpiresOn
            };
        }
        private RefreshToken GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationInDays),
                CreatedOn = DateTime.UtcNow
            };
        }

        private void SaveNewRefreshToken(int userId, RefreshToken token)
        {
            // check if user exists
            bool isExist = _context.Users.Any(u =>  u.Id == userId);
            if (!isExist)
                throw new KeyNotFoundException("User does not exist");
            token.UserId = userId;
            _context.RefreshTokens.Add(token);
            _context.SaveChanges();
        }
        private string GenerateJwtSecurityToken(User user)
        {
            var claims = new[]
                         {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", "User"),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationInMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private User CreateUserEntity(RegisterDTO registerDto)
        {
            // create a new user 
            return new User
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                Password = _hasher.HashPassword(null, registerDto.Password),
            };
        }

        private void SaveUserToDatabase(User user)
        {
            // save the user to the database
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        private async Task ValidateUserRegistrationAsync(RegisterDTO registerDto)
        {
            var validator = new RegisterValidator(_context);
            var validationResult = await validator.ValidateAsync(registerDto);
            if (!validationResult.IsValid)
            {
                var message = string.Join(" | ", validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

                throw new ArgumentException(message);
            }
        }
    }
}
