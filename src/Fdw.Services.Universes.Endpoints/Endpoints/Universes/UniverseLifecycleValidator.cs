using Fdw.Results;
using Fdw.Services.Universes.Abstractions;
using Fdw.Services.Universes.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>
/// Checks a universe's lifecycle values against the collections that define them.
/// </summary>
/// <remarks>
/// The database CHECK constraints refuse the same values, but only after the round trip and with a
/// message naming a constraint rather than a field. This is where a caller finds out what they got
/// wrong. The valid set is not restated here — it is read from the collections, so adding an option
/// does not need this file edited.
/// </remarks>
internal static class UniverseLifecycleValidator
{
    /// <summary>Validates a status, if one was supplied.</summary>
    /// <param name="universeName">The universe being written, for the message.</param>
    /// <param name="status">The status, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateStatus(string universeName, string? status, ILogger logger) =>
        status is null || !ReferenceEquals(UniverseStatuses.ByName(status), UniverseStatuses.NotFound)
            ? GenericResult.Success()
            : Reject(universeName, "Status", status, logger);

    /// <summary>Validates a visibility, if one was supplied.</summary>
    /// <param name="universeName">The universe being written, for the message.</param>
    /// <param name="visibility">The visibility, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateVisibility(string universeName, string? visibility, ILogger logger) =>
        visibility is null || !ReferenceEquals(UniverseVisibilities.ByName(visibility), UniverseVisibilities.NotFound)
            ? GenericResult.Success()
            : Reject(universeName, "Visibility", visibility, logger);

    /// <summary>Validates a join policy, if one was supplied.</summary>
    /// <param name="universeName">The universe being written, for the message.</param>
    /// <param name="joinPolicy">The join policy, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateJoinPolicy(string universeName, string? joinPolicy, ILogger logger) =>
        joinPolicy is null || !ReferenceEquals(UniverseJoinPolicies.ByName(joinPolicy), UniverseJoinPolicies.NotFound)
            ? GenericResult.Success()
            : Reject(universeName, "JoinPolicy", joinPolicy, logger);

    private static IGenericResult Reject(string universeName, string field, string value, ILogger logger) =>
        GenericResult.Failure(
            UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), logger,
            ResultDetails.Create("name", universeName, "field", field, "value", value));
}
