using System;
using System.IO;
using Fdw.Collections;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Base class for Roslyn commands.
/// Commands are stateless data objects that describe an operation to perform.
/// </summary>
public abstract class RoslynCommandBase : TypeOptionBase<int, RoslynCommandBase>, IRoslynCommand
{
    /// <summary>
    /// Gets the command category.
    /// </summary>
    public IRoslynCommandCategory? CommandCategory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandBase"/> class.
    /// Used by TypeCollection for Empty sentinel.
    /// </summary>
    protected RoslynCommandBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, "RoslynCommand")
    {
        CommandCategory = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandBase"/> class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="commandCategory">The category of the command.</param>
    /// <param name="description">The description of the command.</param>
    protected RoslynCommandBase(string name, IRoslynCommandCategory commandCategory, string description)
        : base(GenerateIdFromName(name), name, name, name, description, "RoslynCommand")
    {
        CommandCategory = commandCategory ?? throw new ArgumentNullException(nameof(commandCategory));
    }

    /// <summary>
    /// Gets the file-name suffixes that mark a document as compiler- or build-generated.
    /// </summary>
    private static readonly string[] GeneratedFileSuffixes =
    {
        ".g.cs", ".g.i.cs", ".generated.cs",
    };

    /// <summary>
    /// Determines whether a document is compiler- or build-generated and should be skipped.
    /// </summary>
    /// <param name="document">The document to test.</param>
    /// <returns><see langword="true"/> when the command should ignore it.</returns>
    /// <remarks>
    /// Lives on the command rather than in a helper because it is POLICY, not a utility: skipping
    /// generated code is part of what every one of these commands means, so a translator cannot forget
    /// to apply it, and a command that genuinely wants to see generated code can override it.
    ///
    /// MSBuild adds generated sources to the compilation as ordinary documents — obj/…/AssemblyInfo.cs,
    /// .NETStandard,Version=v2.0.AssemblyAttributes.cs, EmbeddedAttribute.cs, source-generator output —
    /// so Roslyn hands them over like any other file. Reporting on them is noise; REWRITING one is worse,
    /// because the next build regenerates the file and silently discards the change.
    /// </remarks>
    public virtual bool IsGeneratedDocument(Document document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        return IsGeneratedPath(document.FilePath);
    }

    /// <summary>
    /// Determines whether a path denotes a compiler- or build-generated file.
    /// </summary>
    /// <param name="path">The file path, which may be null or empty.</param>
    /// <returns><see langword="true"/> when the path should be skipped.</returns>
    /// <remarks>
    /// Takes a path as well as a Document so a DIAGNOSTIC's location can be filtered by the same rule —
    /// otherwise a probe and a finder would disagree about what counts as generated.
    /// </remarks>
    public virtual bool IsGeneratedPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var normalised = path!.Replace('\\', '/');

        if (normalised.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) return true;
        if (normalised.Contains("/bin/", StringComparison.OrdinalIgnoreCase)) return true;
        if (normalised.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)) return true;
        if (normalised.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)) return true;

        var fileName = Path.GetFileName(normalised);
        foreach (var suffix in GeneratedFileSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>
    /// Generates a deterministic ID from a command name using FNV-1a hash.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <returns>A deterministic ID based on the name.</returns>
    private static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;

            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }
}
