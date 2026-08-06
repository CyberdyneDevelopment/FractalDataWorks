namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// A concrete implementation belonging to a family.
/// </summary>
public sealed class FamilyImplementation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyImplementation"/> class.
    /// </summary>
    public FamilyImplementation(
        string name,
        string fullName,
        string @namespace,
        bool isAbstract,
        int declaredPublicMemberCount,
        int extraBeyondContractCount,
        string filePath,
        int line)
    {
        Name = name;
        FullName = fullName;
        Namespace = @namespace;
        IsAbstract = isAbstract;
        DeclaredPublicMemberCount = declaredPublicMemberCount;
        ExtraBeyondContractCount = extraBeyondContractCount;
        FilePath = filePath;
        Line = line;
    }

    /// <summary>Gets the implementation's simple name.</summary>
    public string Name { get; }

    /// <summary>Gets the fully qualified name.</summary>
    public string FullName { get; }

    /// <summary>Gets the containing namespace.</summary>
    public string Namespace { get; }

    /// <summary>Gets a value indicating whether this type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the count of public members declared directly on this type.</summary>
    public int DeclaredPublicMemberCount { get; }

    /// <summary>Gets the count of public members not present on the root contract.</summary>
    public int ExtraBeyondContractCount { get; }

    /// <summary>Gets the file path.</summary>
    public string FilePath { get; }

    /// <summary>Gets the line number.</summary>
    public int Line { get; }
}
