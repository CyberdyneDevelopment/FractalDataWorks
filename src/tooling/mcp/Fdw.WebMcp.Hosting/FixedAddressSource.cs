using System;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// The host's own address is the row's SelfBaseAddress (FDW-5721): for a host that reaches itself through
/// an address other than the one it listens on, such as a host behind a proxy.
/// </summary>
/// <remarks>
/// An instance that runs on more than one port (A/B slots) must not use it: every instance would call the
/// one address the release carries. <see cref="ListeningAddressSource"/> is the source for those.
/// </remarks>
[TypeOption(typeof(WebMcpSelfAddressSources), "FixedAddress")]
public sealed class FixedAddressSource : WebMcpSelfAddressSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="FixedAddressSource"/> class.</summary>
    public FixedAddressSource()
        : base("FixedAddress", "Fixed address", "The absolute http or https address the WebMcp row states in SelfBaseAddress")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<Uri?> CheckRow(string configurationName, string? selfBaseAddress, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(selfBaseAddress))
        {
            return GenericResult<Uri?>.Failure(WebMcpLog.FixedSelfAddressMissing(logger, configurationName));
        }

        return Uri.TryCreate(selfBaseAddress, UriKind.Absolute, out var address) && WebMcpListeningAddresses.IsHttp(address)
            ? GenericResult<Uri?>.Success(address)
            : GenericResult<Uri?>.Failure(WebMcpLog.ConfigurationInvalid(logger, configurationName,
                FormattableString.Invariant($"SelfBaseAddress '{selfBaseAddress}' is not an absolute http or https address")));
    }

    /// <inheritdoc/>
    /// <remarks>The address was checked when the row was read; a settings record without one did not come from this source's check.</remarks>
    protected override IGenericResult<Uri> ResolveAddress(WebMcpHostSettings settings, IServer server, ILogger logger)
        => settings.SelfBaseAddress is { } address
            ? GenericResult<Uri>.Success(address)
            : GenericResult<Uri>.Failure(WebMcpLog.FixedSelfAddressMissing(logger, WebMcpHosting.ConfigurationName));
}
