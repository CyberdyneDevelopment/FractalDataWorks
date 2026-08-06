using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;

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
        : base(2, "WindowsAuth",
               "Windows Authentication",
               "Windows Integrated Security (Trusted Connection)",
               [], [], [])
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success("Integrated Security=True;");
}
