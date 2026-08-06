using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.CommandCapabilities;

namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Optional connection capability: write a batch of records to an HTTP endpoint by serializing
/// through the container's configured format and posting to the endpoint declared in the
/// <see cref="HttpRecordWriterCapability"/> fields.
/// </summary>
/// <remarks>
/// <para>
/// Why a separate capability interface (not a member of <c>IHttpConnection</c>): keeps the
/// write-path opt-in — only connections whose type adds <see cref="HttpRecordWriterCapability"/>
/// to <c>SupportedCommands</c> need to implement this interface. Callers feature-detect it;
/// connections that do not support HTTP record writes are a structured failure, never a fallback.
/// </para>
/// </remarks>
public interface IHttpRecordWriterConnection
{
    /// <summary>
    /// Gets the <see cref="System.Net.Http.HttpClient"/> used to send records to the HTTP endpoint.
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    /// Writes a batch of records to the HTTP endpoint configured on the container.
    /// </summary>
    /// <param name="container">
    /// The configured container (format + field schema + endpoint metadata from the
    /// <see cref="HttpRecordWriterCapability"/> fields).
    /// </param>
    /// <param name="rows">The rows to write as flat name→value maps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result carrying the written record count, or a failure carrying the error.
    /// </returns>
    Task<IGenericResult<int>> WriteRecords(
        IDataContainer container,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);
}
