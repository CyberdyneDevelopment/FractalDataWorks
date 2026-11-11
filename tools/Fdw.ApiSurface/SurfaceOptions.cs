using System;

namespace Fdw.ApiSurface;

/// <summary>What was asked: a package, or one assembly with its framework, the file to write, and whether to write the surface (the default) or, with --identity, the assembly names and versions. Nothing is assumed; a missing part stops the run.</summary>
internal sealed record SurfaceOptions(string? Package, string? Assembly, string? Framework, string Output, bool Identity)
{
    public static SurfaceOptions Parse(string[] args)
    {
        string? package = Value(args, "--package");
        string? assembly = Value(args, "--assembly");
        string? framework = Value(args, "--tfm");
        string output = Value(args, "--out") ?? throw new ArgumentException("--out <file> is required.", nameof(args));
        if ((package is null) == (assembly is null))
        {
            throw new ArgumentException("Give exactly one of --package <nupkg> and --assembly <dll> --tfm <framework>.", nameof(args));
        }

        if (assembly is not null && framework is null)
        {
            throw new ArgumentException("--assembly needs --tfm <framework>: the surface is recorded per target framework.", nameof(args));
        }

        bool identity = Array.IndexOf(args, "--identity") >= 0;
        return new SurfaceOptions(package, assembly, framework, output, identity);
    }

    private static string? Value(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        if (index < 0)
        {
            return null;
        }

        return index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"{name} needs a value.", nameof(args));
    }
}
