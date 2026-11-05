using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Tags.Configuration;

/// <summary>
/// Database-backed configuration for a tag. Maps to <c>tag.TagConfiguration</c>, keyed to its domain row in <c>tag.Tag</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Tag")]
public partial class TagImplementationConfiguration : ITagImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row.</remarks>
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

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>tag.Tag</c>, never a column of <c>tag.TagConfiguration</c>.</remarks>
    public string OwnerKind { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>tag.Tag</c>, never a column of <c>tag.TagConfiguration</c>.</remarks>
    public Guid OwnerId { get; set; }
}
