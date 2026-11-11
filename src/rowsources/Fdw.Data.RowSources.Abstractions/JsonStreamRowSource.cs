using Fdw.Conventions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Streaming JSON row source: reads records one at a time as the stream is read, never the whole document (FDW-4971).
/// </summary>
/// <remarks>
/// The records are the objects of the top-level array, of the array at <see cref="JsonRowSourceOptions.RowArrayPath"/>, or, when
/// <see cref="JsonRowSourceOptions.RecordsAreLines"/> is set, each top-level value (JSON Lines). They are found by
/// <see cref="JsonRecordScanner"/> over <see cref="Utf8JsonReader"/>; each record is parsed alone and flattened into the row exactly as before.
/// A malformed document ends the read with <see cref="ReadFailure"/> set, after every record before it. The stream is not owned:
/// whoever opened it and asked the format for this reader disposes it.
/// </remarks>
public sealed class JsonStreamRowSource : IRowSourceReader, IAsyncRowSourceReader, IReportsReadFailure
{
    [SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Not owned: the content stream; whoever opened it and asked the format for this reader (StreamedFileRecordSource for a file) disposes it.")]
    private readonly Stream _stream;
    private readonly JsonRowSourceOptions _options;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, object?> _currentRowValues;
    private JsonRecordScanner? _scanner;
    private bool _hasCurrentRow;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonStreamRowSource"/> class.
    /// </summary>
    /// <param name="stream">The input stream containing JSON data.</param>
    /// <param name="options">JSON processing options.</param>
    public JsonStreamRowSource(Stream stream, JsonRowSourceOptions options)
    {
        _stream = stream;
        _options = options;
        if (options.BufferSize < 1)
            throw new ArgumentOutOfRangeException(nameof(options), options.BufferSize, "A JSON reader's buffer holds at least one byte.");
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _fieldNames = [];
        _currentRowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool HasCurrentRow => _hasCurrentRow;

    /// <inheritdoc />
    public int FieldCount => _fieldNames.Count;

    /// <inheritdoc />
    public Fdw.Messages.IGenericMessage? ReadFailure => _scanner?.Failure;

    /// <inheritdoc />
    public bool CanReset => false;

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 1;

    /// <inheritdoc />
    public string GetFieldName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return string.Empty;
        }

        return _fieldNames[ordinal];
    }

    /// <inheritdoc />
    public int GetFieldOrdinal(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return -1;
        }

        return _fieldOrdinals.TryGetValue(fieldName, out var ordinal) ? ordinal : -1;
    }

    /// <inheritdoc />
    public bool IsNull(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return true;
        }

        var fieldName = _fieldNames[ordinal];
        return !_currentRowValues.TryGetValue(fieldName, out var value) || value == null;
    }

    /// <inheritdoc />
    public object? GetValue(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return null;
        }

        var fieldName = _fieldNames[ordinal];
        return _currentRowValues.TryGetValue(fieldName, out var value) ? value : null;
    }

    /// <inheritdoc />
    public object? GetConvertedValue(int ordinal, IDataTypeConverter converter)
    {
        var rawValue = GetValue(ordinal);
        if (rawValue == null)
        {
            return null;
        }

        return converter.ToClr(rawValue);
    }

    /// <inheritdoc />
    public bool Read() => ReadRow();

    /// <inheritdoc />
    public ValueTask<bool> Read(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<bool>(ReadRow());
    }

    /// <summary>
    /// Reads the next row, initialising the reader on first use.
    /// </summary>
    /// <remarks>
    /// Why a private method both Read overloads call: the asynchronous overload calling the synchronous Read()
    /// tripped MA0040, and handing it the token instead would have called itself (FDW-3578).
    /// </remarks>
    private bool ReadRow()
    {
        if (!_initialized)
        {
            Initialize();
        }

        return ReadNextRow();
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only, no reset supported
    }

    private void Initialize()
    {
        _scanner = new JsonRecordScanner(
            _stream,
            _options.RowArrayPath,
            _options.RecordsAreLines,
            _options.BufferSize,
            new JsonReaderOptions
            {
                AllowTrailingCommas = _options.AllowTrailingCommas,
                CommentHandling = _options.AllowComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
                MaxDepth = _options.MaxDepth,
            });
        _initialized = true;
    }

    private bool ReadNextRow()
    {
        _currentRowValues.Clear();
        _hasCurrentRow = false;

        if (_scanner is null || !_scanner.TryNext(out var record))
        {
            return false;
        }

        // Why each record is parsed alone: only its bytes are held, and the row it becomes is flattened exactly as a whole document's was.
        using var document = JsonDocument.Parse(record, new JsonDocumentOptions
        {
            AllowTrailingCommas = _options.AllowTrailingCommas,
            CommentHandling = _options.AllowComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
            MaxDepth = _options.MaxDepth,
        });
        ReadObjectProperties(document.RootElement, string.Empty);
        _hasCurrentRow = true;
        return true;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // JSON parsing — type-based property extraction with nested object/array flattening
    private void ReadObjectProperties(JsonElement element, string prefix)
    {
        foreach (var property in element.EnumerateObject())
        {
            var fieldName = string.IsNullOrEmpty(prefix)
                ? property.Name
                : $"{prefix}{_options.FlattenSeparator}{property.Name}";

#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object when _options.FlattenNestedObjects:
                    ReadObjectProperties(property.Value, fieldName);
                    break;

                case JsonValueKind.Array when _options.FlattenNestedObjects:
                    FlattenArrayByIndex(property.Value, fieldName);
                    break;

                case JsonValueKind.Null:
                    AddFieldValue(fieldName, null);
                    break;

                case JsonValueKind.String:
                    AddFieldValue(fieldName, property.Value.GetString());
                    break;

                case JsonValueKind.Number:
                    if (property.Value.TryGetInt64(out var longValue))
                    {
                        AddFieldValue(fieldName, longValue);
                    }
                    else if (property.Value.TryGetDouble(out var doubleValue))
                    {
                        AddFieldValue(fieldName, doubleValue);
                    }
                    else
                    {
                        AddFieldValue(fieldName, property.Value.GetRawText());
                    }

                    break;

                case JsonValueKind.True:
                    AddFieldValue(fieldName, true);
                    break;

                case JsonValueKind.False:
                    AddFieldValue(fieldName, false);
                    break;

                case JsonValueKind.Array:
                    // FlattenNestedObjects is false: store array as raw JSON string
                    AddFieldValue(fieldName, property.Value.GetRawText());
                    break;

                case JsonValueKind.Object:
                    // Store objects as JSON strings if not flattening
                    AddFieldValue(fieldName, property.Value.GetRawText());
                    break;

                default:
                    AddFieldValue(fieldName, property.Value.GetRawText());
                    break;
            }
#pragma warning restore FDW018
        }
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // JSON parsing — recursive scalar extraction per array element
    private void FlattenArrayByIndex(JsonElement array, string fieldName)
    {
        var index = 0;
        foreach (var element in array.EnumerateArray())
        {
            var indexedName = $"{fieldName}{_options.FlattenSeparator}{index}";

#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    ReadObjectProperties(element, indexedName);
                    break;

                case JsonValueKind.Array:
                    FlattenArrayByIndex(element, indexedName);
                    break;

                case JsonValueKind.Null:
                    AddFieldValue(indexedName, null);
                    break;

                case JsonValueKind.String:
                    AddFieldValue(indexedName, element.GetString());
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                    {
                        AddFieldValue(indexedName, longValue);
                    }
                    else if (element.TryGetDouble(out var doubleValue))
                    {
                        AddFieldValue(indexedName, doubleValue);
                    }
                    else
                    {
                        AddFieldValue(indexedName, element.GetRawText());
                    }

                    break;

                case JsonValueKind.True:
                    AddFieldValue(indexedName, true);
                    break;

                case JsonValueKind.False:
                    AddFieldValue(indexedName, false);
                    break;

                default:
                    AddFieldValue(indexedName, element.GetRawText());
                    break;
            }
#pragma warning restore FDW018

            index++;
        }
    }

    private void AddFieldValue(string name, object? value)
    {
        if (!_fieldOrdinals.ContainsKey(name))
        {
            _fieldOrdinals[name] = _fieldNames.Count;
            _fieldNames.Add(name);
        }

        _currentRowValues[name] = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _currentRowValues.Clear();
        _fieldOrdinals.Clear();
        _fieldNames.Clear();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
