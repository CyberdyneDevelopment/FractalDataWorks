using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Min derived value.</summary>
[TypeOption(typeof(DerivedValueMappers), "Min", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MinDerivedValueMapper : AggregateDerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="MinDerivedValueMapper"/> class.</summary>
    public MinDerivedValueMapper()
        : base("Min")
    {
    }

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new MinValue(value.Name, value.Table, value.Column);
}
