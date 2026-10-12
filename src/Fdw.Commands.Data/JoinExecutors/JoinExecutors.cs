using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Collection of join executor strategies.
/// </summary>
/// <remarks>
/// <para>
/// Source generator discovers all types marked with [TypeOption(typeof(JoinExecutors), ...)].
/// Each executor implements a specific join algorithm (Inner, Left, Right, Full, Cross).
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Lookup by name (matches JoinType.Name)
/// var executor = JoinExecutors.ByName("Inner");
///
/// // Access via static properties
/// var inner = JoinExecutors.Inner;
/// var left = JoinExecutors.Left;
///
/// // Get all executors
/// var all = JoinExecutors.All();
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(JoinExecutorBase), typeof(IJoinExecutor), typeof(JoinExecutors))]
[ExcludeFromCodeCoverage]
public abstract partial class JoinExecutors : TypeCollectionBase<JoinExecutorBase, IJoinExecutor>
{
}
