using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A container inside a data store.</summary>
[TypeOption(typeof(MapKindOptions), "Container")]
[ExcludeFromCodeCoverage]
public sealed class ContainerMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerMapKind"/> class.</summary>
    public ContainerMapKind()
        : base("Container")
    {
    }
}
