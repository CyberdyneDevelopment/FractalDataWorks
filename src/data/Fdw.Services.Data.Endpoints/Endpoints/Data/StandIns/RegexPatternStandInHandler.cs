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

/// <summary>Each value is a string matching the request's pattern.</summary>
[TypeOption(typeof(StandInHandlers), "RegexPattern", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RegexPatternStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="RegexPatternStandInHandler"/> class.</summary>
    public RegexPatternStandInHandler()
        : base("RegexPattern")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "pattern", !string.IsNullOrEmpty(request.Pattern));

    /// <inheritdoc />
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
        => RegexPatternGenerator.Generate(request.Pattern!, random);

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new RegexPatternStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            Pattern = request.Pattern!,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<RegexPatternStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "RegexPatternStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal) { ["pattern"] = row.Pattern });
    }
}
