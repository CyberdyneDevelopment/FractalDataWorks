using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// What a part of a graph reads once it is pushed into its store (FDW-6969): one container, or containers joined to it, with the fields it sends on,
/// the order the in-memory components would send them in and the components it stands in for.
/// </summary>
/// <param name="DataStoreId">The data store record every container of it is in.</param>
/// <param name="Primary">The container the query reads first, with the data store and path every container of it is read at.</param>
/// <param name="PrimaryKey">The columns of the primary container's primary key.</param>
/// <param name="Fields">The fields it sends on, in order.</param>
/// <param name="Joins">The joins of the query, in order.</param>
/// <param name="Order">The columns the rows are ordered by: for a join each as <c>Container.Column</c>, each container's key in join order; for a union the aliases of the branches' keys.</param>
/// <param name="Containers">The containers it reads.</param>
/// <param name="Members">The components it stands in for.</param>
/// <param name="Unions">The other containers whose rows a union stacks under the primary's (FDW-6946's UNION ALL); none for a join.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownSide(
    Guid DataStoreId,
    DataStoreTarget Primary,
    IReadOnlyList<string> PrimaryKey,
    IReadOnlyList<PushdownColumn> Fields,
    IReadOnlyList<IJoinExpression> Joins,
    IReadOnlyList<string> Order,
    IReadOnlyList<string> Containers,
    IReadOnlyList<string> Members,
    IReadOnlyList<IUnionBranch> Unions)
{
    /// <summary>Finds a field the side sends on by its name, ignoring case as a row's fields are matched.</summary>
    /// <param name="name">The field's name.</param>
    /// <returns>The field, or null when the side sends none by that name.</returns>
    public PushdownColumn? Field(string name) => Fields.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
}
