using Lafie.Core.Domain;

namespace Lafie.Identity.Domain;

/// <summary>Agrégat User : compte d'authentification/autorisation.</summary>
public sealed class User : AggregateRoot<Guid>
{
    public string? TenantId { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public bool EmailVerified { get; private set; }
    public string? Phone { get; private set; }
    public bool PhoneVerified { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PasswordAlgo { get; private set; }
    public DateTimeOffset? PasswordChangedOnUtc { get; private set; }
    public bool MustChangePassword { get; private set; }
    public AuthProvider AuthProvider { get; private set; }
    public string? ExternalIssuer { get; private set; }
    public string? ExternalSubject { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaType { get; private set; }
    public UserType UserType { get; private set; }
    public UserStatus Status { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public DateTimeOffset? LastLoginOnUtc { get; private set; }
    public string? LastLoginIp { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Title { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public string? TimeZone { get; private set; }
    public Guid? PractitionerId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }

    public bool IsLockedOut => LockedUntilUtc is { } u && u > DateTimeOffset.UtcNow;
    public bool CanLogin => Status == UserStatus.Active && !IsLockedOut;

#pragma warning disable CS8618 // Rehydrate/factory initialisent tous les champs.
    private User() { }
#pragma warning restore CS8618

    public static User Create(
        string username, string email, string passwordHash, string passwordAlgo, UserType userType,
        string? firstName = null, string? lastName = null, string? displayName = null,
        string? tenantId = null, string? preferredLanguage = "fr")
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = passwordHash,
            PasswordAlgo = passwordAlgo,
            PasswordChangedOnUtc = now,
            AuthProvider = AuthProvider.Local,
            UserType = userType,
            Status = UserStatus.Active,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName ?? $"{firstName} {lastName}".Trim(),
            PreferredLanguage = preferredLanguage,
            CreatedOnUtc = now,
        };
    }

    public void RecordSuccessfulLogin(string? ip)
    {
        LastLoginOnUtc = DateTimeOffset.UtcNow;
        LastLoginIp = ip;
        FailedLoginCount = 0;
        LockedUntilUtc = null;
        Touch();
    }

    /// <summary>Incrémente le compteur d'échecs et verrouille au-delà du seuil.</summary>
    public void RecordFailedLogin(int maxAttempts, TimeSpan lockDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockedUntilUtc = DateTimeOffset.UtcNow.Add(lockDuration);
            Status = UserStatus.Locked;
        }
        Touch();
    }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    public static User Rehydrate(
        Guid id, string? tenantId, string username, string email, bool emailVerified, string? phone, bool phoneVerified,
        string? passwordHash, string? passwordAlgo, DateTimeOffset? passwordChangedOnUtc, bool mustChangePassword,
        AuthProvider authProvider, string? externalIssuer, string? externalSubject, bool mfaEnabled, string? mfaType,
        UserType userType, UserStatus status, int failedLoginCount, DateTimeOffset? lockedUntilUtc,
        DateTimeOffset? lastLoginOnUtc, string? lastLoginIp, string? firstName, string? lastName, string? title,
        string? displayName, string? avatarUrl, string? preferredLanguage, string? timeZone,
        Guid? practitionerId, Guid? employeeId, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
        => new()
        {
            Id = id, TenantId = tenantId, Username = username, Email = email, EmailVerified = emailVerified,
            Phone = phone, PhoneVerified = phoneVerified, PasswordHash = passwordHash, PasswordAlgo = passwordAlgo,
            PasswordChangedOnUtc = passwordChangedOnUtc, MustChangePassword = mustChangePassword, AuthProvider = authProvider,
            ExternalIssuer = externalIssuer, ExternalSubject = externalSubject, MfaEnabled = mfaEnabled, MfaType = mfaType,
            UserType = userType, Status = status, FailedLoginCount = failedLoginCount, LockedUntilUtc = lockedUntilUtc,
            LastLoginOnUtc = lastLoginOnUtc, LastLoginIp = lastLoginIp, FirstName = firstName, LastName = lastName, Title = title,
            DisplayName = displayName, AvatarUrl = avatarUrl, PreferredLanguage = preferredLanguage, TimeZone = timeZone,
            PractitionerId = practitionerId, EmployeeId = employeeId, CreatedOnUtc = createdOnUtc, UpdatedOnUtc = updatedOnUtc,
            ArchivedOnUtc = archivedOnUtc,
        };
}
