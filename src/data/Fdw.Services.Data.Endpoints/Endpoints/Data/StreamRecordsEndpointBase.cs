using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Messages;
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

    /// <summary>Gets the pending character count at which the response buffer is drained.</summary>
    /// <remarks>
    /// A buffer size, not a value standing in for missing configuration: it trades syscalls against
    /// the memory ceiling and changes no behaviour.
    /// </remarks>
    protected virtual int DrainThresholdChars => 8 * 1024;

    /// <summary>Gets the pending byte count at which a binary response's buffer is drained.</summary>
    /// <remarks>A buffer size, like <see cref="DrainThresholdChars"/>: it trades syscalls against memory and changes no behaviour.</remarks>
    protected virtual int DrainThresholdBytes => 32 * 1024;

    /// <summary>
    /// Gets the format choices the request states, by the keys the formats read (<c>Separator</c>, <c>HasHeader</c>, <c>QuoteChar</c>,
    /// <c>WriteByteOrderMark</c>, <c>NeutralizeFormulas</c>, <c>RootElementName</c>, <c>RowElementName</c>, <c>SheetName</c> and so on).
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The stated choices; a choice the negotiated format needs and the request leaves out refuses the stream before it starts.</returns>
    /// <remarks>
    /// Why the request (FDW-3823): an export has no container to state them, and a choice is never defaulted. Mike: "why in the fuck
    /// would you make a selection that is a default". The caller, the console, sends them.
    /// </remarks>
    protected abstract IReadOnlyDictionary<string, object> FormatChoices(TRequest request);

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

        var (format, refusal) = ResolveFormat(resource);
        if (format is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(refusal!), HttpContext)).ConfigureAwait(false);
            return;
        }

        var slot = Slots.GetOrAdd(GetType(), _ => new SemaphoreSlim(_streaming.MaxConcurrentStreams, _streaming.MaxConcurrentStreams));
        if (!await slot.WaitAsync(0, ct).ConfigureAwait(false))
        {
            StreamRecordsEndpointLog.ConcurrencyLimitReached(Logger, resource, _streaming.MaxConcurrentStreams, _streaming.MaxConcurrentStreams);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(StreamRecordsEndpointLog.StreamRecordsLimitReached(Logger, resource, _streaming.MaxConcurrentStreams)), HttpContext)).ConfigureAwait(false);
            return;
        }

        try
        {
            // Why the open's own result is the answer (FDW-1162): nothing is written yet, so it still carries a real
            // status. Its code decides that status -- a refusal is a 400 that says why, a missing data set a 404 -- where
            // answering every one of them 500 with the internal message reported a refused caller as a server fault.
            var opened = await Open(req, resource, ct).ConfigureAwait(false);
            if (opened.IsFailure || opened.Value is null)
            {
                StreamRecordsEndpointLog.StreamNotStarted(Logger, resource);
                await Answer(opened).ConfigureAwait(false);
                return;
            }

            await using var source = opened.Value;
            StreamRecordsEndpointLog.CursorOpened(Logger, resource, source.Schema.Fields.Count);
            StreamRecordsEndpointLog.FormatNegotiated(Logger, resource, format.Format, format.MediaType);

            // Why before the stream starts (FDW-3823): a choice the request leaves out is still answered with a status naming it.
            var options = Options(format, source, req, resource);
            if (options.IsFailure || options.Value is not { } built)
            {
                StreamRecordsEndpointLog.StreamNotStarted(Logger, resource);
                await Answer(options).ConfigureAwait(false);
                return;
            }

            // Why the first record is read before the status is written (FDW-4983): a read refused on its first page has written
            // nothing yet, so its own status still answers it, where a 200 cut short hid the refusal in a trailer.
            await using var records = source.Read(ct).GetAsyncEnumerator(ct);
            var first = await First(records, resource, ct).ConfigureAwait(false);
            if (first is { IsFailure: true })
            {
                StreamRecordsEndpointLog.StreamNotStarted(Logger, resource);
                await Answer(first).ConfigureAwait(false);
                return;
            }

            await Stream(records, first, format, built, resource, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException disconnected) when (ct.IsCancellationRequested)
        {
            // The client went away before anything was written: the client's decision, with nobody left to answer.
            StreamRecordsEndpointLog.ClientDisconnected(Logger, disconnected, resource, 0);
        }
        finally
        {
            slot.Release();
        }
    }

    // Opening the records is the endpoint's own code: whatever it throws is answered as a coded failure, never a bare 500 (FDW-4983).
    private async Task<IGenericResult<IRecordSource<DataRecord>>> Open(TRequest req, string resource, CancellationToken ct)
    {
        try
        {
            return await OpenRecords(req, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(StreamRecordsEndpointLog.StreamOpenThrew(Logger, ex, resource, ex.Message));
        }
    }

    // The format's options from the request's choices. A choice the request leaves out or states unusably is the caller's to fix, so each
    // is its own refusal naming the format and the choice (FDW-4983): the format's own messages were written for a container's metadata,
    // a fault of the host, and answered 500 with no detail. Any other failure of a format answers with that format's own code.
    private IGenericResult<RowWriterOptions> Options(IRecordWriterType format, IRecordSource<DataRecord> source, TRequest req, string resource)
    {
        try
        {
            var options = format.CreateOptions(ColumnsOf(source), FormatChoices(req), resource);
            return options.IsSuccess ? options : ChoiceRefusal(options, options.FailureReason().Message, format.Format, resource);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<RowWriterOptions>.Failure(StreamRecordsEndpointLog.StreamOpenThrew(Logger, ex, resource, ex.Message));
        }
    }

    // The refusal for a failed options build, by its reason; the reason's text is read by the caller, where the build is known to have failed.
    private IGenericResult<RowWriterOptions> ChoiceRefusal(IGenericResult<RowWriterOptions> failed, string reason, string format, string resource) =>
        failed.FirstFailureMessage()?.Number switch
        {
            RowSourceLog.FormatChoiceNotStatedId => GenericResult<RowWriterOptions>.Failure(
                StreamRecordsEndpointLog.StreamRecordsFormatChoiceNotStated(Logger, resource, format, reason)),
            RowSourceLog.FormatChoiceNotUsableId => GenericResult<RowWriterOptions>.Failure(
                StreamRecordsEndpointLog.StreamRecordsFormatChoiceNotUsable(Logger, resource, format, reason)),
            _ => failed,
        };

    // The first record, or null when there is none; a read that throws is a coded failure, as an open that throws is.
    private async Task<IGenericResult<DataRecord>?> First(IAsyncEnumerator<IGenericResult<DataRecord>> records, string resource, CancellationToken ct)
    {
        try
        {
            return await records.MoveNextAsync().ConfigureAwait(false) ? records.Current : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return GenericResult<DataRecord>.Failure(StreamRecordsEndpointLog.StreamOpenThrew(Logger, ex, resource, ex.Message));
        }
    }

    private Task<FastEndpoints.Void> Answer(IGenericResult failure)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failure, HttpContext);
        return Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode));
    }

    private static List<string> ColumnsOf(IRecordSource<DataRecord> source)
    {
        var columns = new List<string>(source.Schema.Fields.Count);
        foreach (var field in source.Schema.Fields)
        {
            columns.Add(field.Name);
        }

        return columns;
    }

    private async Task Stream(
        IAsyncEnumerator<IGenericResult<DataRecord>> records,
        IGenericResult<DataRecord>? first,
        IRecordWriterType format,
        RowWriterOptions options,
        string resource,
        CancellationToken ct)
    {
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

        using var sink = new ResponseSink(HttpContext.Response.Body, format.WritesBinary, DrainThresholdChars, DrainThresholdBytes);
        var writer = sink.CreateWriter(format, options);
        var rowCount = 0L;
        string? failure = null;
        var ceilingHit = false;

        try
        {
            // The first record was read before the status was written; the rest follow it from the same reader.
            for (var record = first; record is not null; record = await records.MoveNextAsync().ConfigureAwait(false) ? records.Current : null)
            {
                if (record.IsFailure)
                {
                    failure = record.FailureReason().Message;
                    break;
                }

                if (rowCount >= _streaming.MaxRows)
                {
                    ceilingHit = true;
                    break;
                }

                writer.Write(record.Value);
                rowCount++;
                await sink.DrainIfFull(ct).ConfigureAwait(false);
            }

            await sink.Finish(writer, ct).ConfigureAwait(false);
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
            await sink.Release(writer).ConfigureAwait(false);
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
        StreamRecordsEndpointLog.StreamCompleted(Logger, resource, rowCount, sink.DrainedByteCount);
    }

    private void AppendStatusTrailer(string value)
    {
        if (HttpContext.Response.SupportsTrailers())
        {
            HttpContext.Response.AppendTrailer(StatusTrailerName, value);
        }
    }

    // Why a query value as well as the Accept header: a browser link or a form post cannot set Accept, and a download is
    // exactly a link. It wins over Accept because it is the more specific statement, made by whoever built the link.
    // Why the file extension as well as the format's name: the extension is what a person writes (csv, xlsx) and the name is the
    // format's own (Delimited). The extension is the format's own statement about itself, so no table is kept beside this.
    private static IRecordWriterType? FormatNamed(string name)
    {
        var wanted = name.Trim();
        foreach (var candidate in RecordWriterTypes.All())
        {
            if (!ReferenceEquals(candidate, RecordWriterTypes.NotFound)
                && !string.IsNullOrEmpty(candidate.MediaType)
                && (string.Equals(candidate.Format, wanted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(candidate.FileExtension, wanted, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return null;
    }

    // The format the request names, by ?format= or the Accept header; the coded message of the refusal when it names none (400) or one no
    // writer serves (406). Why no format of the endpoint's own (FDW-3823): the format is the caller's choice, and an
    // endpoint that served one when the caller named none made that selection for it. Mike: "why in the fuck would you make a
    // selection that is a default".
    private (IRecordWriterType? Format, IGenericMessage? Refusal) ResolveFormat(string resource)
    {
        var named = HttpContext.Request.Query["format"].ToString();
        var accept = HttpContext.Request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(named) && StatesNoPreference(accept))
        {
            StreamRecordsEndpointLog.FormatNotStated(Logger, resource);
            return (null, StreamRecordsEndpointLog.StreamRecordsFormatNotStated(Logger, resource, AvailableFormatNames()));
        }

        var format = string.IsNullOrWhiteSpace(named) ? Accepted(accept) : FormatNamed(named);
        if (format is not null)
        {
            return (format, null);
        }

        StreamRecordsEndpointLog.FormatNotAcceptable(Logger, resource, string.IsNullOrWhiteSpace(named) ? accept : named);
        return (null, string.IsNullOrWhiteSpace(named)
            ? StreamRecordsEndpointLog.StreamRecordsAcceptNotSatisfied(Logger, resource, accept, AvailableMediaTypes())
            : StreamRecordsEndpointLog.StreamRecordsFormatNotNamed(Logger, resource, named, AvailableFormatNames()));
    }

    private static IRecordWriterType? Accepted(string acceptHeader)
    {
        var requested = RecordWriterTypes.ByAcceptHeader(acceptHeader);
        return ReferenceEquals(requested, RecordWriterTypes.NotFound) ? null : requested;
    }

    // Why a wildcard names no format: */* says the client takes whatever is served, and the endpoint serves no format the caller did
    // not name, so it is the same as no Accept header at all.
    private static bool StatesNoPreference(string acceptHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptHeader))
        {
            return true;
        }

        foreach (var entry in acceptHeader.Split(','))
        {
            var trimmed = entry.Trim();
            var separator = trimmed.IndexOf(';', StringComparison.Ordinal);
            if (separator >= 0)
            {
                trimmed = trimmed.Substring(0, separator).Trim();
            }

            if (trimmed.Length > 0 && !trimmed.EndsWith("/*", StringComparison.Ordinal) && !string.Equals(trimmed, "*", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Where a response's bytes go: a text format through a <see cref="DrainingTextWriter"/>, a binary one through a
    /// <see cref="DrainingStream"/>, each drained between records so no synchronous write reaches the response body.
    /// </summary>
    private sealed class ResponseSink : IDisposable
    {
        private readonly DrainingTextWriter? _text;
        private readonly DrainingStream? _bytes;

        internal ResponseSink(System.IO.Stream body, bool binary, int drainThresholdChars, int drainThresholdBytes)
        {
            if (binary)
            {
                _bytes = new DrainingStream(body, drainThresholdBytes);
            }
            else
            {
                _text = new DrainingTextWriter(body, new UTF8Encoding(false), drainThresholdChars);
            }
        }

        internal IRecordWriter<DataRecord> CreateWriter(IRecordWriterType format, RowWriterOptions options)
            => _bytes is not null ? format.CreateBinaryWriter(_bytes, options) : format.CreateWriter(_text!, options);

        internal long DrainedByteCount => _bytes is not null ? _bytes.DrainedByteCount : _text!.DrainedByteCount;

        internal ValueTask DrainIfFull(CancellationToken ct)
            => _bytes is not null ? _bytes.DrainIfFull(ct) : _text!.DrainIfFull(ct);

        // Why a binary writer is disposed before the last drain: a binary file is not a file until its writer finishes it (a
        // workbook's central directory is written on dispose), and that is only after the last row. A failed response never
        // gets here, so no half file ends with the parts that would let it pass for a whole one.
        internal async Task Finish(IRecordWriter<DataRecord> writer, CancellationToken ct)
        {
            if (_bytes is not null)
            {
                await writer.DisposeAsync().ConfigureAwait(false);
                await _bytes.Complete(ct).ConfigureAwait(false);
                return;
            }

            writer.Flush();
            await _text!.Complete(ct).ConfigureAwait(false);
        }

        // A text writer is always disposed here. A binary writer was disposed by Finish when the file was complete, and is left
        // undisposed after a failure: disposing it would finish the workbook, and nothing after a failure should.
        internal ValueTask Release(IRecordWriter<DataRecord> writer)
            => _text is not null ? writer.DisposeAsync() : default;

        // Both are buffers over the response body: disposing them releases the buffers and writes nothing.
        public void Dispose()
        {
            _text?.Dispose();
            _bytes?.Dispose();
        }
    }

    private static string AvailableFormatNames()
    {
        var available = new List<string>();
        foreach (var candidate in RecordWriterTypes.All())
        {
            if (!ReferenceEquals(candidate, RecordWriterTypes.NotFound) && !string.IsNullOrEmpty(candidate.MediaType))
            {
                available.Add($"{candidate.Format} ({candidate.FileExtension})");
            }
        }

        return string.Join(", ", available);
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
