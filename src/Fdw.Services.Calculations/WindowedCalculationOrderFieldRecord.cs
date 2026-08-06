using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Calculations;

/// <summary>
/// Data record mapping for the <c>calc.WindowedCalculationOrderField</c> table.
/// Each record represents a single field used for ordering within windowed calculation partitions.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class WindowedCalculationOrderFieldRecord
{

    /// <summary>Gets or sets the logical identity of this order field.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent windowed calculation identifier.</summary>
    public Guid WindowedCalculationId { get; set; }



    /// <summary>Gets or sets the sort direction (e.g. "Asc", "Desc").</summary>
    public string Direction { get; set; } = "Asc";

    /// <summary>Gets or sets the ordinal position of this order field.</summary>
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
