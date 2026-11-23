using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Lookup derived value: the value of a column in the table row whose key column matches a field of the driver row.</summary>
[TypeOption(typeof(DerivedValueMappers), "Lookup", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LookupDerivedValueMapper : DerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupDerivedValueMapper"/> class.</summary>
    public LookupDerivedValueMapper()
        : base("Lookup")
    {
    }

    /// <inheritdoc />
    public override string? Problem(DerivedValuePayload value) =>
        value.KeyColumn is null || value.MatchField is null ? "a Lookup states its keyColumn and matchField" : null;

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new LookupValue(value.Name, value.Table, value.KeyColumn!, value.MatchField!, value.Column);
}
