using System;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A SQL batch message (MS-TDS 2.2.6.7): ALL_HEADERS, then the batch's Unicode text.
/// </summary>
public sealed class SqlBatchRequest
{
    private SqlBatchRequest(string text)
    {
        Text = text;
    }

    /// <summary>Gets the batch's T-SQL text.</summary>
    public string Text { get; }

    /// <summary>Parses a SQL batch payload.</summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="logger">The logger for a malformed message.</param>
    /// <returns>The batch, or the refusal.</returns>
    public static IGenericResult<SqlBatchRequest> Parse(byte[] payload, ILogger logger)
    {
        var reader = new TdsPayloadReader(payload);
        AllHeaders.Skip(reader);
        var remaining = reader.Remaining;

        if (reader.Overrun || remaining % 2 != 0)
            return GenericResult<SqlBatchRequest>.Failure(
                TdsProtocolLog.Malformed(logger, "SQL batch", "its headers or text do not fit the payload"));

        return GenericResult<SqlBatchRequest>.Success(new SqlBatchRequest(reader.ReadUnicode(remaining / 2)));
    }
}
