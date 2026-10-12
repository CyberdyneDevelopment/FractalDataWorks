namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// One public member belonging to a family contract or implementation.
/// </summary>
public sealed class FamilyContractMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyContractMember"/> class.
    /// </summary>
    public FamilyContractMember(
        string name,
        string kind,
        string signature,
        string accessibility,
        bool isAbstract,
        bool isVirtual,
        bool isStatic,
        bool isOverride,
        string declaringType)
    {
        Name = name;
        Kind = kind;
        Signature = signature;
        Accessibility = accessibility;
        IsAbstract = isAbstract;
        IsVirtual = isVirtual;
        IsStatic = isStatic;
        IsOverride = isOverride;
        DeclaringType = declaringType;
    }

    /// <summary>Gets the member name.</summary>
    public string Name { get; }

    /// <summary>Gets the member kind (Method / Property / Event / Field / NamedType).</summary>
    public string Kind { get; }

    /// <summary>Gets the canonical Roslyn display signature.</summary>
    public string Signature { get; }

    /// <summary>Gets the member accessibility level.</summary>
    public string Accessibility { get; }

    /// <summary>Gets a value indicating whether the member is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets a value indicating whether the member is virtual.</summary>
    public bool IsVirtual { get; }

    /// <summary>Gets a value indicating whether the member is static.</summary>
    public bool IsStatic { get; }

    /// <summary>Gets a value indicating whether the member is an override.</summary>
    public bool IsOverride { get; }

    /// <summary>Gets the simple name of the type that declares this member.</summary>
    public string DeclaringType { get; }
}
