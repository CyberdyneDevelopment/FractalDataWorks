using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Expressions;

/// <summary>
/// Compiled field accessor for IDataRow.
/// </summary>
/// <typeparam name="TValue">The type of the field value.</typeparam>
internal sealed class CompiledFieldAccessor<TValue> : IFieldAccessor<TValue>
{
    /// <inheritdoc/>
    public string FieldName { get; }

    /// <inheritdoc/>
    public int Ordinal { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledFieldAccessor{TValue}"/> class.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="ordinal">The field ordinal.</param>
    public CompiledFieldAccessor(string fieldName, int ordinal)
    {
        FieldName = fieldName;
        Ordinal = ordinal;
    }

    /// <inheritdoc/>
    public TValue GetValue(IDataRow row)
    {
        return row.GetValue<TValue>(Ordinal);
    }

    /// <inheritdoc/>
    public bool TryGetValue(IDataRow row, out TValue? value)
    {
        return row.TryGetValue(Ordinal, out value);
    }
}