using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The columns every calculation input carries, whichever implementation owns it.
/// Each input declaration names where a calculation reads a value from.
/// </summary>
/// <remarks>
/// Why a base and not one shared type: an input belongs to the implementation that reads it, so each
/// implementation keeps its inputs in its own table, held by its RowId (FDW-944). A typed child's table
/// is its type's ConfigurationCommand container, so each implementation has its own input type
/// deriving from this one.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CalculationInputConfigurationBase : IGenericConfiguration
{
    /// <summary>Gets or sets the logical identity of this input.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the input name (alias). Mirrors <see cref="InputAlias"/> to satisfy IGenericConfiguration.</summary>
    public string Name { get => InputAlias; set => InputAlias = value; }

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

    /// <summary>Creates an implementation's input from the input a caller declared.</summary>
    /// <typeparam name="TInput">The owning implementation's input type.</typeparam>
    /// <param name="input">The declared input.</param>
    /// <param name="ordinal">Its position among the calculation's inputs.</param>
    /// <returns>The input, current and not deleted.</returns>
    public static TInput From<TInput>(CalculationInput input, int ordinal)
        where TInput : CalculationInputConfigurationBase, new()
        => new()
        {
            InputAlias = input.InputAlias,
            InputKind = input.Kind.Name,
            DataSetName = input.DataSetName,
            ConnectionName = input.ConnectionName,
            ContainerPath = input.ContainerPath,
            ScalarValueTypeName = input.ScalarValue?.ValueType?.Name,
            ScalarValue = input.ScalarValue?.SerializedValue,
            Ordinal = ordinal,
            IsCurrent = true,
            IsDeleted = false,
        };
}
