using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>What one stand-in strategy does with a request that names it: confirm its parameters, generate a value, persist its own row.</summary>
public interface IStandInHandler : ITypeOption<int, StandInHandlerBase>
{
    /// <summary>Confirms the request supplies every parameter this strategy requires.</summary>
    /// <param name="dataSetName">The data set the stand-in is for.</param>
    /// <param name="fieldName">The field the stand-in is for.</param>
    /// <param name="request">The request to check.</param>
    /// <returns>Success, or a failure naming the missing parameters.</returns>
    IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request);

    /// <summary>Generates one example value.</summary>
    /// <param name="request">The strategy and its parameters.</param>
    /// <param name="random">The generator, already seeded by the caller.</param>
    /// <param name="ordinal">The value's position in the run, for a strategy that counts.</param>
    /// <param name="drawFromDataSetCandidates">The source field's known values, read by DrawFromDataSet only.</param>
    /// <returns>The value, or null when the strategy has nothing to draw.</returns>
    string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates);

    /// <summary>Persists this strategy's own child row under the stand-in's parent row.</summary>
    /// <param name="gateway">The gateway to write through.</param>
    /// <param name="dataStoreName">The data store the stand-in tables are in.</param>
    /// <param name="pathName">The path the stand-in tables are under.</param>
    /// <param name="standIn">The parent row this write is already saving.</param>
    /// <param name="request">The strategy and its parameters.</param>
    /// <param name="now">The write's time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parameters as stored, or the failed save.</returns>
    Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken);
}
