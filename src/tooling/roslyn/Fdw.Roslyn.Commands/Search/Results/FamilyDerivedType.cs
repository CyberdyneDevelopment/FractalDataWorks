using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// A type (interface or abstract class) that derives from a family root.
/// </summary>
public sealed class FamilyDerivedType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyDerivedType"/> class.
    /// </summary>
    public FamilyDerivedType(
        string name,
        string fullName,
        string namespaceName,
        string kind,
        bool isAbstract,
        int extraMemberCount,
        IReadOnlyList<string> extraMemberNames,
        string filePath,
        int line)
    {
        Name = name;
        FullName = fullName;
        Namespace = namespaceName;
        Kind = kind;
        IsAbstract = isAbstract;
        ExtraMemberCount = extraMemberCount;
        ExtraMemberNames = extraMemberNames;
        FilePath = filePath;
        Line = line;
    }

    /// <summary>Gets the derived type's simple name.</summary>
    public string Name { get; }

    /// <summary>Gets the fully qualified name.</summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the declaring namespace.
    /// </summary>
    /// <remarks>
    /// Reported for symmetry with FamilyImplementation: an audit that can see where the concrete
    /// implementations live but not the intermediate abstractions has a hole in exactly the place a
    /// package split needs to look.
    /// </remarks>
    public string Namespace { get; }

    /// <summary>Gets the kind (Interface / Class).</summary>
    public string Kind { get; }

    /// <summary>Gets a value indicating whether the derived type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the count of public members added beyond the root contract.</summary>
    public int ExtraMemberCount { get; }

    /// <summary>Gets the names of public members added beyond the root contract.</summary>
    public IReadOnlyList<string> ExtraMemberNames { get; }

    /// <summary>Gets the file path of the declaration.</summary>
    public string FilePath { get; }

    /// <summary>Gets the line number of the declaration.</summary>
    public int Line { get; }
}
