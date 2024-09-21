using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Default;
using MotoRent.Infrastructure.Exceptions;
using MotoRent.MessageConsumers.Services;
using System.Net;
using System.Text.Json;

namespace MotoRent.Application.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IMessageService _messageService;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IMessageService messageService)
        {
            _next = next;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);

                    if (context.Response.StatusCode == 400 && context.Response.ContentType != null && context.Response.ContentType.Contains("application/problem+json"))
                    {
                        await HandleValidationErrorResponse(context);
                    }
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(context, ex);
                }
                finally
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }

        private async Task HandleValidationErrorResponse(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var problemDetailsJson = await reader.ReadToEndAsync();

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(problemDetailsJson);

            if (problemDetails?.Extensions != null &&
                problemDetails.Extensions.TryGetValue("errors", out var errorsObj) &&
                errorsObj is JsonElement errorsElement)
            {
                var errors = new List<string>();
                foreach (var errorProperty in errorsElement.EnumerateObject())
                {
                    if (errorProperty.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var errorMessage in errorProperty.Value.EnumerateArray())
                        {
                            errors.Add(errorMessage.GetString());
                        }
                    }
                }

                var errorResponse = new ErrorResponseDto
                {
                    Message = string.Join(" ", errors)
                };

                context.Response.Body.SetLength(0);
                await WriteErrorResponse(context, errorResponse, HttpStatusCode.BadRequest);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = new ErrorResponseDto
            {
                Message = "An error occurred while processing your request."
            };

            var statusCode = HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException validationException:
                    errorResponse.Message = string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage));
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case ArgumentException:
                    errorResponse.Message = exception.Message;
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case NotFoundException:
                    errorResponse.Message = exception.Message;
                    statusCode = HttpStatusCode.NotFound;
                    break;
                default:
                    _logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            await WriteErrorResponse(context, errorResponse, statusCode);

            // Log the error to RabbitMQ
            await _messageService.PublishErrorLogAsync($"Error: {errorResponse.Message}");
        }

        private async Task WriteErrorResponse(HttpContext context, ErrorResponseDto errorResponse, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(result);
        }
    }


}
