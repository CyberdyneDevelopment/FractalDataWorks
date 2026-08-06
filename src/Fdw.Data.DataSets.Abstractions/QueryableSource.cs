using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a queryable data source in the expression tree without circular references.
/// </summary>
// Why: pure IQueryable marker/placeholder used only as an Expression.Constant target for expression-tree
// building — it is never enumerated (GetEnumerator is an intentional "must not be called" guard, not
// logic), and every other member is a trivial property set once in the constructor.
[ExcludeFromCodeCoverage]
internal sealed class QueryableSource<T> : IQueryable<T>
{
    public QueryableSource(string dataSetName)
    {
        DataSetName = dataSetName;
        ElementType = typeof(T);
        Expression = Expression.Constant(this);
        Provider = null!; // Not used for expression building
    }

    public string DataSetName { get; }
    public Type ElementType { get; }
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator() => throw new NotSupportedException("QueryableSource is for expression building only");
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"DataSet({DataSetName})";
}