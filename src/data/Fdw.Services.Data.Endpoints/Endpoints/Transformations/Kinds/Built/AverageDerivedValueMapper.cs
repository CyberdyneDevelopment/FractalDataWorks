using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Average derived value.</summary>
[TypeOption(typeof(DerivedValueMappers), "Average", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AverageDerivedValueMapper : AggregateDerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="AverageDerivedValueMapper"/> class.</summary>
    public AverageDerivedValueMapper()
        : base("Average")
    {
    }

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new AverageValue(value.Name, value.Table, value.Column);
}
