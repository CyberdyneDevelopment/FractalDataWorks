using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Checks the portable data types a request names against the <c>DataTypes</c> options (FDW-1103).
/// </summary>
/// <remarks>
/// One check for every write that states a container field's portable type, so a missing or unknown name is
/// refused the same way, with the same code, everywhere. The name is matched exactly: <c>Int32</c>, never
/// <c>int</c> or <c>INT32</c>, because the stored value is what every reader compares against.
/// </remarks>
public static class DataTypeValidator
{
    /// <summary>Validates one portable data type name.</summary>
    /// <param name="dataType">The name the request states.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataTypeUnknown naming the value and the options.</returns>
    public static IGenericResult Validate(string? dataType, ILogger logger) =>
        dataType is { Length: > 0 } name
        && DataTypes.All().Any(option => string.Equals(option.Name, name, StringComparison.Ordinal))
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataStoresResultCodes.ByName("DataTypeUnknown"), logger,
                ResultDetails.Create("DataType", dataType ?? string.Empty, "Options", OptionNames()));

    /// <summary>Validates every portable data type name; the first missing or unknown one is refused.</summary>
    /// <param name="dataTypes">The names the request states.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataTypeUnknown naming the first refused value.</returns>
    public static IGenericResult Validate(IEnumerable<string?> dataTypes, ILogger logger) =>
        dataTypes.Select(dataType => Validate(dataType, logger)).FirstOrDefault(result => result.IsFailure)
            is { } refused
            ? refused
            : GenericResult.Success();

    private static string OptionNames() =>
        string.Join(", ", DataTypes.All().Select(option => option.Name).OrderBy(name => name, StringComparer.Ordinal));
}
