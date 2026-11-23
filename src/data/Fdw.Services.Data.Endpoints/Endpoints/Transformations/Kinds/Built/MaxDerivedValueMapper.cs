using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Max derived value.</summary>
[TypeOption(typeof(DerivedValueMappers), "Max", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxDerivedValueMapper : AggregateDerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="MaxDerivedValueMapper"/> class.</summary>
    public MaxDerivedValueMapper()
        : base("Max")
    {
    }

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new MaxValue(value.Name, value.Table, value.Column);
}
