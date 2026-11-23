using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every stand-in handler: one strategy's validation, generation and persistence.</summary>
[ExcludeFromCodeCoverage]
public abstract class StandInHandlerBase : TypeOptionBase<StandInHandlerBase>, IStandInHandler
{
    /// <summary>Initializes a new instance of the <see cref="StandInHandlerBase"/> class.</summary>
    /// <param name="name">The strategy's name, the same name its <c>StandInStrategies</c> option carries.</param>
    protected StandInHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request);

    /// <inheritdoc />
    public abstract string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates);

    /// <inheritdoc />
    public abstract Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Success when <paramref name="present"/>, else the StandInParameterMissing failure naming the parameters.</summary>
    /// <param name="dataSetName">The data set the stand-in is for.</param>
    /// <param name="fieldName">The field the stand-in is for.</param>
    /// <param name="parameterNames">The parameters this strategy requires.</param>
    /// <param name="present">Whether the request supplies them.</param>
    /// <returns>The result.</returns>
    protected static IGenericResult Require(string dataSetName, string fieldName, string parameterNames, bool present) =>
        present
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataSetsResultCodes.ByName("StandInParameterMissing"),
                ResultDetails.Create("name", $"{dataSetName}.{fieldName}", "field", parameterNames));
}
