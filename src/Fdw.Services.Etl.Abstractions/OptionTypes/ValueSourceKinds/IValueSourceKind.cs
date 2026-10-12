using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Where a loaded column gets its value: a column of the source row, the next value of a sequence, the current time or a constant (FDW-1108).
/// </summary>
public interface IValueSourceKind : ITypeOption<int, IValueSourceKind>
{
    /// <summary>Gets whether the kind maps a differently named source column onto the target column, which only a load that takes a source data set can do.</summary>
    bool MapsSourceColumn { get; }

    /// <summary>Gets whether the kind can supply the parameter of an executable container, where there is no row and no sequence to draw from.</summary>
    bool AllowedAsContainerParameter { get; }

    /// <summary>Gets whether the kind names the source field it reads.</summary>
    bool RequiresSourceField { get; }

    /// <summary>Gets whether the kind names a sequence (with an optional schema and database).</summary>
    bool RequiresSequenceName { get; }

    /// <summary>Gets whether the kind needs ordering columns so a batch is numbered in a stated order.</summary>
    bool RequiresSequenceOrder { get; }

    /// <summary>Gets whether the kind carries a constant value and the type that reads it.</summary>
    bool RequiresConstant { get; }
}
