using System.Net;

namespace Lafie.Core.Exceptions;

public sealed class UnauthorizedException(string message)
    : CustomException(message, null, HttpStatusCode.Unauthorized);
