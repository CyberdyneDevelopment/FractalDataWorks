using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Streaming JSON row source that reads array elements without loading entire document.
/// </summary>
/// <remarks>
/// Uses System.Text.Json Utf8JsonReader for efficient streaming.
/// Supports reading arrays at any nesting level via RowArrayPath option.
/// </remarks>
public sealed class JsonStreamRowSource : IRowSourceReader, IAsyncRowSourceReader
{
    private readonly Stream _stream;
    private readonly JsonRowSourceOptions _options;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, object?> _currentRowValues;
    private JsonDocument? _document;
    private JsonElement.ArrayEnumerator _arrayEnumerator;
    private bool _hasCurrentRow;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonStreamRowSource"/> class.
    /// </summary>
    /// <param name="stream">The input stream containing JSON data.</param>
    /// <param name="options">JSON processing options.</param>
    public JsonStreamRowSource(Stream stream, JsonRowSourceOptions? options = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _options = options ?? new JsonRowSourceOptions();
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _fieldNames = [];
        _currentRowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool HasCurrentRow => _hasCurrentRow;

    /// <inheritdoc />
    public int FieldCount => _fieldNames.Count;

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
    public bool Read()
    {
        if (!_initialized)
        {
            Initialize();
        }

        return ReadNextRow();
    }

    /// <inheritdoc />
    public ValueTask<bool> Read(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<bool>(Read());
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only, no reset supported
    }

    private void Initialize()
    {
        var docOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = _options.AllowTrailingCommas,
            CommentHandling = _options.AllowComments
                ? JsonCommentHandling.Skip
                : JsonCommentHandling.Disallow,
            MaxDepth = _options.MaxDepth
        };

        _document = JsonDocument.Parse(_stream, docOptions);

        var root = _document.RootElement;

        // Navigate to array using path if specified
        if (!string.IsNullOrEmpty(_options.RowArrayPath))
        {
            root = NavigateToPath(root, _options.RowArrayPath!);
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            _arrayEnumerator = default;
        }
        else
        {
            _arrayEnumerator = root.EnumerateArray();
        }

        _initialized = true;
    }

    private static JsonElement NavigateToPath(JsonElement element, string path)
    {
        // Remove optional $ prefix
        var cleanPath = path.StartsWith("$.", StringComparison.Ordinal)
            ? path.Substring(2)
            : path.StartsWith("$", StringComparison.Ordinal)
                ? path.Substring(1)
                : path;

        if (string.IsNullOrEmpty(cleanPath))
        {
            return element;
        }

        var segments = cleanPath.Split('.');

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(segment, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out var index)
                    || index >= element.GetArrayLength())
                {
                    return default;
                }

                element = element[index];
                continue;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            if (!element.TryGetProperty(segment, out var child))
            {
                return default;
            }

            element = child;
        }

        return element;
    }

    private bool ReadNextRow()
    {
        _currentRowValues.Clear();
        _hasCurrentRow = false;

        if (!_arrayEnumerator.MoveNext())
        {
            return false;
        }

        var rowElement = _arrayEnumerator.Current;

        if (rowElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        ReadObjectProperties(rowElement, string.Empty);
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
        _document?.Dispose();
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
