using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Collection of all process states.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ProcessStateBase), typeof(IProcessState), typeof(ProcessStates))]
public partial class ProcessStates : TypeCollectionBase<ProcessStateBase, IProcessState>
{

}