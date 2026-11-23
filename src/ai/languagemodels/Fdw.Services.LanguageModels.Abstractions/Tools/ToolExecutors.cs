using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of who runs a tool a model may call. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(ToolExecutorBase), typeof(IToolExecutor), typeof(ToolExecutors))]
[ExcludeFromCodeCoverage]
public abstract partial class ToolExecutors : TypeCollectionBase<ToolExecutorBase, IToolExecutor>
{
}
