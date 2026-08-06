using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages;

/// <summary>
/// Collection definition to generate ExecutionMessages static class.
/// </summary>
[MessageCollection("ExecutionMessages", ReturnType = typeof(IServiceMessage))]
public abstract class ExecutionMessageCollectionBase : MessageCollectionBase<ExecutionMessage>
{
}