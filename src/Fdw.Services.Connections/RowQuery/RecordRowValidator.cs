using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Validates decoded rows against the container's DECLARED field schema before anything else reads them.
/// A field the container declares <c>IsNullable: false</c> that is ABSENT from a row — or present as
/// <see langword="null"/> — is a schema violation in the source data and fails loud, naming the
/// container, the field and the row index.
/// </summary>
/// <remarks>
/// Why this exists (the mechanism, not a call-site patch): the generated <c>PocoMapper</c>'s
/// <c>GetReaderValue_*</c> helpers turn an absent/null column into the property's DEFAULT value — the
/// framework-wide contract for a column the projection does not carry (a POCO property that is not a
/// declared column, on every transport). That tolerance is correct for an UNDECLARED column and
/// catastrophic for a DECLARED one: a config file that omits <c>Prefix</c> would silently resolve secrets
/// under the empty prefix and still report SUCCESS. The container's declared fields ARE the schema, so
/// the mapper must never be handed a row that violates them — that check lives here, once, for every
/// record-connector transport.
/// </remarks>
public static class RecordRowValidator
{
    /// <summary>
    /// Validates that every row carries a non-null value for every field the container declares as
    /// non-nullable.
    /// </summary>
    /// <param name="rows">The decoded rows, in source order.</param>
    /// <param name="container">The container whose declared fields are the schema.</param>
    /// <param name="logger">Logger for the structured validation failure.</param>
    /// <returns>Success when every row satisfies the declared schema; a fail-loud failure otherwise.</returns>
    public static IGenericResult Validate(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IDataContainer container,
        ILogger logger)
    {
        var fields = ContainerFields.Of(container);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowResult = ValidateRow(rows[rowIndex], rowIndex, fields, container.Name, logger);
            if (!rowResult.IsSuccess)
                return rowResult;
        }

        RecordQueryLog.RowsValidated(logger, container.Name, rows.Count);
        return GenericResult.Success();
    }

    private static IGenericResult ValidateRow(
        IReadOnlyDictionary<string, object?> row,
        int rowIndex,
        IReadOnlyList<IDataField> fields,
        string containerName,
        ILogger logger)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].IsNullable)
                continue;

            // Why: absent key and present-but-null are the SAME violation of a non-nullable declaration —
            // both would reach the mapper as DBNull and silently become the property's default.
            if (!row.TryGetValue(fields[i].Name, out var value) || value is null)
                return GenericResult.Failure(
                    RecordQueryLog.RequiredFieldMissingInRow(logger, containerName, fields[i].Name, rowIndex));
        }

        return GenericResult.Success();
    }
}
