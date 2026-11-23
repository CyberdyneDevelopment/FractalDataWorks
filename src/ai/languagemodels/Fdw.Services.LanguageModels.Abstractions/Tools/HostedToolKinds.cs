using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of the kinds of tool a provider runs itself. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(HostedToolKindBase), typeof(IHostedToolKind), typeof(HostedToolKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class HostedToolKinds : TypeCollectionBase<HostedToolKindBase, IHostedToolKind>
{
}
