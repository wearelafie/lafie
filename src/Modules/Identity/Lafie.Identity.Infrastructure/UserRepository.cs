using Dapper;
using Lafie.Identity.Application.Users;
using Lafie.Identity.Domain;
using Lafie.Persistence;

namespace Lafie.Identity.Infrastructure;

/// <summary>Persistance Dapper des comptes utilisateurs (schéma `identity`).</summary>
public sealed class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private const string Columns = """
        id as "Id", tenant_id as "TenantId", username as "Username", email as "Email", email_verified as "EmailVerified",
        phone as "Phone", phone_verified as "PhoneVerified", password_hash as "PasswordHash", password_algo as "PasswordAlgo",
        password_changed_on_utc as "PasswordChangedOnUtc", must_change_password as "MustChangePassword",
        auth_provider as "AuthProvider", external_issuer as "ExternalIssuer", external_subject as "ExternalSubject",
        mfa_enabled as "MfaEnabled", mfa_type as "MfaType", user_type as "UserType", status as "Status",
        failed_login_count as "FailedLoginCount", locked_until_utc as "LockedUntilUtc", last_login_on_utc as "LastLoginOnUtc",
        last_login_ip as "LastLoginIp", first_name as "FirstName", last_name as "LastName", title as "Title",
        display_name as "DisplayName", avatar_url as "AvatarUrl", preferred_language as "PreferredLanguage", time_zone as "TimeZone",
        practitioner_id as "PractitionerId", employee_id as "EmployeeId",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task<User?> FindByLoginAsync(string emailOrUsername, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from identity.users where lower(email) = lower(@login) or lower(username) = lower(@login)";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { login = emailOrUsername }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from identity.users where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into identity.users
                (id, tenant_id, username, email, email_verified, phone, phone_verified, password_hash, password_algo,
                 password_changed_on_utc, must_change_password, auth_provider, external_issuer, external_subject, mfa_enabled,
                 mfa_type, user_type, status, failed_login_count, locked_until_utc, last_login_on_utc, last_login_ip,
                 first_name, last_name, title, display_name, avatar_url, preferred_language, time_zone,
                 practitioner_id, employee_id, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @TenantId, @Username, @Email, @EmailVerified, @Phone, @PhoneVerified, @PasswordHash, @PasswordAlgo,
                 @PasswordChangedOnUtc, @MustChangePassword, @AuthProvider, @ExternalIssuer, @ExternalSubject, @MfaEnabled,
                 @MfaType, @UserType, @Status, @FailedLoginCount, @LockedUntilUtc, @LastLoginOnUtc, @LastLoginIp,
                 @FirstName, @LastName, @Title, @DisplayName, @AvatarUrl, @PreferredLanguage, @TimeZone,
                 @PractitionerId, @EmployeeId, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(user), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update identity.users set
                username = @Username, email = @Email, email_verified = @EmailVerified, phone = @Phone, phone_verified = @PhoneVerified,
                password_hash = @PasswordHash, password_algo = @PasswordAlgo, password_changed_on_utc = @PasswordChangedOnUtc,
                must_change_password = @MustChangePassword, auth_provider = @AuthProvider, external_issuer = @ExternalIssuer,
                external_subject = @ExternalSubject, mfa_enabled = @MfaEnabled, mfa_type = @MfaType, user_type = @UserType,
                status = @Status, failed_login_count = @FailedLoginCount, locked_until_utc = @LockedUntilUtc,
                last_login_on_utc = @LastLoginOnUtc, last_login_ip = @LastLoginIp, first_name = @FirstName, last_name = @LastName,
                title = @Title, display_name = @DisplayName, avatar_url = @AvatarUrl, preferred_language = @PreferredLanguage,
                time_zone = @TimeZone, practitioner_id = @PractitionerId, employee_id = @EmployeeId,
                updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(user), cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(string email, string username, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from identity.users where lower(email) = lower(@email) or lower(username) = lower(@username))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { email, username }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = "select r.code from identity.user_roles ur join identity.roles r on r.id = ur.role_id where ur.user_id = @userId order by r.code";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<string>(new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select distinct rp.permission_code
            from identity.user_roles ur
            join identity.role_permissions rp on rp.role_id = ur.role_id
            where ur.user_id = @userId
            order by rp.permission_code
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<string>(new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task AssignRoleAsync(Guid userId, string roleCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into identity.user_roles (user_id, role_id, scope, created_on_utc)
            select @userId, r.id, '', now() from identity.roles r where r.code = @roleCode and r.tenant_id is null
            on conflict (user_id, role_id, scope) do nothing
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { userId, roleCode }, cancellationToken: cancellationToken));
    }

    private static object ToParams(User u) => new
    {
        u.Id, u.TenantId, u.Username, u.Email, u.EmailVerified, u.Phone, u.PhoneVerified, u.PasswordHash, u.PasswordAlgo,
        u.PasswordChangedOnUtc, u.MustChangePassword, AuthProvider = u.AuthProvider.ToString(), u.ExternalIssuer, u.ExternalSubject,
        u.MfaEnabled, u.MfaType, UserType = u.UserType.ToString(), Status = u.Status.ToString(), u.FailedLoginCount,
        u.LockedUntilUtc, u.LastLoginOnUtc, u.LastLoginIp, u.FirstName, u.LastName, u.Title, u.DisplayName, u.AvatarUrl,
        u.PreferredLanguage, u.TimeZone, u.PractitionerId, u.EmployeeId, u.CreatedOnUtc, u.UpdatedOnUtc, u.ArchivedOnUtc,
    };

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string? TenantId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool EmailVerified { get; init; }
        public string? Phone { get; init; }
        public bool PhoneVerified { get; init; }
        public string? PasswordHash { get; init; }
        public string? PasswordAlgo { get; init; }
        public DateTime? PasswordChangedOnUtc { get; init; }
        public bool MustChangePassword { get; init; }
        public string AuthProvider { get; init; } = "Local";
        public string? ExternalIssuer { get; init; }
        public string? ExternalSubject { get; init; }
        public bool MfaEnabled { get; init; }
        public string? MfaType { get; init; }
        public string UserType { get; init; } = "Staff";
        public string Status { get; init; } = "Active";
        public int FailedLoginCount { get; init; }
        public DateTime? LockedUntilUtc { get; init; }
        public DateTime? LastLoginOnUtc { get; init; }
        public string? LastLoginIp { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Title { get; init; }
        public string? DisplayName { get; init; }
        public string? AvatarUrl { get; init; }
        public string? PreferredLanguage { get; init; }
        public string? TimeZone { get; init; }
        public Guid? PractitionerId { get; init; }
        public Guid? EmployeeId { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime? UpdatedOnUtc { get; init; }
        public DateTime? ArchivedOnUtc { get; init; }

        private static DateTimeOffset? Off(DateTime? v) => v is { } d ? new DateTimeOffset(DateTime.SpecifyKind(d, DateTimeKind.Utc), TimeSpan.Zero) : null;

        public User ToDomain() => User.Rehydrate(
            Id, TenantId, Username, Email, EmailVerified, Phone, PhoneVerified, PasswordHash, PasswordAlgo,
            Off(PasswordChangedOnUtc), MustChangePassword, Enum.Parse<Lafie.Identity.Domain.AuthProvider>(AuthProvider),
            ExternalIssuer, ExternalSubject, MfaEnabled, MfaType, Enum.Parse<Lafie.Identity.Domain.UserType>(UserType),
            Enum.Parse<Lafie.Identity.Domain.UserStatus>(Status), FailedLoginCount, Off(LockedUntilUtc), Off(LastLoginOnUtc), LastLoginIp,
            FirstName, LastName, Title, DisplayName, AvatarUrl, PreferredLanguage, TimeZone, PractitionerId, EmployeeId,
            Off(CreatedOnUtc)!.Value, Off(UpdatedOnUtc), Off(ArchivedOnUtc));
    }
}
