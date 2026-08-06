using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode;

/// <summary>
/// Collection of write mode types.
/// </summary>
[TypeCollection(typeof(WriteModeBase), typeof(IWriteMode), typeof(WriteModes))]
public abstract partial class WriteModes : TypeCollectionBase<WriteModeBase, IWriteMode>
{
}
