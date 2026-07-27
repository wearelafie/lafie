using System.Net;

namespace Lafie.Core.Exceptions;

public sealed class ForbiddenException(string message)
    : CustomException(message, null, HttpStatusCode.Forbidden);
