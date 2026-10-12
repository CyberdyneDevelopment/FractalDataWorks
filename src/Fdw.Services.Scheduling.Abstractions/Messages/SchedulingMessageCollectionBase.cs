using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Collection definition to generate SchedulingMessages static class.
/// </summary>
[MessageCollection("SchedulingMessages", ReturnType = typeof(IServiceMessage))]
public abstract class SchedulingMessageCollectionBase : MessageCollectionBase<SchedulingMessage>
{

}