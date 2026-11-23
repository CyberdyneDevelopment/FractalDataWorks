using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Each value is a random number between the request's minimum and maximum.</summary>
[TypeOption(typeof(StandInHandlers), "NumericRange", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NumericRangeStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="NumericRangeStandInHandler"/> class.</summary>
    public NumericRangeStandInHandler()
        : base("NumericRange")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "minValue/maxValue", request.MinValue is not null && request.MaxValue is not null);

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
    {
        var value = request.MinValue!.Value + ((decimal)random.NextDouble() * (request.MaxValue!.Value - request.MinValue!.Value));
        var rounded = request.Decimals is { } decimals ? Math.Round(value, decimals) : Math.Round(value, 0);
        return rounded.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new NumericRangeStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            MinValue = request.MinValue!.Value,
            MaxValue = request.MaxValue!.Value,
            Decimals = request.Decimals,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<NumericRangeStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "NumericRangeStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["minValue"] = row.MinValue,
                ["maxValue"] = row.MaxValue,
                ["decimals"] = row.Decimals,
            });
    }
}
