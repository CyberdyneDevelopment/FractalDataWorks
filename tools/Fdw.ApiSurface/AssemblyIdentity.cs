using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>
/// Writes the identity of one assembly: its own name and version (DEF), and for every assembly it references the version it asks for (REF).
/// bindcheck.py compares the references of packages that are not rebuilt with the definitions of the ones that are.
/// </summary>
internal static class AssemblyIdentity
{
    public static void Write(MetadataReader reader, string framework, SurfaceLines lines)
    {
        AssemblyDefinition definition = reader.GetAssemblyDefinition();
        string name = reader.GetString(definition.Name);
        lines.Add(framework, "DEF", name, definition.Version.ToString());
        foreach (AssemblyReferenceHandle handle in reader.AssemblyReferences)
        {
            AssemblyReference reference = reader.GetAssemblyReference(handle);
            lines.Add(framework, "REF", name + "->" + reader.GetString(reference.Name), reference.Version.ToString());
        }
    }
}
