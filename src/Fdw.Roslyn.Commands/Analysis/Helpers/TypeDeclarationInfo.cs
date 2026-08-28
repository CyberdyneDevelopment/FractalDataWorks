using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Helpers;

/// <summary>
/// A top-level type declaration read syntactically from a document.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TypeDeclarationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeDeclarationInfo"/> class.
    /// </summary>
    /// <param name="namespaceName">The declared namespace.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="isTypeOption">Whether the type carries a TypeOption attribute.</param>
    public TypeDeclarationInfo(string namespaceName, string typeName, bool isTypeOption)
    {
        Namespace = namespaceName;
        TypeName = typeName;
        IsTypeOption = isTypeOption;
    }

    /// <summary>Gets the declared namespace.</summary>
    public string Namespace { get; }

    /// <summary>Gets the type name.</summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets a value indicating whether the type carries <c>[TypeOption]</c> or <c>[ServiceTypeOption]</c>.
    /// </summary>
    public bool IsTypeOption { get; }
}
