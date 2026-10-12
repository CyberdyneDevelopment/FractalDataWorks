using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// Windows Integrated Authentication — uses current Windows identity.
/// No KVP keys required.
/// </summary>
[TypeOption(typeof(MsSqlAuthenticationTypes), "WindowsAuth")]
public sealed class WindowsAuthConfiguration : MsSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="WindowsAuthConfiguration"/> class.</summary>
    public WindowsAuthConfiguration()
        : base("WindowsAuth",
               "Windows Authentication",
               "Windows Integrated Security (Trusted Connection)",
               [], [], [], requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success("Integrated Security=True;");

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>Integrated Security signs in with the Windows or Kerberos identity of the process the host runs as.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => true;
}
