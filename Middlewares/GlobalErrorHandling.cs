using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;

namespace Todo_List_API.Middlewares
{
    public class GlobalErrorHandling : IExceptionHandler
    {
        private readonly ILogger<GlobalErrorHandling> _logger;
        private readonly IHostEnvironment _env;

        public GlobalErrorHandling(ILogger<GlobalErrorHandling> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Log the exception details with stack trace and message at Error level
            _logger.LogCritical("Unhandled exception occurred. Message: {Message} || FullException: {Exception}", exception.Message, exception.ToString());

            // set default ProblemDetails object 
            var problemDetails = new ProblemDetails
            {
                // Set the HTTP status code
                Status = (int)HttpStatusCode.InternalServerError, 
                // Default error title
                Title = "An error occurred while processing your request.", 
                // Default client-friendly message
                Detail = "An unexpected error occurred. Please try again later.",
                // Link to the HTTP status code documentation
                Type = "https://httpstatuses.com/500",
                // Set the instance to the current request path for context
                Instance = httpContext.Request.Path,
            };
            
            if (_env.IsDevelopment())
            {
                // Provide detailed exception message for easier debugging
                problemDetails.Detail = exception.Message;
            }
            // Use a switch statement to handle specific exception types differently
            // Customize based on exception type
            switch (exception)
            {
                case ArgumentException argEx:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = argEx.Message;
                    problemDetails.Type = $"https://httpstatuses.com/{problemDetails.Status}";
                    break;

                case KeyNotFoundException argEx:
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Title = "Not Found";
                    problemDetails.Detail = argEx.Message;
                    problemDetails.Type = "https://httpstatuses.com/404";
                    break;

                case AuthenticationException e:
                    problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Title = "Unauthorized";
                    problemDetails.Detail = e.Message;
                    problemDetails.Type = "https://httpstatuses.com/401";
                    break;

                case UnauthorizedAccessException e:
                    problemDetails.Status = (int)HttpStatusCode.Forbidden;
                    problemDetails.Title = "Forbidden";
                    problemDetails.Detail = e.Message;
                    problemDetails.Type = "https://httpstatuses.com/403";
                    break;

                case InvalidOperationException ex:
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = ex.Message;
                    problemDetails.Type = "https://httpstatuses.com/409";
                    break;

                case SqlException:
                case DbUpdateException:
                    problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                    problemDetails.Title = "Database Error";
                    problemDetails.Detail = "Database error occurred.";
                    problemDetails.Type = "https://httpstatuses.com/503";
                    break;

                case NotImplementedException:
                    problemDetails.Status = (int)HttpStatusCode.NotImplemented;
                    problemDetails.Title = "Not Implemented";
                    problemDetails.Detail = "Functionality not implemented.";
                    problemDetails.Type = "https://httpstatuses.com/501";
                    break;

                case TimeoutException:
                    problemDetails.Status = (int)HttpStatusCode.RequestTimeout;
                    problemDetails.Title = "Timeout";
                    problemDetails.Detail = "The request timed out.";
                    problemDetails.Type = "https://httpstatuses.com/408";
                    break;
            }
            httpContext.Response.StatusCode = problemDetails.Status ?? 500;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
