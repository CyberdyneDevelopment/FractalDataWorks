using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// TypeCollection for data write modes.
/// </summary>
[TypeCollection(typeof(WriteModeBase), typeof(IWriteMode), typeof(WriteModes))]
[ExcludeFromCodeCoverage]
public abstract partial class WriteModes : TypeCollectionBase<WriteModeBase, IWriteMode> { }
