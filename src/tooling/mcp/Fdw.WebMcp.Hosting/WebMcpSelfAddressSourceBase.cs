using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// A source of a host's own address (FDW-5721): it checks what the WebMcp row states for it when the row
/// is read, and gives the address once the host is running.
/// </summary>
public abstract class WebMcpSelfAddressSourceBase : TypeOptionBase<WebMcpSelfAddressSourceBase>
{
    /// <summary>Initializes the Empty/NotFound sentinel (source-generated).</summary>
    protected WebMcpSelfAddressSourceBase()
        : base(string.Empty)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WebMcpSelfAddressSourceBase"/> class.</summary>
    /// <param name="name">The source's name, as a WebMcp row's SelfAddressSource states it.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">Where the address comes from.</param>
    protected WebMcpSelfAddressSourceBase(string name, string displayName, string description)
        : base(name, $"WebMcpSelfAddress:{name}", displayName, description, "WebMcp")
    {
    }

    /// <summary>Gets whether this is the Empty/NotFound sentinel.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>
    /// Checks what the row states beside this source, when the row is read: the address the source reads
    /// from the row, if any, is stated and well formed, and nothing the source does not read is stated.
    /// </summary>
    /// <param name="configurationName">The WebMcp row's name, for the messages.</param>
    /// <param name="selfBaseAddress">The row's SelfBaseAddress, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The address the row states for this source, or <see langword="null"/> when the source reads none from the row; or the refusal.</returns>
    public IGenericResult<Uri?> Check(string configurationName, string? selfBaseAddress, ILogger logger)
        => CheckRow(configurationName, selfBaseAddress, logger);

    /// <summary>Gives the host's own address, once the host is running.</summary>
    /// <param name="settings">The host's WebMCP settings, as the row was read.</param>
    /// <param name="server">The host's server.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The address tool calls are sent to, or the refusal naming why there is none.</returns>
    public IGenericResult<Uri> Resolve(WebMcpHostSettings settings, IServer server, ILogger logger)
        => ResolveAddress(settings, server, logger);

    /// <summary>The source's own check of the row.</summary>
    /// <param name="configurationName">The WebMcp row's name, for the messages.</param>
    /// <param name="selfBaseAddress">The row's SelfBaseAddress, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The address the row states for this source, or <see langword="null"/>; or the refusal.</returns>
    /// <remarks>
    /// Virtual and refusing rather than abstract: the generated NotFound sentinel overrides abstract members
    /// as public, and this one is protected. Every source overrides it; the sentinel is refused as a source no
    /// option answers to.
    /// </remarks>
    protected virtual IGenericResult<Uri?> CheckRow(string configurationName, string? selfBaseAddress, ILogger logger)
        => GenericResult<Uri?>.Failure(WebMcpLog.SelfAddressSourceUnknown(logger, configurationName, Name));

    /// <summary>The source's own step: gives the address once the host is running.</summary>
    /// <param name="settings">The host's WebMCP settings, as the row was read.</param>
    /// <param name="server">The host's server.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The address, or the refusal.</returns>
    /// <remarks>Virtual and refusing rather than abstract, for the same reason as <see cref="CheckRow"/>.</remarks>
    protected virtual IGenericResult<Uri> ResolveAddress(WebMcpHostSettings settings, IServer server, ILogger logger)
        => GenericResult<Uri>.Failure(WebMcpLog.SelfAddressSourceUnknown(logger, WebMcpHosting.ConfigurationName, Name));

    /// <summary>Refuses a SelfBaseAddress stated beside a source that does not read it.</summary>
    /// <param name="configurationName">The WebMcp row's name, for the messages.</param>
    /// <param name="selfBaseAddress">The row's SelfBaseAddress, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>Success with no address when none is stated; otherwise the refusal.</returns>
    protected IGenericResult<Uri?> NoStatedAddress(string configurationName, string? selfBaseAddress, ILogger logger)
        => string.IsNullOrWhiteSpace(selfBaseAddress)
            ? GenericResult<Uri?>.Success(null)
            : GenericResult<Uri?>.Failure(WebMcpLog.SelfBaseAddressNotRead(logger, configurationName, Name, selfBaseAddress));
}
