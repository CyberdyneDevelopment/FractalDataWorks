using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of the kinds that fold one column of a table: they take no key column and no match field.</summary>
[ExcludeFromCodeCoverage]
public abstract class AggregateDerivedValueMapperBase : DerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="AggregateDerivedValueMapperBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected AggregateDerivedValueMapperBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public override string? Problem(DerivedValuePayload value) =>
        value.KeyColumn is not null || value.MatchField is not null ? "keyColumn and matchField belong to a Lookup" : null;
}
