using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The names a person reads a data set by, beside its technical name: the display name and the abbreviation a create or update request states
/// (FDW-6117, FDW-6129). One rule for every route that takes them, so a data set of any kind is refused, kept and cleared the same way.
/// </summary>
public static class DataSetNames
{
    // The widths of the DisplayName and Abbreviation columns on every data set table.
    private const int DisplayNameLength = 200;
    private const int AbbreviationLength = 20;

    /// <summary>Refuses a display name or an abbreviation longer than the column that holds it.</summary>
    /// <param name="displayName">The display name the request states; null when it states none.</param>
    /// <param name="abbreviation">The abbreviation the request states; null when it states none.</param>
    /// <param name="dataSetName">The data set's name, for the message.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or <c>DisplayNameTooLong</c> or <c>AbbreviationTooLong</c> naming the data set and the length.</returns>
    public static IGenericResult<bool> Check(string? displayName, string? abbreviation, string dataSetName, ILogger logger) =>
        displayName is { Length: > DisplayNameLength }
            ? GenericResult<bool>.Failure(
                DataSetsResultCodes.DisplayNameTooLong, logger,
                ResultDetails.Create("name", dataSetName, "length", displayName.Length, "maximum", DisplayNameLength))
            : abbreviation is { Length: > AbbreviationLength }
                ? GenericResult<bool>.Failure(
                    DataSetsResultCodes.AbbreviationTooLong, logger,
                    ResultDetails.Create("name", dataSetName, "length", abbreviation.Length, "maximum", AbbreviationLength))
                : GenericResult<bool>.Success(true);

    /// <summary>Sets the names a request states: a name left out keeps the stored one, an empty one clears it.</summary>
    /// <param name="config">The data set's configuration, new or stored.</param>
    /// <param name="displayName">The display name the request states; null when it states none.</param>
    /// <param name="abbreviation">The abbreviation the request states; null when it states none.</param>
    public static void Set(IDataSetImplementationConfiguration config, string? displayName, string? abbreviation)
    {
        if (displayName is not null)
        {
            config.DisplayName = displayName.Length == 0 ? null : displayName;
        }

        if (abbreviation is not null)
        {
            config.Abbreviation = abbreviation.Length == 0 ? null : abbreviation;
        }
    }
}
