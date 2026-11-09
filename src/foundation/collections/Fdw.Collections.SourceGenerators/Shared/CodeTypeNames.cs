using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Names a type the way generated code must write it (FDW-4171): fully qualified from <c>global::</c>, with the nullable
/// modifier kept and special types written as keywords (<c>string</c>, <c>int?</c>). Without <c>global::</c>, a carried type
/// such as <c>Serilog.Events.LogEventLevel</c>, named inside a collection whose namespace also begins with <c>Serilog</c>,
/// resolves to that namespace and fails (CS0234).
/// </summary>
/// <remarks>
/// Only for a type written into generated source. A string used as an identity (an Id hash, a signature, a dictionary
/// key or a comparison against a known name) keeps <c>ToDisplayString()</c>, because changing it would change generated
/// Ids or break the match.
/// </remarks>
internal static class CodeTypeNames
{
    private static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>The type as generated code writes it.</summary>
    /// <param name="type">The type.</param>
    /// <returns>Its fully qualified name, for example <c>global::Serilog.Events.LogEventLevel</c>.</returns>
    public static string Of(ITypeSymbol type) => type.ToDisplayString(Format);
}
