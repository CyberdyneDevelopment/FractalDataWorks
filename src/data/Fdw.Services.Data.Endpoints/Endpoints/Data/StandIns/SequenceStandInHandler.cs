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

/// <summary>Each value is the start plus the increment times its position.</summary>
[TypeOption(typeof(StandInHandlers), "Sequence", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SequenceStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="SequenceStandInHandler"/> class.</summary>
    public SequenceStandInHandler()
        : base("Sequence")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "startValue/increment", request.StartValue is not null && request.Increment is not null);

    /// <inheritdoc />
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
        => (request.StartValue!.Value + (ordinal * request.Increment!.Value)).ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new SequenceStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            StartValue = request.StartValue!.Value,
            Increment = request.Increment!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<SequenceStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "SequenceStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["startValue"] = row.StartValue,
                ["increment"] = row.Increment,
            });
    }
}
