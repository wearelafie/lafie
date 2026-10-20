using Lafie.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Lafie.Api;

/// <summary>Traduit les <see cref="CustomException"/> (NotFound/Forbidden/…) en ProblemDetails avec le bon code HTTP.</summary>
public sealed class CustomExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not CustomException custom) return false;

        httpContext.Response.StatusCode = (int)custom.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = custom,
            ProblemDetails =
            {
                Status = (int)custom.StatusCode,
                Title = custom.Message,
                Detail = custom.Errors is null ? null : string.Join("; ", custom.Errors),
            },
        });
    }
}
