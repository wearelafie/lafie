using System.Net;

namespace Lafie.Core.Exceptions;

public sealed class NotFoundException(string message)
    : CustomException(message, null, HttpStatusCode.NotFound);
