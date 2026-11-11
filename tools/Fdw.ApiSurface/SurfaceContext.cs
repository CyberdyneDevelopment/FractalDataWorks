using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>What every record writer needs: the assembly being read, how to print its types, where the records go, and the framework they belong to.</summary>
internal sealed record SurfaceContext(MetadataReader Reader, SignatureTextProvider Provider, SurfaceLines Lines, string Framework);
