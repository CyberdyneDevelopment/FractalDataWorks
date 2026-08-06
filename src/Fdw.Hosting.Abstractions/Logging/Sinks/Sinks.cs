using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Collection of sink TypeOptions.
/// Provides type-safe access to logging sink types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SinkBase), typeof(ISink), typeof(Sinks))]
public sealed partial class Sinks : TypeCollectionBase<SinkBase, ISink>
{
}
