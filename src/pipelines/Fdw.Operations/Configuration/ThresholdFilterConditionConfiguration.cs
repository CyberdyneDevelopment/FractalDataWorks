using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>One condition of a rule's criteria (FDW-3936). Child of ThresholdFilterGroupConfiguration.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdFilterConditionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the value the subject reports that the condition compares; one of the threshold type's fields.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the operator: the name of an option of FilterOperators.</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Gets or sets the value compared with, as text in the invariant culture; none for an operator that takes no value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets how Value is read: Int64, Double, String, Boolean, TimeSpan or DateTimeOffset.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the place of the condition in its group, counted from 1; for display, the criteria do not depend on it.</summary>
    public int Ordinal { get; set; }
}
