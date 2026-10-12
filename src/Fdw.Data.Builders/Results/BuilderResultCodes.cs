using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// TypeCollection for Data.Builders result codes.
/// EventId range: 6250-6299
/// </summary>
[TypeCollection(typeof(BuilderResultCodeBase), typeof(IBuilderResultCode), typeof(BuilderResultCodes))]
public abstract partial class BuilderResultCodes : TypeCollectionBase<BuilderResultCodeBase, IBuilderResultCode> { }
