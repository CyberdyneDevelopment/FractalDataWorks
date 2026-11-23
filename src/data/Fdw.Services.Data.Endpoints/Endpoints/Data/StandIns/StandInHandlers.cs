using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Results;
using Fdw.Results;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The stand-in handlers, keyed by the strategy name each one serves.</summary>
/// <remarks>
/// A strategy the collection holds no handler for is a failure, never a quiet default: <see cref="For"/> answers the
/// StandInStrategyInvalid code, so no caller reads through the NotFound sentinel.
/// </remarks>
[TypeCollection(typeof(StandInHandlerBase), typeof(IStandInHandler), typeof(StandInHandlers))]
[ExcludeFromCodeCoverage]
public abstract partial class StandInHandlers : TypeCollectionBase<StandInHandlerBase, IStandInHandler>
{
    /// <summary>Finds the handler for a strategy.</summary>
    /// <param name="dataSetName">The data set the stand-in is for, named in a failure.</param>
    /// <param name="fieldName">The field the stand-in is for, named in a failure.</param>
    /// <param name="strategy">The strategy name.</param>
    /// <returns>The handler, or the StandInStrategyInvalid failure when none serves the strategy.</returns>
    public static IGenericResult<IStandInHandler> For(string dataSetName, string fieldName, string strategy) =>
        ReferenceEquals(ByName(strategy), NotFound)
            ? GenericResult<IStandInHandler>.Failure(
                DataSetsResultCodes.ByName("StandInStrategyInvalid"),
                ResultDetails.Create("name", $"{dataSetName}.{fieldName}", "strategy", strategy))
            : GenericResult<IStandInHandler>.Success(ByName(strategy));
}
