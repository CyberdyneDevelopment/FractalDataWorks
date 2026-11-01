using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A transaction manager request (MS-TDS 2.2.6.9): ALL_HEADERS, then the request type.
/// </summary>
public sealed class TransactionManagerRequest
{
    /// <summary>Asks for the distributed transaction coordinator's address.</summary>
    public const ushort GetDtcAddress = 0;

    /// <summary>Enlists the session in a distributed transaction.</summary>
    public const ushort PropagateTransaction = 1;

    /// <summary>Begins a transaction.</summary>
    public const ushort BeginTransaction = 5;

    /// <summary>Promotes the transaction to a distributed one.</summary>
    public const ushort PromoteTransaction = 6;

    /// <summary>Commits the transaction.</summary>
    public const ushort CommitTransaction = 7;

    /// <summary>Rolls the transaction back.</summary>
    public const ushort RollbackTransaction = 8;

    /// <summary>Saves a point within the transaction.</summary>
    public const ushort SaveTransaction = 9;

    private TransactionManagerRequest(ushort requestType)
    {
        RequestType = requestType;
    }

    /// <summary>Gets the request type.</summary>
    public ushort RequestType { get; }

    /// <summary>Parses a transaction manager payload.</summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="logger">The logger for a malformed message.</param>
    /// <returns>The request, or the refusal.</returns>
    public static IGenericResult<TransactionManagerRequest> Parse(byte[] payload, ILogger logger)
    {
        var reader = new TdsPayloadReader(payload);
        AllHeaders.Skip(reader);
        var requestType = reader.ReadUInt16();

        return reader.Overrun
            ? GenericResult<TransactionManagerRequest>.Failure(
                TdsProtocolLog.Malformed(logger, "transaction manager request", "it carries no request type"))
            : GenericResult<TransactionManagerRequest>.Success(new TransactionManagerRequest(requestType));
    }
}
