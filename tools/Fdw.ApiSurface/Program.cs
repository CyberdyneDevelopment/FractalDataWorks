using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Text;

namespace Fdw.ApiSurface;

/// <summary>
/// Writes the public surface of a package (or one assembly) as the sorted records apisurface.py compares:
/// <c>Fdw.ApiSurface --package X.nupkg --out surface.txt</c> or <c>Fdw.ApiSurface --assembly X.dll --tfm net10.0 --out surface.txt</c>.
/// With <c>--identity</c> it writes the assembly names and versions of the package's runtime assemblies instead (see AssemblyIdentity).
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            SurfaceOptions options = SurfaceOptions.Parse(args);
            SurfaceLines lines = new();
            foreach (AssemblyImage image in Images(options))
            {
                Read(image, lines, options.Identity);
            }

            File.WriteAllText(options.Output, string.Join('\n', lines.Sorted()) + "\n", new UTF8Encoding(false));
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or BadImageFormatException)
        {
            Console.Error.WriteLine("FAIL: " + exception.Message);
            return 1;
        }
    }

    private static IReadOnlyList<AssemblyImage> Images(SurfaceOptions options)
    {
        if (options.Package is not null)
        {
            return PackageAssemblies.Read(options.Package, options.Identity);
        }

        string path = options.Assembly ?? throw new InvalidOperationException("No assembly was given.");
        string framework = options.Framework ?? throw new InvalidOperationException("No target framework was given.");
        return [new AssemblyImage(framework, Path.GetFileName(path), File.ReadAllBytes(path))];
    }

    private static void Read(AssemblyImage image, SurfaceLines lines, bool identity)
    {
        using PEReader pe = new(ImmutableArray.Create(image.Bytes));
        if (!pe.HasMetadata)
        {
            throw new InvalidOperationException($"{image.Name} ({image.Framework}) has no .NET metadata.");
        }

        if (identity)
        {
            AssemblyIdentity.Write(pe.GetMetadataReader(), image.Framework, lines);
            return;
        }

        AssemblySurface.Write(pe.GetMetadataReader(), image.Framework, lines);
    }
}
