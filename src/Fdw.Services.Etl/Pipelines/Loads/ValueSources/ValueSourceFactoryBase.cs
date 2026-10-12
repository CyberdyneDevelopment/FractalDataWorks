using System;
using Fdw.Collections;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>
/// Base class for the options of <see cref="ValueSourceFactories"/>, using the CRTP pattern.
/// </summary>
public abstract class ValueSourceFactoryBase : TypeOptionBase<ValueSourceFactoryBase>, IValueSourceFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSourceFactoryBase"/> class.
    /// </summary>
    /// <param name="name">The option name, the same as the <c>ValueSourceKinds</c> option it builds.</param>
    /// <param name="suppliesRowValue">Whether the engine can give the column its value in the rows themselves.</param>
    protected ValueSourceFactoryBase(string name, bool suppliesRowValue) : base(name)
    {
        SuppliesRowValue = suppliesRowValue;
    }

    /// <inheritdoc/>
    public bool SuppliesRowValue { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger);

    /// <inheritdoc/>
    public virtual object? RowValue(ColumnValueSource source, DateTimeOffset now) => throw new InvalidOperationException(
        $"A {Name} value source cannot be given to the rows; check SuppliesRowValue first.");
}
