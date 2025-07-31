using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Todo_List_API.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<ToDo> ToDos { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ToDoTag> ToDoTags { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
}
