using GatewayService.Contracts;
using GatewayService.Infrastructure;

namespace GatewayService.Api;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ValidationErrorResponse(
                "Validation failed", [new { field = "request", error = exception.Message }]));
        }
        catch (KeyNotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(exception.Message));
        }
        catch (DownstreamUnavailableException exception)
        {
            logger.LogWarning(exception, "A downstream service is unavailable");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(exception.Message));
        }
    }
}
