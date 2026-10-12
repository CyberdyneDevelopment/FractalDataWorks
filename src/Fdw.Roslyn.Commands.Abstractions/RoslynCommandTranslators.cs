using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Development.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Type collection for Roslyn command translators.
/// Child collection of <see cref="DevelopmentCommandTranslators"/> for C# specific translators.
/// Discovers all translators marked with [TypeOption(typeof(RoslynCommandTranslators), "TranslatorName", RestrictToCurrentCompilation = true)].
/// </summary>
[TypeCollection(typeof(RoslynCommandTranslatorBase), typeof(IRoslynCommandTranslator), typeof(RoslynCommandTranslators),
    TypeOption = typeof(DevelopmentCommandTranslators), TypeOptionName = "Roslyn")]
public abstract partial class RoslynCommandTranslators
    : TypeCollectionBase<RoslynCommandTranslatorBase, IRoslynCommandTranslator>
{
}
