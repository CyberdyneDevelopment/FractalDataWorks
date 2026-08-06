using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Global collection of all process types across all assemblies.
/// This uses the TypeCollection pattern to automatically discover
/// all ProcessTypeBase implementations at runtime.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ProcessTypeBase), typeof(IProcessType), typeof(ProcessTypes))]
public abstract partial class ProcessTypes
    : TypeCollectionBase<ProcessTypeBase, IProcessType>
{
}