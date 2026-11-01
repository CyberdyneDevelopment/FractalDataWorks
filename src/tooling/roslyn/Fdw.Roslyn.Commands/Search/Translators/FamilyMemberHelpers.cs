using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Roslyn.Commands.Search.Logging;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MsCompilation = Microsoft.CodeAnalysis.Compilation;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Helpers for enumerating, classifying, and keying public members during family analysis.
/// Every public method accepts an optional <see cref="ILogger"/>; when null,
/// <see cref="NullLogger.Instance"/> is used so log calls no-op without per-site checks.
/// </summary>
internal static class FamilyMemberHelpers
{
    /// <summary>
    /// Returns the symbols representing public members declared directly on
    /// <paramref name="type"/> (not inherited). Skips synthesized accessors,
    /// constructors, and implicitly declared symbols.
    /// </summary>
    public static IEnumerable<ISymbol> GetDeclaredPublicMembers(INamedTypeSymbol type, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        FamilyMemberHelpersLog.GetDeclaredPublicMembersStart(logger, type.ToDisplayString());

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "implicitly-declared");
                continue;
            }
            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "not-public");
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method:
                    if (method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                    {
                        FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "constructor");
                        continue;
                    }
                    if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
                    {
                        FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "property-accessor");
                        continue;
                    }
                    if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
                    {
                        FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "event-accessor");
                        continue;
                    }
                    if (method.AssociatedSymbol is not null)
                    {
                        FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "associated-symbol");
                        continue;
                    }
                    FamilyMemberHelpersLog.MemberIncluded(logger, type.Name, member.Name, "Method");
                    yield return method;
                    break;
                case IPropertySymbol:
                    FamilyMemberHelpersLog.MemberIncluded(logger, type.Name, member.Name, "Property");
                    yield return member;
                    break;
                case IEventSymbol:
                    FamilyMemberHelpersLog.MemberIncluded(logger, type.Name, member.Name, "Event");
                    yield return member;
                    break;
                case IFieldSymbol:
                    FamilyMemberHelpersLog.MemberIncluded(logger, type.Name, member.Name, "Field");
                    yield return member;
                    break;
                default:
                    FamilyMemberHelpersLog.MemberSkipped(logger, type.Name, member.Name, "unhandled-kind");
                    break;
            }
        }
    }

    /// <summary>
    /// Computes a stable signature key for a member used for cross-type equality.
    /// Methods: name + ordered parameter type signatures. Properties/events: name only.
    /// </summary>
    public static string GetMemberKey(ISymbol member, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        switch (member)
        {
            case IMethodSymbol method:
                var sb = new StringBuilder();
                sb.Append(method.Name);
                sb.Append('(');
                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(method.Parameters[i].Type.ToDisplayString());
                }
                sb.Append(')');
                var key = sb.ToString();
                FamilyMemberHelpersLog.GetMemberKeyMethod(logger, method.Name, method.Parameters.Length, key);
                return key;
            default:
                FamilyMemberHelpersLog.GetMemberKeyOther(logger, member.Name);
                return member.Name;
        }
    }

    /// <summary>
    /// Converts an ISymbol member into the public <see cref="FamilyContractMember"/>.
    /// </summary>
    public static FamilyContractMember ToContractMember(ISymbol member, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        bool isStatic = member.IsStatic;
        bool isAbstract = member is IMethodSymbol m1 && m1.IsAbstract
                       || member is IPropertySymbol p1 && p1.IsAbstract
                       || member is IEventSymbol e1 && e1.IsAbstract;
        bool isVirtual = member is IMethodSymbol m2 && m2.IsVirtual
                       || member is IPropertySymbol p2 && p2.IsVirtual
                       || member is IEventSymbol e2 && e2.IsVirtual;
        bool isOverride = member is IMethodSymbol m3 && m3.IsOverride
                       || member is IPropertySymbol p3 && p3.IsOverride
                       || member is IEventSymbol e3 && e3.IsOverride;

        FamilyMemberHelpersLog.ToContractMemberCalled(logger, member.Name, isAbstract, isVirtual, isStatic, isOverride);

        return new FamilyContractMember(
            member.Name,
            member.Kind.ToString(),
            member.ToDisplayString(),
            member.DeclaredAccessibility.ToString(),
            isAbstract,
            isVirtual,
            isStatic,
            isOverride,
            member.ContainingType?.Name ?? string.Empty);
    }

    /// <summary>
    /// Describes a generic type parameter (name + constraints).
    /// </summary>
    public static string DescribeTypeParameter(ITypeParameterSymbol param, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var constraints = new List<string>();
        if (param.HasReferenceTypeConstraint) constraints.Add("class");
        if (param.HasValueTypeConstraint) constraints.Add("struct");
        if (param.HasNotNullConstraint) constraints.Add("notnull");
        if (param.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
        foreach (var t in param.ConstraintTypes)
            constraints.Add(t.ToDisplayString());
        if (param.HasConstructorConstraint) constraints.Add("new()");

        FamilyMemberHelpersLog.DescribeTypeParameterCalled(logger, param.Name, constraints.Count);

        return constraints.Count == 0
            ? param.Name
            : $"{param.Name} : {string.Join(", ", constraints)}";
    }

    /// <summary>
    /// Returns a human-readable type-kind label.
    /// </summary>
    public static string DescribeTypeKind(INamedTypeSymbol type, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        string kind;
        if (type.TypeKind == TypeKind.Interface) kind = "Interface";
        else if (type.TypeKind == TypeKind.Struct) kind = "Struct";
        else if (type.TypeKind == TypeKind.Enum) kind = "Enum";
        else if (type.IsRecord) kind = type.IsAbstract ? "AbstractRecord" : "Record";
        else if (type.IsAbstract) kind = "AbstractClass";
        else kind = type.IsSealed ? "SealedClass" : "Class";

        FamilyMemberHelpersLog.DescribeTypeKindResult(logger, type.Name, kind);
        return kind;
    }

    /// <summary>
    /// Returns true if <paramref name="type"/> derives from or implements <paramref name="root"/>.
    /// </summary>
    public static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol root, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        // Interface root: check all interfaces of the candidate
        if (root.TypeKind == TypeKind.Interface)
        {
            FamilyMemberHelpersLog.DerivesFromInterfaceCheck(logger, type.Name, root.Name);
            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, root))
                {
                    FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, true);
                    return true;
                }
                if (iface.ConstructedFrom is not null
                    && SymbolEqualityComparer.Default.Equals(iface.ConstructedFrom, root))
                {
                    FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, true);
                    return true;
                }
            }
            FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, false);
            return false;
        }

        // Class root: walk base chain
        FamilyMemberHelpersLog.DerivesFromClassChain(logger, type.Name, root.Name);
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, root))
            {
                FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, true);
                return true;
            }
            if (current.ConstructedFrom is not null
                && SymbolEqualityComparer.Default.Equals(current.ConstructedFrom, root))
            {
                FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, true);
                return true;
            }
        }
        FamilyMemberHelpersLog.DerivesFromResult(logger, type.Name, root.Name, false);
        return false;
    }

    /// <summary>
    /// Enumerates every named type declared in <paramref name="compilation"/> including nested types.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> EnumerateAllNamedTypes(MsCompilation compilation, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var assemblyName = compilation.AssemblyName ?? "<unknown>";
        FamilyMemberHelpersLog.EnumerateAllNamedTypesStart(logger, assemblyName);
        var yielded = 0;
        var queue = new Queue<INamespaceOrTypeSymbol>();
        queue.Enqueue(compilation.GlobalNamespace);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var member in current.GetMembers())
            {
                if (member is INamespaceSymbol ns)
                {
                    queue.Enqueue(ns);
                }
                else if (member is INamedTypeSymbol type)
                {
                    yielded++;
                    yield return type;
                    foreach (var nested in type.GetTypeMembers())
                        queue.Enqueue(nested);
                }
            }
        }
        FamilyMemberHelpersLog.EnumerateAllNamedTypesDone(logger, assemblyName, yielded);
    }
}
