using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.RowSources.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

/// <summary>
/// Interface defining the contract for HTTP protocol implementations.
/// </summary>
/// <remarks>
/// <para>
/// HTTP protocols encapsulate both request building and response processing logic.
/// This allows different protocols (REST, SOAP, GraphQL, OData) to be used with
/// the same HttpConnection by simply selecting a different protocol.
/// </para>
/// <para>
/// Protocol implementations handle:
/// <list type="bullet">
/// <item><description>Request translation - converting IDataCommand to HttpRequestMessage</description></item>
/// <item><description>Response processing - parsing responses and extracting results</description></item>
/// <item><description>Protocol-specific concerns (SOAP envelopes, GraphQL queries, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IHttpProtocol : ITypeOption<int, HttpProtocolBase>
{
    /// <summary>
    /// Gets the description of this HTTP protocol.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the default content type for requests using this protocol.
    /// </summary>
    string DefaultContentType { get; }

    /// <summary>
    /// Translates a data command into an HTTP request message.
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The storage container with schema information.</param>
    /// <param name="context">The protocol context with configuration and resolved secrets.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the HTTP request message or failure information.</returns>
    Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes an HTTP response and extracts the result.
    /// </summary>
    /// <param name="response">The HTTP response to process.</param>
    /// <param name="container">The storage container with schema information.</param>
    /// <param name="resultType">The expected result type.</param>
    /// <param name="context">The protocol context with configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the extracted value or failure information.</returns>
    Task<IGenericResult<object?>> ProcessResponse(
        HttpResponseMessage response,
        IStorageContainer container,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens a streaming record source over response content for the container's format, or returns
    /// null when no reader exists for it.
    /// </summary>
    /// <param name="container">The container carrying the format + row-extraction metadata.</param>
    /// <param name="content">The response content stream, read lazily by the returned source.</param>
    /// <param name="fields">The container's fields, used to build the record schema once.</param>
    /// <param name="logger">Optional logger threaded to the created source.</param>
    /// <returns>A record source over the content, or null when the format has no reader here.</returns>
    /// <remarks>
    /// On the contract rather than only on the base class because the protocol is what knows its
    /// format: a connection holds an <see cref="IHttpProtocol"/> and must be able to ask for a
    /// streaming read without testing the concrete protocol type first.
    /// </remarks>
    IRecordSource<DataRecord>? TryOpenRecordSource(
        IStorageContainer container,
        Stream content,
        IReadOnlyList<IDataField> fields,
        ILogger? logger = null);
}
