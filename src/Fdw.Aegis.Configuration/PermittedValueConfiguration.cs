using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// One value a declared parameter permits.
/// </summary>
/// <remarks>
/// A child of <see cref="ParameterAllowEntryConfiguration"/>, hanging from it by
/// <see cref="ParameterAllowEntryId"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class PermittedValueConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this value.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical FK to the owning <see cref="ParameterAllowEntryConfiguration"/>.</summary>
    public Guid ParameterAllowEntryId { get; set; }

    /// <summary>Gets or sets the permitted value.</summary>
    public string Value { get; set; } = string.Empty;
}
