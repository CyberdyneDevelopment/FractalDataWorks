using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Fdw.Schema;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Checks the datum types a request names against the defined types (FDW-988).
/// </summary>
/// <remarks>
/// One check for every write that carries a datum type -- data store fields and data set fields -- so an
/// unknown type is refused the same way, with the same code, everywhere. Every field has a type
/// (<c>NotYetTyped</c> until someone picks one); only a PATCH can leave one out, meaning "unchanged".
/// </remarks>
public static class DatumTypeValidator
{
    /// <summary>Validates a datum type.</summary>
    /// <param name="datumTypeId">The datum type's Id.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DatumTypeUnknown naming the Id.</returns>
    public static IGenericResult Validate(int datumTypeId, ILogger logger) =>
        !ReferenceEquals(DatumTypes.ById(datumTypeId), DatumTypes.NotFound)
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataStoresResultCodes.ByName("DatumTypeUnknown"), logger,
                ResultDetails.Create("DatumTypeId", datumTypeId.ToString(CultureInfo.InvariantCulture)));

    /// <summary>Validates every datum type; the first unknown one is refused.</summary>
    /// <param name="datumTypeIds">The datum type Ids.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DatumTypeUnknown naming the first unknown Id.</returns>
    public static IGenericResult Validate(IEnumerable<int> datumTypeIds, ILogger logger) =>
        datumTypeIds.Select(id => Validate(id, logger)).FirstOrDefault(result => result.IsFailure)
            is { } refused
            ? refused
            : GenericResult.Success();
}
