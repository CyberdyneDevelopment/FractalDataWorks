using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// Another test implementation of <see cref="IInputInfoModel"/> with different properties.
/// </summary>
public class ComplexInputInfoModel : IInputInfoModel
{
    private readonly List<string> _tags = [];
    private readonly Dictionary<string, string> _properties = [];

    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the tags.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets the properties.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties => _properties;

    private string _cachedHash = string.Empty;

    /// <summary>
    /// Gets the input hash.
    /// </summary>
    public string InputHash
    {
        get
        {
            if (!string.IsNullOrEmpty(_cachedHash))
            {
                return _cachedHash;
            }

            using var writer = new StringWriter();
            WriteToHash(writer);
            _cachedHash = writer.ToString();

            return _cachedHash;
        }
    }

    /// <summary>
    /// Adds a tag.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(string tag)
    {
        _tags.Add(tag);
        _cachedHash = string.Empty; // Invalidate cache
    }

    /// <summary>
    /// Sets a property.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(string key, string value)
    {
        _properties[key] = value;
        _cachedHash = string.Empty; // Invalidate cache
    }

    /// <summary>
    /// Writes the model state to a TextWriter for hash calculation.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    public void WriteToHash(TextWriter writer)
    {
        writer.Write(Id);

        foreach (var tag in _tags)
        {
            writer.Write(tag);
        }

        foreach (var kvp in _properties.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }
    }
}