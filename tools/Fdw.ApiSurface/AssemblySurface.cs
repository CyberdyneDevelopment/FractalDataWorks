using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the public surface of one assembly, for one target framework, into the collected records.</summary>
internal static class AssemblySurface
{
    public static void Write(MetadataReader reader, string framework, SurfaceLines lines)
    {
        SurfaceContext context = new(reader, new SignatureTextProvider(), lines, framework);
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeRecords.Write(context, handle);
        }
    }
}
