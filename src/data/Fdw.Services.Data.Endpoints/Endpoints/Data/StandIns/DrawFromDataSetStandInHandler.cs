using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Each value is drawn from the known values of another data set's field.</summary>
[TypeOption(typeof(StandInHandlers), "DrawFromDataSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DrawFromDataSetStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="DrawFromDataSetStandInHandler"/> class.</summary>
    public DrawFromDataSetStandInHandler()
        : base("DrawFromDataSet")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "sourceDataSetId/sourceFieldId", request.SourceDataSetId is not null && request.SourceFieldId is not null);

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
        => drawFromDataSetCandidates is { Count: > 0 } ? drawFromDataSetCandidates[random.Next(drawFromDataSetCandidates.Count)] : null;

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new DrawFromDataSetStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            SourceDataSetId = request.SourceDataSetId!.Value,
            SourceFieldId = request.SourceFieldId!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<DrawFromDataSetStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "DrawFromDataSetStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["sourceDataSetId"] = row.SourceDataSetId.ToString(),
                ["sourceFieldId"] = row.SourceFieldId.ToString(),
            });
    }
}
