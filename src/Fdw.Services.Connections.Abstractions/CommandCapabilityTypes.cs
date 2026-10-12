using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// TypeCollection for connection command capability types.
/// Source generator creates a static property and O(1) lookup for each
/// <c>[TypeOption(typeof(CommandCapabilityTypes), …)]</c> registered in any loaded assembly.
/// </summary>
/// <remarks>
/// Use <c>CommandCapabilityTypes.ByName("RawQuery")</c> for O(1) lookup.
/// Use <c>CommandCapabilityTypes.All()</c> for enumeration.
/// Connection types override <c>SupportedCommands</c> by calling <c>CommandCapabilityTypes.ByName(…)</c>
/// rather than instantiating capability objects directly — the TypeCollection owns the singletons.
/// </remarks>
[TypeCollection(typeof(CommandCapabilityTypeBase), typeof(ICommandCapabilityType), typeof(CommandCapabilityTypes))]
public sealed partial class CommandCapabilityTypes : TypeCollectionBase<CommandCapabilityTypeBase, ICommandCapabilityType>
{
}
