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
using Microsoft.EntityFrameworkCore;
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


        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, ApplicationDbContext context,
            IOptions<JwtOptions> jwt)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _jwt = jwt.Value;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
        {
            await ValidateUserRegistrationAsync(registerDto);
            // if validation does not throw, proceed with registration 
            var registeredUser = CreateUserEntity(registerDto);
            SaveUserToDatabase(registeredUser);
            return GenerateToken(registeredUser);
        }

        // create a JWT token for the registered user
        private AuthResponseDTO GenerateToken(User user)
        {
            string jwtToken = GenerateJwtSecurityToken(user);
            RefreshToken refereshToken = GenerateRefreshToken();
            SaveNewRefreshToken(user.Id, refereshToken);
            return new AuthResponseDTO
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
            bool isExist = _context.Users.Any(u => u.Id == userId);
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

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
        {
            // check if user exists 
            var user = await ValidateLoginAsync(loginDto);
            // if user exists, generate a JWT token and refresh token
            return await GenerateTokenForLoggedInUser(user);
        }

        private async Task<AuthResponseDTO> GenerateTokenForLoggedInUser(User user)
        {
            string jwtToken = GenerateJwtSecurityToken(user);
            var refreshToken = await GetActiveRefreshToken(user);
            return new AuthResponseDTO
            {
                Message = "User logged in successfully",
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresOn = refreshToken.ExpiresOn,
                IsAuthenticated = true,
                Email = user.Email,
            };
        }

        public async Task<RefreshToken> GetActiveRefreshToken(User user)
        {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(r =>
                r.UserId == user.Id && r.ExpiresOn > DateTime.UtcNow && r.RevokedOn == null);
            return refreshToken is not null ? refreshToken : CreateAndSaveNewRefreshToken(user.Id);
        }

        private RefreshToken CreateAndSaveNewRefreshToken(int userId)
        {
            var newRefreshToken = GenerateRefreshToken();
            SaveNewRefreshToken(userId, newRefreshToken);
            return newRefreshToken;
        }

        private async Task<User> ValidateLoginAsync(LoginDTO loginDto)
        {
            // check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user is null)
                throw new AuthenticationException("Invalid email or password.");

            // check if password is correct
            var passwordVerificationResult = _hasher.VerifyHashedPassword(user, user.Password, loginDto.Password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
                throw new AuthenticationException("Invalid email or password.");

            // if user exists and password is correct, return the user
            return user;
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            // validate the refresh token
            RefreshToken? token = await ValidateRefreshTokenAsync(refreshToken);

            // find the user associated with the refresh token
            User? user = await FindUserByRefreshToken(token);

            // revoke the old refresh token
            await RevokeRefreshTokenAsync(token);

            // generate a new JWT token and refresh token
            return GenerateTokenForRefreshedUser(user);
        }

        private AuthResponseDTO GenerateTokenForRefreshedUser(User? user)
        {
            // generate a new JWT token and refresh token
            string jwtToken = GenerateJwtSecurityToken(user);
            var newRefreshToken = GenerateRefreshToken();
            SaveNewRefreshToken(user.Id, newRefreshToken);
            return new AuthResponseDTO
            {
                Message = "Token refreshed successfully",
                Token = jwtToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresOn = newRefreshToken.ExpiresOn,
                IsAuthenticated = true,
                Email = user.Email
            };
        }

        private async Task RevokeRefreshTokenAsync(RefreshToken? token)
        {
            // revoke the old refresh token
            token.RevokedOn = DateTime.UtcNow;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        private async Task<User?> FindUserByRefreshToken(RefreshToken? token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
            if (user is null)
                throw new KeyNotFoundException("User not found.");
            return user;
        }

        private async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                            .FirstOrDefaultAsync(r => r.Token == refreshToken && r.ExpiresOn > DateTime.UtcNow && r.RevokedOn == null);
            if (token is null)
                throw new AuthenticationException("Invalid or expired refresh token.");
            return token;
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {

            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken &&
            t.RevokedOn == null);

            if (token is null || token.IsExpired)
                throw new AuthenticationException("Token already revoked or not found.");

            token.RevokedOn = DateTime.UtcNow;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}