namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a [TypeLookup] property with its extracted value.
/// </summary>
internal readonly record struct LookupPropertyModel(
    string PropertyName,
    string PropertyType,
    string MethodName,
    string? ExtractedValue // The literal value extracted from constructor, null if not extractable
);