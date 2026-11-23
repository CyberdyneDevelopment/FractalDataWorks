using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Each value is one of the request's values, picked by weight.</summary>
[TypeOption(typeof(StandInHandlers), "WeightedPick", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WeightedPickStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="WeightedPickStandInHandler"/> class.</summary>
    public WeightedPickStandInHandler()
        : base("WeightedPick")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "values", request.Values is { Count: > 0 });

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
    {
        var options = request.Values!;
        var totalWeight = options.Sum(o => o.Weight);
        var roll = random.Next(0, Math.Max(totalWeight, 1));
        var cumulative = 0;
        foreach (var option in options.OrderBy(o => o.Ordinal))
        {
            cumulative += option.Weight;
            if (roll < cumulative) return option.Value;
        }

        return options[^1].Value;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var pick = new WeightedPickStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            CreateDate = now,
        };
        var pickSaved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<WeightedPickStandInConfiguration>(pick, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "WeightedPickStandIn"), cancellationToken).ConfigureAwait(false);
        if (pickSaved.IsFailure) return pickSaved.ToNewResult<Dictionary<string, object?>>();

        var valueTarget = new DataStoreTarget(dataStoreName, pathName, "WeightedPickStandInValue");
        foreach (var option in request.Values!)
        {
            var valueRow = new WeightedPickStandInValueConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = $"{pick.Name}:{option.Ordinal}",
                WeightedPickStandInId = pick.Id,
                Value = option.Value,
                Weight = option.Weight,
                Ordinal = option.Ordinal,
                CreateDate = now,
            };
            var valueSaved = await gateway.Execute<ConfigurationWriteResult>(
                new ConfigurationSaveCommand<WeightedPickStandInValueConfiguration>(valueRow, replacesCurrent: true), valueTarget, cancellationToken).ConfigureAwait(false);
            if (valueSaved.IsFailure) return valueSaved.ToNewResult<Dictionary<string, object?>>();
        }

        return GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["values"] = request.Values.Select(v => new { v.Value, v.Weight, v.Ordinal }).ToList(),
        });
    }
}
