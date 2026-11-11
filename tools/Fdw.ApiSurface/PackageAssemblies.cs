using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Fdw.ApiSurface;

/// <summary>
/// Reads the assemblies a package offers to compile against: ref/&lt;framework&gt; where it has one, otherwise lib/&lt;framework&gt;.
/// With <c>runtimeOnly</c> it reads the lib/&lt;framework&gt; assemblies only, which are the ones the runtime loads.
/// </summary>
internal static class PackageAssemblies
{
    public static IReadOnlyList<AssemblyImage> Read(string packagePath, bool runtimeOnly)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        List<ZipArchiveEntry> entries = [.. archive.Entries.Where(e => e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))];
        HashSet<string> referenced = [.. entries.Where(e => string.Equals(Folder(e), "ref", StringComparison.Ordinal)).Select(Framework)];
        List<AssemblyImage> images = [];
        foreach (ZipArchiveEntry entry in entries)
        {
            string folder = Folder(entry);
            bool isLib = string.Equals(folder, "lib", StringComparison.Ordinal);
            bool wanted = runtimeOnly ? isLib : string.Equals(folder, "ref", StringComparison.Ordinal) || (isLib && !referenced.Contains(Framework(entry)));
            if (wanted)
            {
                images.Add(new AssemblyImage(Framework(entry), entry.Name, Bytes(entry)));
            }
        }

        return images;
    }

    private static string Folder(ZipArchiveEntry entry) => entry.FullName.Split('/')[0];

    private static string Framework(ZipArchiveEntry entry)
    {
        string[] parts = entry.FullName.Split('/');
        return parts.Length > 2 ? parts[1] : throw new InvalidOperationException($"{entry.FullName} is not under a target framework folder.");
    }

    private static byte[] Bytes(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
