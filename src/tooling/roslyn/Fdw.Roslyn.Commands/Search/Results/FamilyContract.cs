using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Canonical contract surface of a root family type (interface or abstract class).
/// </summary>
public sealed class FamilyContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyContract"/> class.
    /// </summary>
    public FamilyContract(
        string name,
        string fullName,
        string kind,
        bool isAbstract,
        bool isSealed,
        IReadOnlyList<string> genericParameters,
        IReadOnlyList<string> baseTypes,
        IReadOnlyList<string> implementedInterfaces,
        IReadOnlyList<FamilyContractMember> members,
        string filePath,
        int line)
    {
        Name = name;
        FullName = fullName;
        Kind = kind;
        IsAbstract = isAbstract;
        IsSealed = isSealed;
        GenericParameters = genericParameters;
        BaseTypes = baseTypes;
        ImplementedInterfaces = implementedInterfaces;
        Members = members;
        FilePath = filePath;
        Line = line;
    }

    /// <summary>Gets the type's simple name.</summary>
    public string Name { get; }

    /// <summary>Gets the fully qualified type name.</summary>
    public string FullName { get; }

    /// <summary>Gets the type kind (Interface / Class / Struct / Record).</summary>
    public string Kind { get; }

    /// <summary>Gets a value indicating whether the type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets a value indicating whether the type is sealed.</summary>
    public bool IsSealed { get; }

    /// <summary>Gets the generic parameter descriptors (name + constraints).</summary>
    public IReadOnlyList<string> GenericParameters { get; }

    /// <summary>Gets the base type chain (excluding object).</summary>
    public IReadOnlyList<string> BaseTypes { get; }

    /// <summary>Gets the directly implemented interface names.</summary>
    public IReadOnlyList<string> ImplementedInterfaces { get; }

    /// <summary>Gets the public members declared on this type.</summary>
    public IReadOnlyList<FamilyContractMember> Members { get; }

    /// <summary>Gets the file path of the primary declaration.</summary>
    public string FilePath { get; }

    /// <summary>Gets the line number of the primary declaration.</summary>
    public int Line { get; }
}
