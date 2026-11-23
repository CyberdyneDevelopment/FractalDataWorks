using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>One path, or one container of it, that a write of a data store adds, changes or removes (FDW-6968).</summary>
/// <param name="Path">The path: as written when the write keeps it, as stored when the write removes it.</param>
/// <param name="Container">The container; null when the change is to the path itself.</param>
public sealed record DataStoreItemChange(DataPathConfiguration Path, DataContainerConfiguration? Container);
