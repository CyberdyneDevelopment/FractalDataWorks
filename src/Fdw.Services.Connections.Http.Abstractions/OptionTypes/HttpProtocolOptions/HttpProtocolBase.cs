using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.Results;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

/// <summary>
/// Base class for HTTP protocol types in the TypeOption pattern.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides common HTTP protocol functionality and defines
/// virtual extension points for protocol-specific behavior. Derived classes
/// override the virtual methods to implement protocol-specific logic.
/// </para>
/// <para>
/// Extension points:
/// <list type="bullet">
/// <item><description><see cref="BuildRequestBody"/> - Build the request body content</description></item>
/// <item><description><see cref="GetHttpMethod"/> - Determine the HTTP method to use</description></item>
/// <item><description><see cref="GetRequestPath"/> - Build the request path/URL</description></item>
/// <item><description><see cref="ConfigureRequestHeaders"/> - Add protocol-specific headers</description></item>
/// <item><description><see cref="ExtractResult"/> - Extract the result from response content</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class HttpProtocolBase : TypeOptionBase<int, HttpProtocolBase>, IHttpProtocol
{
    /// <summary>
    /// Default JSON serializer options for protocols that use JSON.
    /// </summary>
    protected static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpProtocolBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the HTTP protocol.</param>
    /// <param name="name">The name of the HTTP protocol.</param>
    /// <param name="description">The description of the HTTP protocol.</param>
    /// <param name="defaultContentType">The default content type for this protocol.</param>
    protected HttpProtocolBase(int id, string name, string description, string defaultContentType)
        : base(id, name)
    {
        Description = description;
        DefaultContentType = defaultContentType;
    }

    /// <inheritdoc/>
    public new string Description { get; }

    /// <inheritdoc/>
    public string DefaultContentType { get; }

    /// <inheritdoc/>
    public virtual async Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get request components via virtual methods
            var method = GetHttpMethod(command, container, context);
            var path = GetRequestPath(command, container, context);

            var request = new HttpRequestMessage(method, path);

            // Build body if needed
            var bodyResult = await BuildRequestBody(command, container, context, cancellationToken).ConfigureAwait(false);
            if (!bodyResult.IsSuccess)
            {
                return bodyResult.ToNewResult<HttpRequestMessage>();
            }

            if (bodyResult.Value is not null)
            {
                request.Content = bodyResult.Value;
            }

            // Configure headers
            ConfigureRequestHeaders(request, command, container, context);

            return GenericResult<HttpRequestMessage>.Success(request);
        }
        catch (Exception ex)
        {
            return GenericResult<HttpRequestMessage>.Failure(
                HttpResultCodes.ByName("CommandTranslationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IGenericResult<object?>> ProcessResponse(
        HttpResponseMessage response,
        IStorageContainer container,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Check for HTTP-level errors first
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("HttpErrorResponse"),
                ResultDetails.Create()
                    .With("StatusCode", (int)response.StatusCode)
                    .With("ReasonPhrase", response.ReasonPhrase ?? string.Empty)
                    .With("ErrorContent", errorContent));
        }

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
        {
            return GenericResult<object?>.Success(null);
        }

        if (IsRowCollectionType(resultType) && HasRowReaderForFormat(container))
        {
            return ExtractRowsFromContent(content, container);
        }

        // Delegate to virtual method for protocol-specific extraction
        return await ExtractResult(content, response, resultType, context, cancellationToken).ConfigureAwait(false);
    }

    #region Virtual Extension Points

    /// <summary>
    /// Gets the HTTP method to use for the request.
    /// </summary>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The HTTP method to use.</returns>
    protected virtual HttpMethod GetHttpMethod(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
    {
        // Default: GET for queries, POST for mutations
        return command.CommandType switch
        {
            "Query" => HttpMethod.Get,
            "Insert" => HttpMethod.Post,
            "Update" => HttpMethod.Put,
            "Delete" => HttpMethod.Delete,
            _ => HttpMethod.Post
        };
    }

    /// <summary>
    /// Gets the request path for the HTTP request.
    /// </summary>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The request path (relative or absolute URL).</returns>
    protected virtual string GetRequestPath(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
    {
        return container.Path?.PathValue ?? container.Name;
    }

    /// <summary>
    /// Builds the request body content.
    /// </summary>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP content for the request body, or null for no body.</returns>
    protected virtual Task<IGenericResult<HttpContent?>> BuildRequestBody(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Default: no body for GET, JSON body for mutations
        if (string.Equals(command.CommandType, "Query", StringComparison.Ordinal))
        {
            return Task.FromResult(GenericResult<HttpContent?>.Success(null));
        }

        // For commands with input data, serialize to JSON
        if (command is IDataCommandWithInput commandWithInput && commandWithInput.InputData is not null)
        {
            var json = JsonSerializer.Serialize(commandWithInput.InputData, DefaultJsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, DefaultContentType);
            return Task.FromResult(GenericResult<HttpContent?>.Success(content));
        }

        return Task.FromResult(GenericResult<HttpContent?>.Success(null));
    }

    /// <summary>
    /// Configures request headers for the HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    protected virtual void ConfigureRequestHeaders(
        HttpRequestMessage request,
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
    {
        // Default: set Accept header
        request.Headers.Accept.ParseAdd(DefaultContentType);
    }

    /// <summary>
    /// Extracts the result from the response content.
    /// </summary>
    /// <remarks>
    /// Row-collection result types are handled by <see cref="ProcessResponse"/> before this
    /// method is called; this method handles all other types via JSON deserialization.
    /// </remarks>
    /// <param name="content">The response content as a string.</param>
    /// <param name="response">The full HTTP response.</param>
    /// <param name="resultType">The expected result type.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted result.</returns>
    protected virtual Task<IGenericResult<object?>> ExtractResult(
        string content,
        HttpResponseMessage response,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Default: JSON deserialization
        try
        {
            var result = JsonSerializer.Deserialize(content, resultType, DefaultJsonOptions);
            return Task.FromResult(GenericResult<object?>.Success(result));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(GenericResult<object?>.Failure(
                HttpResultCodes.ByName("ResponseDeserializationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    #endregion

    #region Row Extraction Helpers

    /// <summary>
    /// Returns true when a row source reader is available for the container's format. The default
    /// implementation only knows its bundled JSON reader (this base lives in a netstandard2.0
    /// assembly that cannot reference the net10 <c>RecordSourceTypes</c> TypeCollection); the net10
    /// <c>RestProtocolBase</c> override answers for every registered format.
    /// </summary>
    /// <param name="container">The container whose <see cref="IStorageContainer.Format"/> is checked.</param>
    /// <returns>True when <see cref="TryCreateRowReader"/> can produce a reader for this format.</returns>
    protected virtual bool HasRowReaderForFormat(IStorageContainer container)
        => container.Format is not null
           && string.Equals(container.Format.Name, "Json", StringComparison.Ordinal);

    /// <summary>
    /// Creates a row source reader for the container's format over the supplied content stream, or
    /// returns null when no reader is available for that format.
    /// </summary>
    /// <param name="container">The container carrying the format + row-extraction metadata.</param>
    /// <param name="content">The response content stream.</param>
    /// <returns>A reader positioned before the first row, or null when the format is unsupported here.</returns>
    /// <remarks>
    /// Default: builds a <see cref="JsonStreamRowSource"/> when the format is JSON, reading
    /// <c>RecordSelector</c>/<c>FlattenNestedObjects</c>/<c>FlattenSeparator</c> from the container
    /// metadata. The net10 <c>RestProtocolBase</c> override resolves any registered format via the
    /// <c>RecordSourceTypes</c> TypeCollection so the dispatch is fully format-driven.
    /// </remarks>
    protected virtual IRowSourceReader? TryCreateRowReader(IStorageContainer container, Stream content)
    {
        if (container.Format is null
            || !string.Equals(container.Format.Name, "Json", StringComparison.Ordinal))
        {
            return null;
        }

        var meta = container.Metadata;
        var recordSelector = meta.TryGetValue("RecordSelector", out var selectorObj)
            ? selectorObj as string
            : null;
        var flattenNestedObjects = meta.TryGetValue("FlattenNestedObjects", out var flattenObj)
            && flattenObj is bool boolVal
            && boolVal;
        var flattenSeparator = meta.TryGetValue("FlattenSeparator", out var sepObj) && sepObj is string sep
            ? sep
            : ".";

        return new JsonStreamRowSource(content, new JsonRowSourceOptions
        {
            RowArrayPath = recordSelector,
            FlattenNestedObjects = flattenNestedObjects,
            FlattenSeparator = string.IsNullOrEmpty(flattenSeparator) ? "." : flattenSeparator
        });
    }

    /// <summary>
    /// Extracts rows as <see cref="List{T}"/> of <c>Dictionary&lt;string, object?&gt;</c>
    /// from response content using the row reader resolved for the container's format via
    /// <see cref="TryCreateRowReader"/>.
    /// </summary>
    /// <param name="content">Raw response body.</param>
    /// <param name="container">The storage container whose format + Metadata drive the reader.</param>
    /// <returns>
    /// A success result containing the extracted rows, or a failure with
    /// <c>ResponseRowExtractionFailed</c> if parsing fails or no reader is available for the format.
    /// </returns>
    protected IGenericResult<object?> ExtractRowsFromContent(
        string content,
        IStorageContainer container)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(bytes);

            using var src = TryCreateRowReader(container, stream);
            if (src is null)
            {
                return GenericResult<object?>.Failure(
                    HttpResultCodes.ByName("ResponseRowExtractionFailed"),
                    ResultDetails.Create().With("ErrorMessage",
                        $"No row source reader available for format '{container.Format?.Name}'."));
            }

            var rows = new List<Dictionary<string, object?>>();
            while (src.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < src.FieldCount; i++)
                    row[src.GetFieldName(i)] = src.GetValue(i);
                rows.Add(row);
            }

            return GenericResult<object?>.Success(rows);
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("ResponseRowExtractionFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <summary>
    /// Returns true when <paramref name="resultType"/> is a collection of string-keyed dictionaries
    /// (IEnumerable&lt;Dictionary&lt;string,object?&gt;&gt; or compatible).
    /// </summary>
    /// <remarks>
    /// Recognised generic definitions: IEnumerable&lt;T&gt;, List&lt;T&gt;, ICollection&lt;T&gt;,
    /// IList&lt;T&gt;, IReadOnlyList&lt;T&gt;. Element T must be Dictionary&lt;string,?&gt; or
    /// IDictionary&lt;string,?&gt;.
    /// </remarks>
    protected static bool IsRowCollectionType(Type resultType)
    {
        if (!resultType.IsGenericType)
            return false;

        var genericDef = resultType.GetGenericTypeDefinition();
        var isEnumerableVariant =
            genericDef == typeof(IEnumerable<>) ||
            genericDef == typeof(List<>) ||
            genericDef == typeof(ICollection<>) ||
            genericDef == typeof(IReadOnlyList<>) ||
            genericDef == typeof(IList<>);

        if (!isEnumerableVariant)
            return false;

        var elementType = resultType.GetGenericArguments()[0];
        return IsDictionaryRowType(elementType) || IsDynamicRowType(elementType);
    }

    private static bool IsDictionaryRowType(Type t)
    {
        if (!t.IsGenericType)
            return false;

        var def = t.GetGenericTypeDefinition();
        if (def != typeof(Dictionary<,>) && def != typeof(IDictionary<,>))
            return false;

        return t.GetGenericArguments()[0] == typeof(string);
    }

    private static bool IsDynamicRowType(Type t)
        => t.Equals(typeof(object))
        || t.Equals(typeof(global::Fdw.Data.DataContainers.Abstractions.IDataRow))
        || t.Equals(typeof(global::Fdw.Data.DataContainers.Abstractions.DataRow));

    #endregion
}
