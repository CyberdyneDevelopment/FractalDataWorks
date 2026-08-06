using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// An external system (API, file system, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "ExternalSystem")]
public sealed class ExternalSystemNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalSystemNodeType"/> class.
    /// </summary>
    public ExternalSystemNodeType() : base(4, "ExternalSystem") { }
}
