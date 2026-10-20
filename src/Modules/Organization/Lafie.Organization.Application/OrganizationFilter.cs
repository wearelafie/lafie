namespace Lafie.Organization.Application;

/// <summary>Critères de recherche/pagination des organisations.</summary>
public sealed record OrganizationFilter(
    string? Search = null,
    string? Type = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20)
{
    public const int MaxPageSize = 100;

    /// <summary>Page normalisée (>= 1).</summary>
    public int SafePage => Page < 1 ? 1 : Page;

    /// <summary>Taille de page bornée (1..MaxPageSize).</summary>
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);

    public int Offset => (SafePage - 1) * SafePageSize;
}
