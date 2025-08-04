using Microsoft.EntityFrameworkCore;
using Todo_List_API.Extensions;
using Todo_List_API.Interfaces;
using Todo_List_API.Middlewares;
using Todo_List_API.Models;
using Todo_List_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(SwaggerExtensions.Options());
builder.Services.AddExceptionHandler<GlobalErrorHandling>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseSqlServer(connectionString);
});
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToDoService, ToDoService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(SwaggerExtensions.UiOptions());
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler(op => { });
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();