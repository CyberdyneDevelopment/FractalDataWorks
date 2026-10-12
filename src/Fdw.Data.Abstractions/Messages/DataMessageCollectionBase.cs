using System.Diagnostics.CodeAnalysis;
using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Data.Abstractions.Messages;

/// <summary>
/// Collection definition to generate DataMessages static class.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageCollection("DataMessages", ReturnType = typeof(IDataMessage))]
public abstract class DataMessageCollectionBase : MessageCollectionBase<DataMessage>
{
}
