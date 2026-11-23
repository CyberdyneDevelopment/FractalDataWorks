using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>One aggregate function a store can compute, with the words it is asked for by and the type of what it yields.</summary>
public interface ISqlAggregate : ITypeOption<int, SqlAggregateBase>
{
    /// <summary>Gets the keyword the query translator emits.</summary>
    string Sql { get; }

    /// <summary>Gets the name a data query request states it by.</summary>
    string QueryName { get; }

    /// <summary>Gets the words a data set's aggregate definition may state it by, in upper case.</summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>Gets the name of the data type this function yields over a field.</summary>
    /// <param name="fieldTypeName">The name of the data type of the field it is applied to.</param>
    /// <returns>The data type name of the result.</returns>
    string ResultType(string fieldTypeName);
}
