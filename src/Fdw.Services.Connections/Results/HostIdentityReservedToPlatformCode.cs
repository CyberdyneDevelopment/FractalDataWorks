using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection that presents the host's own identity was refused because the caller is not a platform
/// administrator (FDW-1068).
/// </summary>
/// <remarks>
/// A connection that acts as the host (Windows or Kerberos sign-in, a managed identity, the host's file system)
/// reaches whatever the host reaches, including the platform's own databases. Creating or changing one is a
/// platform administrator's decision. The reason names the authentication the connection uses and what to use
/// instead, so the caller can correct the request. A validation code, because only those carry their reason to the
/// caller (FDW-1046). HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "HostIdentityReservedToPlatform", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HostIdentityReservedToPlatformCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="HostIdentityReservedToPlatformCode"/> class.</summary>
    public HostIdentityReservedToPlatformCode()
        : base(20002, "HostIdentityReservedToPlatform",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' of type '{connectionType}' connects as the host's own identity; only a platform administrator may create or change it. Give it an authentication method that carries its own credential",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
