using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="LoadStepKinds"/>, using the CRTP pattern.
/// </summary>
public abstract class LoadStepKindBase : TypeOptionBase<LoadStepKindBase>, ILoadStepKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadStepKindBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the step does, as the console shows it beside the kind.</param>
    /// <param name="usesKeyColumns">Whether the load matches rows on key columns: at least one is needed, and none are accepted when false.</param>
    /// <param name="usesCompareColumns">Whether the load decides a matched row changed by comparing columns: they are accepted, and none is needed, when true.</param>
    /// <param name="usesWriteColumns">Whether the load writes a stated list of columns: at least one is needed, and none are accepted when false.</param>
    /// <param name="allowsVersioning">Whether the load can keep versioned history with effective-from and effective-to columns.</param>
    /// <param name="allowsSoftDelete">Whether the load can set a deleted flag instead of removing rows.</param>
    /// <param name="usesSourceDataSet">Whether the step reads its rows from a source data set of its own, with an optional filter expression.</param>
    /// <param name="usesDestinationContainer">Whether the step runs a named executable container of its destination data store.</param>
    /// <param name="usesRows">Whether the step works on the pipeline's transformed output rows.</param>
    /// <param name="acceptsRejectedRecords">Whether the step can write the records a pipeline's stages rejected, with their reason and stage.</param>
    protected LoadStepKindBase(string name, string description, bool usesKeyColumns, bool usesCompareColumns, bool usesWriteColumns, bool allowsVersioning, bool allowsSoftDelete, bool usesSourceDataSet, bool usesDestinationContainer, bool usesRows, bool acceptsRejectedRecords) : base(name, name, description, "LoadStepKinds")
    {
        UsesKeyColumns = usesKeyColumns;
        UsesCompareColumns = usesCompareColumns;
        UsesWriteColumns = usesWriteColumns;
        AllowsVersioning = allowsVersioning;
        AllowsSoftDelete = allowsSoftDelete;
        UsesSourceDataSet = usesSourceDataSet;
        UsesDestinationContainer = usesDestinationContainer;
        UsesRows = usesRows;
        AcceptsRejectedRecords = acceptsRejectedRecords;
    }

    /// <inheritdoc/>
    public bool UsesKeyColumns { get; }

    /// <inheritdoc/>
    public bool UsesCompareColumns { get; }

    /// <inheritdoc/>
    public bool UsesWriteColumns { get; }

    /// <inheritdoc/>
    public bool AllowsVersioning { get; }

    /// <inheritdoc/>
    public bool AllowsSoftDelete { get; }

    /// <inheritdoc/>
    public bool UsesSourceDataSet { get; }

    /// <inheritdoc/>
    public bool UsesDestinationContainer { get; }

    /// <inheritdoc/>
    public bool UsesRows { get; }

    /// <inheritdoc/>
    public bool AcceptsRejectedRecords { get; }
}
