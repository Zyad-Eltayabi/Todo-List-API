using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Todo_List_API.DTOs;
using Todo_List_API.Models;

namespace Todo_List_API.Validations
{
    public class RegisterValidator : AbstractValidator<RegisterDTO>
    {
        private readonly ApplicationDbContext _context;
        public RegisterValidator(ApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MustAsync(async (Email, CancellationToken) =>
            {
                return await EmailExistsAsync(Email);
            }).WithMessage("Email already exists.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one digit.")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
