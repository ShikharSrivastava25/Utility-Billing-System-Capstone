using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UtilityBillingSystem.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = new
                {
                    message = "",
                    statusCode = 0
                }
            };

            // Handle specific exception types
            switch (exception)
            {
                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new
                    {
                        error = new
                        {
                            message = exception.Message,
                            statusCode = context.Response.StatusCode
                        }
                    };
                    break;

                case InvalidOperationException:
                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        error = new
                        {
                            message = exception.Message,
                            statusCode = context.Response.StatusCode
                        }
                    };
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new
                    {
                        error = new
                        {
                            message = exception.Message,
                            statusCode = context.Response.StatusCode
                        }
                    };
                    break;

                case DbUpdateException dbEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var dbMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                    response = new
                    {
                        error = new
                        {
                            message = $"Database error: {dbMessage}",
                            statusCode = context.Response.StatusCode
                        }
                    };
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new
                    {
                        error = new
                        {
                            message = "An error occurred while processing your request.",
                            statusCode = context.Response.StatusCode
                        }
                    };
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}


