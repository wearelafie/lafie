namespace Lafie.Core.Context;

/// <summary>Métadonnées de requête HTTP sans dépendance à ASP.NET Core (audit/log).</summary>
public interface IRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
}
