using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Checks the data classifications a request names against the defined levels.
/// </summary>
/// <remarks>
/// One check for every write that carries a classification -- stores, paths, containers, fields, data
/// sets and workspaces -- so an unknown level is refused the same way, with the same code, everywhere.
/// Every classification is a level (<c>NotYetClassified</c> until someone picks one); only a PATCH
/// can leave one out, meaning "unchanged", and that is not checked.
/// </remarks>
public static class DataClassificationValidator
{
    /// <summary>Validates a classification.</summary>
    /// <param name="classificationId">The classification's Id.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataClassificationUnknown naming the Id.</returns>
    public static IGenericResult Validate(int classificationId, ILogger logger) =>
        !ReferenceEquals(DataClassifications.ById(classificationId), DataClassifications.NotFound)
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataStoresResultCodes.ByName("DataClassificationUnknown"), logger,
                ResultDetails.Create("ClassificationId", classificationId.ToString(CultureInfo.InvariantCulture)));

    /// <summary>Validates a PATCH's classification, if it named one.</summary>
    /// <param name="classificationId">The classification's Id, or null when the PATCH left it unchanged.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataClassificationUnknown naming the Id.</returns>
    public static IGenericResult Validate(int? classificationId, ILogger logger) =>
        classificationId is { } named ? Validate(named, logger) : GenericResult.Success();

    /// <summary>Validates every classification; the first unknown one is refused.</summary>
    /// <param name="classificationIds">The classification Ids.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataClassificationUnknown naming the first unknown Id.</returns>
    public static IGenericResult Validate(IEnumerable<int> classificationIds, ILogger logger) =>
        classificationIds.Select(id => Validate(id, logger)).FirstOrDefault(result => result.IsFailure)
            is { } refused
            ? refused
            : GenericResult.Success();
}
