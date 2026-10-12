using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>
/// TypeCollection for framework message severity levels.
/// </summary>
[TypeCollection(typeof(MessageSeverityBase), typeof(IMessageSeverity), typeof(MessageSeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class MessageSeverities : TypeCollectionBase<MessageSeverityBase, IMessageSeverity> { }
