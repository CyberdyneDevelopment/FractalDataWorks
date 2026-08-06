namespace Fdw.Services.Credentials.Sql.Options;

/// <summary>
/// App-wiring options for the SQL credential package — a single POINTER selecting which configured
/// credential service this app uses.
/// </summary>
/// <remarks>
/// <para>
/// This carries NO policy. The credential policy (vault name, secret manager + HMAC key name,
/// environment, per-user token limit) lives entirely in the typed <c>sec.SqlCredentialService</c>
/// configuration row. This options class holds ONLY the selector — the name of the credential
/// service to resolve — exactly the same species as <c>Users:CredentialServiceName</c> and the
/// connections→secret-managers exemplar (a consumer's own configuration carries the dependency's
/// name; the consumer calls <c>provider.Get(name)</c>).
/// </para>
/// </remarks>
public sealed class CredentialsSqlOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "CredentialsSql";

    /// <summary>
    /// Gets or sets the name of the credential service whose vault and PAT policy back the SQL
    /// PAT and agent-key services.
    /// </summary>
    // Why: No default value — a missing or blank name must fail loud on first credential operation,
    // never silently resolve to the wrong service or skip credential storage. Matches the no-fallback
    // rule and the existing UsersServiceOptions.CredentialServiceName behaviour.
    public string? CredentialServiceName { get; set; }
}
