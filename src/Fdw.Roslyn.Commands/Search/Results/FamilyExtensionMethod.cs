namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// An extension method whose <c>this</c> parameter type belongs to the family.
/// </summary>
public sealed class FamilyExtensionMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyExtensionMethod"/> class.
    /// </summary>
    public FamilyExtensionMethod(
        string name,
        string fullName,
        string owningClass,
        string targetType,
        string signature,
        string filePath,
        int line)
    {
        Name = name;
        FullName = fullName;
        OwningClass = owningClass;
        TargetType = targetType;
        Signature = signature;
        FilePath = filePath;
        Line = line;
    }

    /// <summary>Gets the extension method's name.</summary>
    public string Name { get; }

    /// <summary>Gets the fully qualified method name.</summary>
    public string FullName { get; }

    /// <summary>Gets the static class that hosts the extension method.</summary>
    public string OwningClass { get; }

    /// <summary>Gets the family type this extension targets (the <c>this</c> parameter type).</summary>
    public string TargetType { get; }

    /// <summary>Gets the full method signature.</summary>
    public string Signature { get; }

    /// <summary>Gets the file path of the extension method declaration.</summary>
    public string FilePath { get; }

    /// <summary>Gets the line number of the declaration.</summary>
    public int Line { get; }
}
