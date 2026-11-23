using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for an individual quality rule reference value.
/// Child of QualityRuleImplementationConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Quality")]
public sealed partial class QualityRuleReferenceValueConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reference value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    // Why [NotStored]: the description is kept in the descriptions table, in the language of the caller's tenant (FDW-6208), so the column is gone and no read selects it and no save writes it; the
    // provider fills the member on every read that composes the record. The member stays, so every caller of Description still gets the text.
    [NotStored]
    public string? Description { get; set; }
}
