using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// Data record mapping for the <c>calc.CalculationEntityInput</c> table.
/// Each record represents a single input declaration for a calculation entity.
/// </summary>
/// <remarks>
/// Why: implements IGenericConfiguration so [GenerateMapper] emits a cascade child descriptor for the
/// parent <see cref="CalculationEntityConfiguration.Inputs"/> collection — the keystone base read
/// composes calc.CalculationEntityInput rows (matched by the child's ConfigurationCommand container name)
/// and the cascade-save persists them.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class CalculationEntityInputRecord : IGenericConfiguration
{
    /// <summary>Gets the configuration section name (computed; not a persisted column).</summary>
    public string SectionName => "CalculationEntityInputs";

    /// <summary>Gets the service type domain.</summary>
    public string ServiceType => "Calculation";

    /// <summary>Gets the service option type discriminator (none for inputs).</summary>
    public string? ServiceOptionType => null;


    /// <summary>Gets or sets the logical identity of this input.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the input name (alias). Mirrors <see cref="InputAlias"/> to satisfy IGenericConfiguration.</summary>
    // Why: IGenericConfiguration requires a settable Name; an input's identity is its alias.
    public string Name { get => InputAlias; set => InputAlias = value; }

    /// <summary>Gets or sets the parent calculation entity identifier.</summary>
    public Guid CalculationEntityId { get; set; }


    /// <summary>Gets or sets the alias used to reference this input in expressions.</summary>
    public string InputAlias { get; set; } = string.Empty;

    /// <summary>Gets or sets the input kind (e.g. "Scalar", "DataSet", "Container").</summary>
    public string InputKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the source DataSet name, if input kind is DataSet.</summary>
    public string? DataSetName { get; set; }

    /// <summary>Gets or sets the source connection name, if input kind is Container.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the container path within the connection.</summary>
    public string? ContainerPath { get; set; }

    /// <summary>Gets or sets the scalar value type name (e.g. "Decimal", "Int32").</summary>
    public string? ScalarValueTypeName { get; set; }

    /// <summary>Gets or sets the serialized scalar value.</summary>
    public string? ScalarValue { get; set; }

    /// <summary>Gets or sets the ordinal position of this input.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets the soft delete flag.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified this record.</summary>
    public string ModifyBy { get; set; } = string.Empty;
}
