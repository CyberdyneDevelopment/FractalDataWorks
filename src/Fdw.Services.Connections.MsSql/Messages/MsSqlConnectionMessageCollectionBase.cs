using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.MsSql.Messages;

/// <summary>
/// Collection base for MS SQL connection messages.
/// Generates static factory methods in MsSqlConnectionMessages class.
/// </summary>
[MessageCollection("MsSqlConnectionMessages")]
public abstract class MsSqlConnectionMessageCollectionBase : MessageCollectionBase<MsSqlConnectionMessage>
{
}
