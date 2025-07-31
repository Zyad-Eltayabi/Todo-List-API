using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Todo_List_API.Extensions;

public static class SwaggerExtensions
{
    public static Action<SwaggerGenOptions> Options()
    {
        return options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Todo List API",
                Description = "An ASP.NET Core Web API for a Todo List Application with Swagger UI",
                Contact = new OpenApiContact
                {
                    Name = "Zyad Eltayibi",
                    Email = "ZyadEltayibi@gmail.com",
                    Url = new Uri("https://github.com/Zyad-Eltayabi")
                },
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter only your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        };
    }
    public static Action<SwaggerUIOptions> UiOptions()
    {
        return options =>
        {
            options.DocumentTitle = "Todo List API";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo List API");
            options.RoutePrefix = "swagger";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.ShowCommonExtensions();
        };
    }
}