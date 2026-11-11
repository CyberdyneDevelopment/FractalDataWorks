using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// Reads an HTTP source's complete set as one stream of records (FDW-4938): it asks for a page, streams that page's rows as they arrive,
/// releases the response, and asks its paging plan for the next page, until the remote says it is done.
/// </summary>
/// <remarks>
/// <para>
/// Each page is read through the protocol's row reader for the container's format (<see cref="IHttpProtocol.TryOpenRecordSource"/>), so a
/// paged read and a single request agree about what a row is. Every page carries the filter and ordering of the translated query: the plan
/// writes only its own paging parameters, or follows the address the remote gives.
/// </para>
/// <para>
/// A failure ends the stream with that failure as its last item, never by stopping quietly: a request the remote refuses, a page that cannot
/// be read, and every stop the plan makes (a page larger than asked, a repeated or malformed next link, too many pages). Rows already read are
/// not taken back, so a consumer treats the stream as failed.
/// </para>
/// <para>
/// Only one page is held at a time. A style that reads the page body for its next link or token (<see cref="HttpPagingPlan.ReadsBody"/>) keeps
/// that page's bytes, which the JSON reader parses whole anyway; any other style streams the body.
/// </para>
/// </remarks>
public sealed class HttpPagedRecordSource : IRecordSource<DataRecord>
{
    private readonly HttpClient _client;
    private readonly HttpRequestMessage _query;
    private readonly IHttpProtocol _protocol;
    private readonly IStorageContainer _container;
    private readonly IReadOnlyList<IDataField> _fields;
    private readonly HttpPagingPlan _plan;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="HttpPagedRecordSource"/> class.</summary>
    /// <param name="client">The connection's client; the connection owns it.</param>
    /// <param name="query">The translated query, carrying no paging of its own. This source owns and disposes it.</param>
    /// <param name="protocol">The connection's protocol, whose row reader reads each page.</param>
    /// <param name="container">The container read, with its format and the format's choices.</param>
    /// <param name="fields">The container's fields, the records' schema.</param>
    /// <param name="plan">The read's paging plan.</param>
    /// <param name="logger">The logger; none logs nothing.</param>
    public HttpPagedRecordSource(
        HttpClient client,
        HttpRequestMessage query,
        IHttpProtocol protocol,
        IStorageContainer container,
        IReadOnlyList<IDataField> fields,
        HttpPagingPlan plan,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(plan);
        _client = client;
        _query = query;
        _protocol = protocol;
        _container = container;
        _fields = fields;
        _plan = plan;
        _logger = logger ?? NullLogger.Instance;
        Schema = new RecordSchema(fields);
    }

    /// <inheritdoc/>
    public RecordSchema Schema { get; }

    /// <inheritdoc/>
    /// <remarks>Reads the same pages as the asynchronous read, waiting on each request.</remarks>
    public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

    /// <inheritdoc/>
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_plan.Take == 0)
            yield break;

        HttpPagingLog.ReadingPaged(_logger, _plan.ConnectionName, _plan.Style, _plan.PageSize, _plan.MaxPages);
        var read = new ReadState();
        var address = (Uri?)_plan.FirstPage(_query.RequestUri!);
        while (address is not null && !read.Ended)
        {
            read.Page++;
            await foreach (var record in ReadPage(Absolute(address), read, cancellationToken).ConfigureAwait(false))
                yield return record;
            address = read.Next;
        }

        if (!read.Failed)
            HttpPagingLog.ReadComplete(_logger, _plan.ConnectionName, read.Page, read.Taken);
    }

    // One page: asked for, its rows streamed (less the rows the read drops itself, up to the rows it takes), then released before the next
    // page's address is asked of the plan. A failure is the page's last item and ends the read.
    private async IAsyncEnumerable<IGenericResult<DataRecord>> ReadPage(Uri address, ReadState read, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        read.Next = null;
        HttpPagingLog.RequestingPage(_logger, _plan.ConnectionName, read.Page, address.AbsoluteUri);
        var sent = await Send(read.Page, address, cancellationToken).ConfigureAwait(false);
        if (sent.IsFailure)
        {
            yield return read.Fail(sent.ToNewResult<DataRecord>());
            yield break;
        }

        using var response = sent.Value!;
        byte[] body = _plan.ReadsBody ? await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false) : [];
        var content = _plan.ReadsBody ? new MemoryStream(body, writable: false) : await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var contentScope = content.ConfigureAwait(false);

        var opened = _protocol.TryOpenRecordSource(_container, content, _fields, _logger);
        if (opened.IsFailure || opened.Value is null)
        {
            yield return read.Fail(NotOpened(opened));
            yield break;
        }

        var rows = 0;
        await using (opened.Value.ConfigureAwait(false))
        {
            await foreach (var record in opened.Value.Read(cancellationToken).ConfigureAwait(false))
            {
                if (record.IsFailure)
                {
                    yield return read.Fail(record);
                    yield break;
                }

                rows++;
                if (read.Drops(_plan.SkipHere))
                    continue;

                yield return record;
                if (read.Keeps(_plan.Take))
                    yield break;
            }
        }

        var next = _plan.Next(new HttpPageOutcome(read.Page, address, rows, response.Headers, body));
        if (next.IsFailure)
            yield return read.Fail(next.ToNewResult<DataRecord>());
        else
            read.Next = next.Value;
    }

    private IGenericResult<DataRecord> NotOpened(IGenericResult<IRecordSource<DataRecord>?> opened)
        => opened.IsFailure
            ? opened.ToNewResult<DataRecord>()
            : GenericResult<DataRecord>.Failure(
                HttpResultCodes.ByName("ResponseRowExtractionFailed"),
                ResultDetails.Create().With("ErrorMessage", $"No row source reader available for format '{_container.Format?.Name}'."));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _query.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    // A relative page address is relative to the connection's base address, as the translated query's is.
    private Uri Absolute(Uri address)
        => address.IsAbsoluteUri || _client.BaseAddress is null ? address : new Uri(_client.BaseAddress, address);

    // The first page is the translated query itself, so a body it carries (a single-response read) goes with it; every later page is a GET
    // of the same method and headers at the plan's address, which a paging style only ever writes into the address.
    private async Task<IGenericResult<HttpResponseMessage>> Send(int page, Uri address, CancellationToken cancellationToken)
    {
        HttpRequestMessage request;
        if (page == 1)
        {
            _query.RequestUri = address;
            request = _query;
        }
        else
        {
            request = new HttpRequestMessage(_query.Method, address);
            foreach (var header in _query.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        try
        {
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return GenericResult<HttpResponseMessage>.Success(response);

            using (response)
            {
                return GenericResult<HttpResponseMessage>.Failure(
                    HttpResultCodes.ByName("HttpErrorResponse"),
                    ResultDetails.Create(
                        "StatusCode", (int)response.StatusCode,
                        "ReasonPhrase", response.ReasonPhrase ?? string.Empty,
                        "ErrorContent", await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false))
                        .With("Page", page));
            }
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<HttpResponseMessage>.Failure(
                HttpResultCodes.ByName("RequestFailed"),
                ResultDetails.Create().With("Page", page).With("ErrorMessage", ex.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GenericResult<HttpResponseMessage>.Failure(HttpResultCodes.ByName("RequestTimeout"));
        }
        finally
        {
            if (page > 1)
                request.Dispose();
        }
    }

    // Where one read stands across its pages: the page it is on, the rows it has dropped and kept, the next page's address, and whether it
    // has ended (its rows all taken, or a failure).
    private sealed class ReadState
    {
        private int _dropped;

        public int Page { get; set; }

        public long Taken { get; private set; }

        public Uri? Next { get; set; }

        public bool Failed { get; private set; }

        public bool Done { get; private set; }

        public bool Ended => Failed || Done;

        public IGenericResult<DataRecord> Fail(IGenericResult<DataRecord> failure)
        {
            Failed = true;
            return failure;
        }

        // Whether this row is one the read drops itself, before the rows it returns.
        public bool Drops(int skipHere)
        {
            if (_dropped >= skipHere)
                return false;
            _dropped++;
            return true;
        }

        // Counts a row returned, and whether it was the last the read takes.
        public bool Keeps(int? take)
        {
            Taken++;
            Done = take is int most && Taken >= most;
            return Done;
        }
    }
}
