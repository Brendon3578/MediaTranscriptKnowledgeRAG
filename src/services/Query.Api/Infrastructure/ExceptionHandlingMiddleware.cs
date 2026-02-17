using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace Query.Api.Infrastructure
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var status = MapStatusCode(ex);
            var title = MapTitle(ex);
            var typeUri = $"https://httpstatuses.com/{status}";

            _logger.LogError(ex, "Unhandled exception for {Path} with status {Status}", context.Request.Path, status);

            var problem = new ProblemDetails
            {
                Title = title,
                Status = status,
                Detail = CreateSafeDetail(ex),
                Type = typeUri,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await context.Response.WriteAsync(json);
        }

        private static int MapStatusCode(Exception ex) =>
            ex switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                ExternalDependencyException => StatusCodes.Status503ServiceUnavailable,
                HttpRequestException => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

        private static string MapTitle(Exception ex) =>
            ex switch
            {
                ValidationException => "Validation error",
                NotFoundException => "Resource not found",
                ConflictException => "Conflict",
                BusinessRuleException => "Business rule violation",
                ExternalDependencyException => "Service unavailable",
                HttpRequestException => "Service unavailable",
                _ => "Unexpected error"
            };

        private string CreateSafeDetail(Exception ex)
        {
            if (_env.IsDevelopment())
            {
                return ex.Message;
            }

            return ex.Message;
        }
    }
}
