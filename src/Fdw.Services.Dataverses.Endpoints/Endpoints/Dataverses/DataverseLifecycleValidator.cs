using Fdw.Results;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>
/// Checks a dataverse's lifecycle values against the collections that define them.
/// </summary>
/// <remarks>
/// The database CHECK constraints refuse the same values, but only after the round trip and with a
/// message naming a constraint rather than a field. This is where a caller finds out what they got
/// wrong. The valid set is not restated here — it is read from the collections, so adding an option
/// does not need this file edited.
/// </remarks>
internal static class DataverseLifecycleValidator
{
    /// <summary>Validates a status, if one was supplied.</summary>
    /// <param name="dataverseName">The dataverse being written, for the message.</param>
    /// <param name="status">The status, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateStatus(string dataverseName, string? status, ILogger logger) =>
        status is null || !ReferenceEquals(DataverseStatuses.ByName(status), DataverseStatuses.NotFound)
            ? GenericResult.Success()
            : Reject(dataverseName, "Status", status, logger);

    /// <summary>Validates a visibility, if one was supplied.</summary>
    /// <param name="dataverseName">The dataverse being written, for the message.</param>
    /// <param name="visibility">The visibility, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateVisibility(string dataverseName, string? visibility, ILogger logger) =>
        visibility is null || !ReferenceEquals(DataverseVisibilities.ByName(visibility), DataverseVisibilities.NotFound)
            ? GenericResult.Success()
            : Reject(dataverseName, "Visibility", visibility, logger);

    /// <summary>Validates a join policy, if one was supplied.</summary>
    /// <param name="dataverseName">The dataverse being written, for the message.</param>
    /// <param name="joinPolicy">The join policy, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateJoinPolicy(string dataverseName, string? joinPolicy, ILogger logger) =>
        joinPolicy is null || !ReferenceEquals(DataverseJoinPolicies.ByName(joinPolicy), DataverseJoinPolicies.NotFound)
            ? GenericResult.Success()
            : Reject(dataverseName, "JoinPolicy", joinPolicy, logger);

    private static IGenericResult Reject(string dataverseName, string field, string value, ILogger logger) =>
        GenericResult.Failure(
            DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), logger,
            ResultDetails.Create("name", dataverseName, "field", field, "value", value));
}
