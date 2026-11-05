using Fdw.Conventions;

namespace Fdw.SourceGenerators.Models;

/// <summary>The key type a TypeCollection takes when its definition states none.</summary>
internal static class CollectionKeyTypes
{
    /// <summary>The TypeCollection convention's int key.</summary>
    [PlatformDefault]
    public const string Default = "int";
}
