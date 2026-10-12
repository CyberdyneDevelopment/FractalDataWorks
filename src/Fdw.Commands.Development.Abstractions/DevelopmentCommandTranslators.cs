using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Type collection for development command translators.
/// </summary>
[TypeCollection(typeof(DevelopmentCommandTranslatorBase), typeof(IDevelopmentCommandTranslator), typeof(DevelopmentCommandTranslators))]
public abstract partial class DevelopmentCommandTranslators
    : TypeCollectionBase<DevelopmentCommandTranslatorBase, IDevelopmentCommandTranslator>
{
}
