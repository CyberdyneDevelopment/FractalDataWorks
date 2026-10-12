using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="ValueSourceKinds"/>, using the CRTP pattern.
/// </summary>
public abstract class ValueSourceKindBase : TypeOptionBase<ValueSourceKindBase>, IValueSourceKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSourceKindBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="mapsSourceColumn">Whether the kind maps a differently named source column onto the target column, which only a load that takes a source data set can do.</param>
    /// <param name="allowedAsContainerParameter">Whether the kind can supply the parameter of an executable container, where there is no row and no sequence to draw from.</param>
    /// <param name="requiresSourceField">Whether the kind names the source field it reads.</param>
    /// <param name="requiresSequenceName">Whether the kind names a sequence (with an optional schema and database).</param>
    /// <param name="requiresSequenceOrder">Whether the kind needs ordering columns so a batch is numbered in a stated order.</param>
    /// <param name="requiresConstant">Whether the kind carries a constant value and the type that reads it.</param>
    protected ValueSourceKindBase(string name, bool mapsSourceColumn, bool allowedAsContainerParameter, bool requiresSourceField, bool requiresSequenceName, bool requiresSequenceOrder, bool requiresConstant) : base(name, "ValueSourceKinds")
    {
        MapsSourceColumn = mapsSourceColumn;
        AllowedAsContainerParameter = allowedAsContainerParameter;
        RequiresSourceField = requiresSourceField;
        RequiresSequenceName = requiresSequenceName;
        RequiresSequenceOrder = requiresSequenceOrder;
        RequiresConstant = requiresConstant;
    }

    /// <inheritdoc/>
    public bool MapsSourceColumn { get; }

    /// <inheritdoc/>
    public bool AllowedAsContainerParameter { get; }

    /// <inheritdoc/>
    public bool RequiresSourceField { get; }

    /// <inheritdoc/>
    public bool RequiresSequenceName { get; }

    /// <inheritdoc/>
    public bool RequiresSequenceOrder { get; }

    /// <inheritdoc/>
    public bool RequiresConstant { get; }
}
