using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One datum type -- what a column's data is -- as offered to the people typing columns (FDW-988).</summary>
public class DatumTypeResponse
{
    /// <summary>Gets or sets the id to send as <c>DatumTypeId</c> on a data store field or a data set field.</summary>
    public int DatumTypeId { get; set; }

    /// <summary>Gets or sets the type's name, for example <c>Measure</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what data of this type is.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a query returns a column of this type when it asks for nothing extra.</summary>
    public bool IsDefaultResult { get; set; }

    /// <summary>Gets or sets whether a column of this type may be shown to a person.</summary>
    public bool IsDisplayable { get; set; }

    /// <summary>Gets or sets whether a column of this type identifies its record.</summary>
    public bool IsKeyRole { get; set; }

    /// <summary>Gets or sets whether a result can be ordered by a column of this type.</summary>
    public bool IsOrderable { get; set; }

    /// <summary>Gets or sets whether a result can be grouped by a column of this type.</summary>
    public bool IsGroupable { get; set; }

    /// <summary>Gets or sets whether a column of this type can be summed, averaged and the like.</summary>
    public bool IsAggregatable { get; set; }

    /// <summary>Gets or sets whether a column of this type is a point in time that describes the record.</summary>
    public bool IsPointInTime { get; set; }

    /// <summary>Gets or sets whether a column of this type is where a record's validity period starts.</summary>
    public bool IsLowerDateBoundary { get; set; }

    /// <summary>Gets or sets whether a column of this type is where a record's validity period ends.</summary>
    public bool IsUpperDateBoundary { get; set; }

    /// <summary>Gets or sets the chart axes a column of this type can sit on as it stands: Dimension, Measure, Time or Place.</summary>
    public List<string> Axes { get; set; } = [];

    /// <summary>Gets or sets the aggregates a column of this type allows: Count, Sum, Avg, Min, Max. A chart plots a Dimension only through one of these.</summary>
    public List<string> Aggregates { get; set; } = [];

    /// <summary>Gets or sets the filter operators a column of this type takes.</summary>
    public List<string> AllowedOperators { get; set; } = [];
}
