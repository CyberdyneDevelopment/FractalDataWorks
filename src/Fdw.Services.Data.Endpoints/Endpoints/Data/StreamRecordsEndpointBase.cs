using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for streaming records to the response in any registered format, instead of
/// materialising them and sending one body.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <remarks>
/// The endpoint opens a record cursor, negotiates a format from the <c>Accept</c> header against the
/// registered <see cref="RecordWriterTypes"/>, and pumps records through that format's writer into the
/// response body a batch at a time. Memory stays bounded by the drain threshold however many records
/// are read, and the first byte reaches the client before the last record is read.
/// <para>
/// <b>Failure has two shapes and they are not interchangeable.</b> Everything that can be checked
/// before the first byte — negotiation, the concurrency limit, opening the cursor — is checked there,
/// so it still carries a real status code. Once the first byte is written the status code is fixed, so
/// a later failure is reported in the <see cref="RecordWriterTypes.StreamStatusTrailerName"/> trailer and the response is left
/// deliberately unterminated. A truncated body is a detectable failure; a complete body that silently
/// stops early is not, and is never produced here.
/// </para>
/// </remarks>
public abstract class StreamRecordsEndpointBase<TRequest> : Endpoint<TRequest>
    where TRequest : notnull, new()
{
    /// <summary>The response trailer carrying the outcome of a stream that had already started.</summary>
    /// <remarks>Read from <see cref="RecordWriterTypes.StreamStatusTrailerName"/>, where a deployment overrides it.</remarks>
    public static string StatusTrailerName => RecordWriterTypes.StreamStatusTrailerName;

    // Why keyed on the concrete endpoint type rather than a field: a static field on a generic base is
    // shared by every endpoint closing it over the same request type, which would silently pool two
    // unrelated endpoints' limits together.
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> Slots = new();

    private readonly ResponseStreamingOptions _streaming;

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Initializes a new instance of the <see cref="StreamRecordsEndpointBase{TRequest}"/> class.</summary>
    /// <param name="streaming">The host's configured streaming limits.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// The limits come from the host's ResponseStreaming configuration rather than from each endpoint, so every
    /// streaming endpoint on a host is bound by the same ceiling. A host that has not configured them cannot
    /// construct a streaming endpoint at all: the dependency is missing, and that is the failure.
    /// </remarks>
    protected StreamRecordsEndpointBase(ResponseStreamingOptions streaming, ILogger logger)
    {
        _streaming = streaming ?? throw new ArgumentNullException(nameof(streaming));
        Logger = logger ?? NullLogger<StreamRecordsEndpointBase<TRequest>>.Instance;
    }

    /// <summary>Gets the plural resource name used for the documentation tag.</summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the route this endpoint is served at.</summary>
    protected abstract string Route { get; }

    /// <summary>Gets the authorization policy required to stream this resource.</summary>
    protected abstract string PolicyName { get; }

    /// <summary>
    /// Gets the record format served when the client expressed no preference, by
    /// <see cref="IRecordWriterType.Format"/> name.
    /// </summary>
    /// <remarks>
    /// Stated by the endpoint rather than defaulted by the framework: an endpoint knows what its
    /// callers read, and a framework picking for it would make the served format depend on which
    /// formats happened to be registered.
    /// </remarks>
    protected abstract string DefaultFormat { get; }

    /// <summary>Gets the pending character count at which the response buffer is drained.</summary>
    /// <remarks>
    /// A buffer size, not a value standing in for missing configuration: it trades syscalls against
    /// the memory ceiling and changes no behaviour.
    /// </remarks>
    protected virtual int DrainThresholdChars => 8 * 1024;

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>Gets the endpoint summary for OpenAPI documentation.</summary>
    protected virtual string EndpointSummary => $"Stream {ResourceName}";

    /// <summary>Gets the endpoint description for OpenAPI documentation.</summary>
    protected virtual string EndpointDescription =>
        $"Streams {ResourceName} records progressively in the format requested by the Accept header. " +
        $"The outcome is reported in the {StatusTrailerName} trailer; a truncated body means the stream failed.";

    /// <summary>
    /// Opens the record cursor to stream. The cursor owns its reader and connection and is disposed by
    /// the base once the response is finished.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success the cursor; on failure a structured non-success result.</returns>
    protected abstract Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecords(
        TRequest request, CancellationToken cancellationToken);

    /// <summary>Gets the identifier of the resource being streamed, for logging.</summary>
    /// <param name="request">The request.</param>
    /// <returns>An identifier naming what is being streamed.</returns>
    protected virtual string ResourceIdentifier(TRequest request) => ResourceName;

    /// <summary>Hook for additional endpoint configuration, called from <see cref="Configure"/>.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get(Route);
        Policies(PolicyName);
        Tags(EndpointTag);

        // Why the metadata: response-buffering middleware would otherwise hold this response to set a
        // Content-Length, turning it back into a materialising endpoint with no outward sign.
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));

        Summary(summary =>
        {
            summary.Summary = EndpointSummary;
            summary.Description = EndpointDescription;
        });

        ConfigureEndpoint();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var resource = ResourceIdentifier(req);

        var format = NegotiateFormat(HttpContext.Request.Headers.Accept.ToString());
        if (format is null)
        {
            StreamRecordsEndpointLog.FormatNotAcceptable(Logger, resource, HttpContext.Request.Headers.Accept.ToString());
            AddError($"No available record format satisfies the Accept header. Available: {AvailableMediaTypes()}.");
            await Send.ErrorsAsync(406, ct).ConfigureAwait(false);
            return;
        }

        var slot = Slots.GetOrAdd(GetType(), _ => new SemaphoreSlim(_streaming.MaxConcurrentStreams, _streaming.MaxConcurrentStreams));
        if (!await slot.WaitAsync(0, ct).ConfigureAwait(false))
        {
            StreamRecordsEndpointLog.ConcurrencyLimitReached(Logger, resource, _streaming.MaxConcurrentStreams, _streaming.MaxConcurrentStreams);
            AddError($"This endpoint is already streaming its limit of {_streaming.MaxConcurrentStreams} concurrent responses.");
            await Send.ErrorsAsync(429, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            // Why the open's own result is the answer (FDW-1162): nothing is written yet, so it still carries a real
            // status. Its code decides that status -- a refusal is a 400 that says why, a missing data set a 404 -- where
            // answering every one of them 500 with the internal message reported a refused caller as a server fault.
            var opened = await OpenRecords(req, ct).ConfigureAwait(false);
            if (opened.IsFailure || opened.Value is null)
            {
                StreamRecordsEndpointLog.StreamNotStarted(Logger, resource);
                var (statusCode, problem) = ResultHttpStatusMapper.Map(opened, HttpContext);
                await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
                return;
            }

            await using var source = opened.Value;
            StreamRecordsEndpointLog.CursorOpened(Logger, resource, source.Schema.Fields.Count);
            StreamRecordsEndpointLog.FormatNegotiated(Logger, resource, format.Format, format.MediaType);

            await Stream(source, format, resource, ct).ConfigureAwait(false);
        }
        finally
        {
            slot.Release();
        }
    }

    private async Task Stream(
        IRecordSource<DataRecord> source, IRecordWriterType format, string resource, CancellationToken ct)
    {
        var columns = new List<string>(source.Schema.Fields.Count);
        foreach (var field in source.Schema.Fields)
        {
            columns.Add(field.Name);
        }

        HttpContext.Response.StatusCode = 200;
        HttpContext.Response.ContentType = format.MediaType;

        // Why attachment: an export is a file the caller keeps, and without it a browser renders a CSV inline
        // instead of downloading it. A programmatic client ignores the header, so it costs them nothing. The
        // extension is the format's own statement about itself, not a table kept beside the endpoint.
        var disposition = new ContentDispositionHeaderValue("attachment");
        disposition.SetHttpFileName(string.Concat(resource, ".", format.FileExtension));
        HttpContext.Response.Headers.ContentDisposition = disposition.ToString();
        if (HttpContext.Response.SupportsTrailers())
        {
            HttpContext.Response.DeclareTrailer(StatusTrailerName);
        }

        var draining = new DrainingTextWriter(HttpContext.Response.Body, new UTF8Encoding(false), DrainThresholdChars);
        var writer = format.CreateWriter(draining, format.CreateOptions(columns));
        var rowCount = 0L;
        string? failure = null;
        var ceilingHit = false;

        try
        {
            await foreach (var record in source.Read(ct).ConfigureAwait(false))
            {
                if (record.IsFailure)
                {
                    failure = record.CurrentMessage ?? "A record could not be read.";
                    break;
                }

                if (rowCount >= _streaming.MaxRows)
                {
                    ceilingHit = true;
                    break;
                }

                writer.Write(record.Value);
                rowCount++;
                await draining.DrainIfFull(ct).ConfigureAwait(false);
            }

            writer.Flush();
            await draining.Complete(ct).ConfigureAwait(false);
        }
        catch (NotSupportedException unserializable)
        {
            // A column whose type has no defined serialized form. It reaches here rather than
            // corrupting the output because RecordValueText refuses to stringify what it does not
            // understand; routing it into the same failure path means the response is truncated and
            // the reason reported, exactly like a source failure.
            failure = unserializable.Message;
        }
        catch (OperationCanceledException disconnected)
        {
            // Why this is not reported as a failure: the client going away is the client's decision, and
            // there is no longer anybody to report it to. Disposal still releases the cursor's connection.
            StreamRecordsEndpointLog.ClientDisconnected(Logger, disconnected, resource, rowCount);
            return;
        }
        finally
        {
            await writer.DisposeAsync().ConfigureAwait(false);
            await draining.DisposeAsync().ConfigureAwait(false);
        }

        if (failure is not null)
        {
            StreamRecordsEndpointLog.StreamFailedMidResponse(Logger, resource, rowCount, failure);
            AppendStatusTrailer($"error; rows={rowCount}; reason={failure}");

            // Why the response is aborted rather than completed: the body is already partly written and
            // the status code says 200. Completing it would hand the client a well-formed response that
            // silently omits the rest of the data. Aborting makes the truncation visible even to a
            // client that never reads the trailer.
            HttpContext.Abort();
            return;
        }

        if (ceilingHit)
        {
            StreamRecordsEndpointLog.RowCeilingReached(Logger, resource, _streaming.MaxRows);
            AppendStatusTrailer($"truncated; rows={rowCount}; limit={_streaming.MaxRows}");

            // Why a ceiling hit aborts rather than ending cleanly: the response is short of what the
            // source held, and response trailers exist only on HTTP/2 and later - over HTTP/1.1
            // SupportsTrailers() is false and the reason goes nowhere. Ending cleanly would hand an
            // HTTP/1.1 client a well-formed body it cannot tell from a complete one, which is the
            // silent truncation this endpoint must never produce. Aborting is visible on every
            // protocol version; the trailer adds the reason where the protocol carries one.
            HttpContext.Abort();
            return;
        }

        AppendStatusTrailer($"complete; rows={rowCount}");
        StreamRecordsEndpointLog.StreamCompleted(Logger, resource, rowCount, draining.DrainedByteCount);
    }

    private void AppendStatusTrailer(string value)
    {
        if (HttpContext.Response.SupportsTrailers())
        {
            HttpContext.Response.AppendTrailer(StatusTrailerName, value);
        }
    }

    private IRecordWriterType? NegotiateFormat(string acceptHeader)
    {
        var requested = RecordWriterTypes.ByAcceptHeader(acceptHeader);
        if (!ReferenceEquals(requested, RecordWriterTypes.NotFound))
        {
            return requested;
        }

        if (!StatesNoPreference(acceptHeader))
        {
            return null;
        }

        var fallbackToDeclared = RecordWriterTypes.ByName(DefaultFormat);
        return ReferenceEquals(fallbackToDeclared, RecordWriterTypes.NotFound) ? null : fallbackToDeclared;
    }

    // Why a wildcard is treated as no preference rather than as an unsatisfiable request: */* says the
    // client will take whatever the endpoint serves, which is what DefaultFormat states.
    private static bool StatesNoPreference(string acceptHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptHeader))
        {
            return true;
        }

        foreach (var entry in acceptHeader.Split(','))
        {
            var trimmed = entry.Trim();
            var separator = trimmed.IndexOf(';');
            if (separator >= 0)
            {
                trimmed = trimmed.Substring(0, separator).Trim();
            }

            if (trimmed.Length > 0 && !trimmed.EndsWith("/*", StringComparison.Ordinal) && trimmed != "*")
            {
                return false;
            }
        }

        return true;
    }

    private static string AvailableMediaTypes()
    {
        var available = new List<string>();
        foreach (var candidate in RecordWriterTypes.All())
        {
            if (!string.IsNullOrEmpty(candidate.MediaType))
            {
                available.Add(candidate.MediaType);
            }
        }

        return string.Join(", ", available);
    }
}
