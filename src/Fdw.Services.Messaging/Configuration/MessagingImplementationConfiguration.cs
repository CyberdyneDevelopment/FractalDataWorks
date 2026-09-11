using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Services.Messaging.Abstractions;

namespace Fdw.Services.Messaging.Configuration;

/// <summary>Where this deployment keeps its messages.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class MessagingImplementationConfiguration : IMessagingImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid MessagingId { get; set; }


    /// <inheritdoc/>
    public string? DataStoreName { get; set; }

    /// <inheritdoc/>
    public string? PathName { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }
}
