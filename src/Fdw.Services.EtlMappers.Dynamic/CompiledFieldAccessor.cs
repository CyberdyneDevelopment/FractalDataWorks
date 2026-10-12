using System;
using System.Data;
using System.Linq.Expressions;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Dynamic;

/// <summary>
/// Compiled field accessor using expression trees for efficient field access.
/// </summary>
public sealed class CompiledFieldAccessor
{
    private readonly Func<IDataReader, int, object?> _getValue;

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the ordinal position in the reader.
    /// </summary>
    public int Ordinal { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledFieldAccessor"/> class.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="ordinal">The ordinal position.</param>
    public CompiledFieldAccessor(string fieldName, int ordinal)
    {
        FieldName = fieldName;
        Ordinal = ordinal;
        _getValue = CompileAccessor();
    }

    /// <summary>
    /// Gets the value from the reader using the compiled accessor.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>The field value or null.</returns>
    public object? GetValue(IDataReader reader)
    {
        if (Ordinal < 0)
            return null;

        return _getValue(reader, Ordinal);
    }

    private static Func<IDataReader, int, object?> CompileAccessor()
    {
        // Parameters
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        var ordinalParam = Expression.Parameter(typeof(int), "ordinal");

        // IDataReader extends IDataRecord; IsDBNull and GetValue are defined on IDataRecord
        var isDbNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull))!;
        var getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue))!;

        // reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)
        var isDbNullCall = Expression.Call(readerParam, isDbNullMethod, ordinalParam);
        var getValueCall = Expression.Call(readerParam, getValueMethod, ordinalParam);
        var nullConst = Expression.Constant(null, typeof(object));

        var conditional = Expression.Condition(
            isDbNullCall,
            nullConst,
            Expression.Convert(getValueCall, typeof(object)));

        var lambda = Expression.Lambda<Func<IDataReader, int, object?>>(
            conditional, readerParam, ordinalParam);

        return lambda.Compile();
    }
}
