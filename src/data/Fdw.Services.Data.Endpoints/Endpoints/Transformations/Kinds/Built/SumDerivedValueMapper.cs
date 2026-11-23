using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Sum derived value.</summary>
[TypeOption(typeof(DerivedValueMappers), "Sum", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SumDerivedValueMapper : AggregateDerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="SumDerivedValueMapper"/> class.</summary>
    public SumDerivedValueMapper()
        : base("Sum")
    {
    }

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new SumValue(value.Name, value.Table, value.Column);
}
