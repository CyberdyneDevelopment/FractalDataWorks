using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Count derived value.</summary>
[TypeOption(typeof(DerivedValueMappers), "Count", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CountDerivedValueMapper : AggregateDerivedValueMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="CountDerivedValueMapper"/> class.</summary>
    public CountDerivedValueMapper()
        : base("Count")
    {
    }

    /// <inheritdoc />
    public override DerivedValueDefinition Make(DerivedValuePayload value) =>
        new CountValue(value.Name, value.Table, value.Column);
}
