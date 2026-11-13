using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Options;

/// <summary>
/// What an option is applied to: the column a stored row is about, the row's own settings, and the values selected so far.
/// </summary>
public sealed class DataOptionContext
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionContext"/> class.</summary>
    /// <param name="dataStoreName">The name of the data store that holds the column's container.</param>
    /// <param name="containerName">The name of the data container.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="optionType">The option type the row names, as it was stored.</param>
    /// <param name="option">The option the row names, as it was stored.</param>
    /// <param name="value">The row's value; an option that needs one fails when it is missing.</param>
    /// <param name="isActive">Whether the row is switched on; an inactive row changes nothing.</param>
    /// <param name="values">The values selected so far, which the option builds on.</param>
    /// <param name="logger">The logger the option writes to.</param>
    public DataOptionContext(
        string dataStoreName,
        string containerName,
        string columnName,
        string optionType,
        string option,
        string? value,
        bool isActive,
        IReadOnlyList<string> values,
        ILogger? logger = null)
    {
        DataStoreName = dataStoreName;
        ContainerName = containerName;
        ColumnName = columnName;
        OptionType = optionType;
        Option = option;
        Value = value;
        IsActive = isActive;
        Values = values;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the name of the data store that holds the column's container.</summary>
    public string DataStoreName { get; }

    /// <summary>Gets the name of the data container.</summary>
    public string ContainerName { get; }

    /// <summary>Gets the name of the column.</summary>
    public string ColumnName { get; }

    /// <summary>Gets the option type the row names, as it was stored.</summary>
    public string OptionType { get; }

    /// <summary>Gets the option the row names, as it was stored.</summary>
    public string Option { get; }

    /// <summary>Gets the row's value; null when the row has none.</summary>
    public string? Value { get; }

    /// <summary>Gets whether the row is switched on. An inactive row changes nothing.</summary>
    public bool IsActive { get; }

    /// <summary>Gets the values selected so far, which the option builds on and does not change.</summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>Gets the logger the option writes to.</summary>
    public ILogger Logger { get; }
}
