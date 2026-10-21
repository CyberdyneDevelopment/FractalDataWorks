using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// What a guide query or audit is about: a project, a type, or a type inside a project.
/// </summary>
internal sealed class GuideTarget
{
    private readonly IReadOnlyDictionary<string, INamedTypeSymbol>? _reach;
    private readonly Microsoft.CodeAnalysis.Compilation? _compilation;

    private GuideTarget(
        string label,
        Project? project,
        INamedTypeSymbol? type,
        IReadOnlyDictionary<string, INamedTypeSymbol>? reach,
        Microsoft.CodeAnalysis.Compilation? compilation)
    {
        Label = label;
        Project = project;
        Type = type;
        _reach = reach;
        _compilation = compilation;
    }

    /// <summary>Gets the text naming the target in results and logs.</summary>
    public string Label { get; }

    /// <summary>Gets the project the target was resolved in; null for a type found outside any one project.</summary>
    public Project? Project { get; }

    /// <summary>Gets the type the target names; null for a project target.</summary>
    public INamedTypeSymbol? Type { get; }

    /// <summary>
    /// Creates a target for a project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="compilation">The project's compilation.</param>
    /// <returns>The target.</returns>
    public static GuideTarget ForProject(Project project, Microsoft.CodeAnalysis.Compilation compilation) =>
        new($"project '{project.Name}'", project, null, null, compilation);

    /// <summary>
    /// Creates a target for a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="project">The project it was found in, when the search was scoped to one.</param>
    /// <returns>The target, reaching the type itself, every base type and every interface it implements.</returns>
    /// <remarks>
    /// The reached types keep their constructed form. Interface-member lookup on a closed generic interface
    /// finds nothing for the open definition's members, which would report every member as unimplemented.
    /// </remarks>
    public static GuideTarget ForType(INamedTypeSymbol type, Project? project)
    {
        var reach = new Dictionary<string, INamedTypeSymbol>(System.StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType)
            reach.TryAdd(GuideTypeResolver.FullName(current), current);
        foreach (var contract in type.AllInterfaces)
            reach.TryAdd(GuideTypeResolver.FullName(contract), contract);

        return new GuideTarget($"type '{GuideTypeResolver.FullName(type)}'", project, type, reach, null);
    }

    /// <summary>
    /// Finds the abstractions of a guide that the target reaches.
    /// </summary>
    /// <param name="guide">The guide.</param>
    /// <returns>The resolved types the guide names that the target implements, derives from or references.</returns>
    public IReadOnlyList<INamedTypeSymbol> Matches(GuideBodyModel guide) =>
        guide.Implements
            .Select(i => Resolve(i.Type))
            .OfType<INamedTypeSymbol>()
            .ToList();

    private INamedTypeSymbol? Resolve(string fullName)
    {
        if (_reach is not null)
            return _reach.TryGetValue(fullName, out var symbol) ? symbol : null;

        return _compilation is null ? null : GuideTypeResolver.Resolve(_compilation, fullName);
    }
}
