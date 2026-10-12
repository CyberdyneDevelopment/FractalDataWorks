using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// TypeCollection for result status levels.
/// </summary>
[TypeCollection(typeof(ResultStatusBase), typeof(IResultStatus), typeof(ResultStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class ResultStatuses : TypeCollectionBase<ResultStatusBase, IResultStatus>
{
}
