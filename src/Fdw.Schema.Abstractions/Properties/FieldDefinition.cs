#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Schema.Properties;

/// <summary>
/// Default implementation of <see cref="IFieldDefinition"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FieldDefinition : IFieldDefinition
{
    public required string Name { get; init; }
    public required IPropertyRole Role { get; init; }
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public required Type ClrType { get; init; }
    public string? SourceMapping { get; init; }
    public string? Calculator { get; init; }
    public string? Transformer { get; init; }
    public string? Format { get; init; }
}
