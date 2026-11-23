using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Descriptions.Configuration;

/// <summary>
/// Database-backed configuration for a description. Maps to <c>catalog.DescriptionConfiguration</c>, keyed to its domain row in <c>catalog.Description</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Description")]
public partial class DescriptionImplementationConfiguration : IDescriptionImplementationConfiguration
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
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public string DescriptionType { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public string DescriptionSubType { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public Guid ThingId { get; set; }

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public string Language { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public int Ordinal { get; set; }

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public string ThingName { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public string? OwnerName { get; set; }

    /// <inheritdoc/>
    /// <remarks>A list field of the domain row: set on a save and copied onto <c>catalog.Description</c>, never a column of <c>catalog.DescriptionConfiguration</c>.</remarks>
    public Guid? OwnerId { get; set; }
}
