using System.Net;

namespace Lafie.Core.Exceptions;

/// <summary>Exception applicative de base portant un code HTTP et des erreurs.</summary>
public class CustomException(
    string message,
    IEnumerable<string>? errors = null,
    HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : Exception(message)
{
    public IEnumerable<string>? Errors { get; } = errors;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
