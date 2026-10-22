namespace Lafie.Organization.Application;

/// <summary>Critères de recherche/pagination des établissements.</summary>
public sealed record OrganizationFilter(
    string? Search = null,
    string? FacilityTypeCode = null,
    string? OperationalStatus = null,
    bool IncludeArchived = false,
    int Page = 1,
    int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}
