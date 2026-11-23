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

/// <summary>Every generated value is the request's one literal.</summary>
[TypeOption(typeof(StandInHandlers), "FixedValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FixedValueStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="FixedValueStandInHandler"/> class.</summary>
    public FixedValueStandInHandler()
        : base("FixedValue")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "fixedValue", request.FixedValue is not null);

    /// <inheritdoc />
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
        => request.FixedValue;

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new FixedValueStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            FixedValue = request.FixedValue!,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<FixedValueStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "FixedValueStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal) { ["fixedValue"] = row.FixedValue });
    }
}
