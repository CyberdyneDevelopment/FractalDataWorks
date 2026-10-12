using System;
using System.Collections.Generic;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds;

/// <summary>
/// An RPC request (MS-TDS 2.2.6.6): ALL_HEADERS, the procedure by name or well-known id, then its
/// parameters.
/// </summary>
/// <remarks>
/// One procedure per request. A client batching several RPCs into one message is refused: that is how a
/// client sends batched writes, and this server serves reads.
/// </remarks>
public sealed class RpcRequest
{
    private const ushort ProcedureIdMarker = 0xFFFF;
    private const byte OutputFlag = 0x01;
    private const byte BatchFlag = 0xFF;
    private const byte NoExecFlag = 0xFE;

    private RpcRequest(string procedureName, IReadOnlyList<RpcParameter> parameters)
    {
        ProcedureName = procedureName;
        Parameters = parameters;
    }

    /// <summary>Gets the procedure's name, as named or as its well-known id stands for.</summary>
    public string ProcedureName { get; }

    /// <summary>Gets the parameters in the order they were sent.</summary>
    public IReadOnlyList<RpcParameter> Parameters { get; }

    /// <summary>Parses an RPC payload.</summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="logger">The logger for a malformed or unsupported message.</param>
    /// <returns>The request, or the refusal.</returns>
    public static IGenericResult<RpcRequest> Parse(byte[] payload, ILogger logger)
    {
        var reader = new TdsPayloadReader(payload);
        AllHeaders.Skip(reader);

        var nameLength = reader.ReadUInt16();
        var name = nameLength == ProcedureIdMarker
            ? RpcProcedureIds.NameOf(reader.ReadUInt16())
            : reader.ReadUnicode(nameLength);
        reader.ReadUInt16();

        if (reader.Overrun || name.Length == 0)
            return Malformed(logger, "it names no procedure this protocol defines");

        var parameters = new List<RpcParameter>();
        while (reader.Remaining > 0)
        {
            var parameter = ReadParameter(reader, logger);
            if (parameter.IsFailure)
                return parameter.ToNewResult<RpcRequest>();

            parameters.Add(parameter.Value!);
        }

        return GenericResult<RpcRequest>.Success(new RpcRequest(name, parameters));
    }

    private static IGenericResult<RpcParameter> ReadParameter(TdsPayloadReader reader, ILogger logger)
    {
        var first = reader.BytesAt(reader.Position, 1);
        if (first.Length == 1 && first[0] is BatchFlag or NoExecFlag)
            return GenericResult<RpcParameter>.Failure(TdsProtocolLog.NotSupported(logger, "several RPCs batched in one request"));

        var name = reader.ReadByteLengthUnicode();
        var status = reader.ReadByte();
        if (!TdsTypeInfo.TryRead(reader, out var typeInfo))
            return GenericResult<RpcParameter>.Failure(TdsProtocolLog.NotSupported(logger,
                $"parameter '{name}' of type 0x{typeInfo.TypeId:X2}"));

        if (!TdsValueReader.TryRead(reader, typeInfo, out var value, out var problem))
            return GenericResult<RpcParameter>.Failure(TdsProtocolLog.NotSupported(logger, $"parameter '{name}' as {problem}"));

        if (reader.Overrun)
            return GenericResult<RpcParameter>.Failure(TdsProtocolLog.Malformed(logger, "RPC request", $"parameter '{name}' runs past the end of the payload"));

        return GenericResult<RpcParameter>.Success(new RpcParameter(name, (status & OutputFlag) != 0, typeInfo, value));
    }

    private static IGenericResult<RpcRequest> Malformed(ILogger logger, string reason)
        => GenericResult<RpcRequest>.Failure(TdsProtocolLog.Malformed(logger, "RPC request", reason));
}
