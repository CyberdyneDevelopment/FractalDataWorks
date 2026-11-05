using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Reads the Id a type option declares, by following its constructor chain to the parameter marked [TypeOptionId] (FDW-4045).
/// </summary>
/// <remarks>
/// Why a semantic walk and not the literal arguments of the option's own base(...): a result code passes its number to a domain
/// base, which passes it on as <c>number</c> to ResultCodeBase, which passes it as <c>id</c> to TypeOptionBase. The first base
/// names no <c>id</c>, so a match by parameter name never found it. Binding each initializer gives the constructor it calls,
/// overloads, named and default arguments included, and a constant value for literals, const fields and nameof alike.
/// </remarks>
internal static class DeclaredIdReader
{
    private const string TypeOptionIdAttributeName = "Fdw.Collections.Attributes.TypeOptionIdAttribute";

    /// <summary>Reads an option's declared Id.</summary>
    /// <param name="option">The type option.</param>
    /// <param name="compilation">The compilation the option is read from.</param>
    /// <returns>
    /// The Id as invariant text when it is read; otherwise why it cannot be read, for an option written in this compilation.
    /// Both are <see langword="null"/> for an option compiled into another assembly, whose constructors show no initializer.
    /// </returns>
    public static (string? Id, string? Problem) Read(INamedTypeSymbol option, Compilation compilation)
    {
        if (!option.Locations.Any(location => location.IsInSource)
            || compilation.GetTypeByMetadataName(TypeOptionIdAttributeName) is not { } idAttribute)
        {
            return (null, null);
        }

        var start = option.InstanceConstructors.FirstOrDefault(ctor => ctor.Parameters.Length == 0 && !ctor.IsImplicitlyDeclared);
        return start is null
            ? (null, "it declares no parameterless constructor")
            : Follow(start, compilation, idAttribute);
    }

    private static (string? Id, string? Problem) Follow(IMethodSymbol start, Compilation compilation, INamedTypeSymbol idAttribute)
    {
        var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        IReadOnlyDictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var current = start.OriginalDefinition;

        while (visited.Add(current))
        {
            if (Call(current, compilation) is not { } call)
            {
                return (null, $"the constructor of {current.ContainingType.Name} is not source and marks no parameter [TypeOptionId]");
            }

            var outgoing = Outgoing(call, values);
            var target = call.TargetMethod.OriginalDefinition;
            if (IdParameter(target, idAttribute) is { } idParameter)
            {
                return outgoing.TryGetValue(idParameter.Name, out var id) && id is not null
                    ? (id, null)
                    : (null, $"the Id it passes to {target.ContainingType.Name} is not a constant");
            }

            values = outgoing;
            current = target;
        }

        return (null, "its constructor chain calls itself");
    }

    // The this(...) or base(...) call a constructor makes, bound; none for a constructor with no initializer in source.
    private static IInvocationOperation? Call(IMethodSymbol ctor, Compilation compilation)
    {
        if (ctor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not ConstructorDeclarationSyntax { Initializer: { } initializer } syntax)
        {
            return null;
        }

        return compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(initializer) switch
        {
            IInvocationOperation invocation => invocation,
            IExpressionStatementOperation { Operation: IInvocationOperation invocation } => invocation,
            _ => null,
        };
    }

    private static Dictionary<string, string?> Outgoing(IInvocationOperation call, IReadOnlyDictionary<string, string?> incoming)
    {
        var outgoing = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var argument in call.Arguments.Where(argument => argument.Parameter is not null))
        {
            outgoing[argument.Parameter?.Name ?? string.Empty] = ValueOf(argument.Value, incoming);
        }

        return outgoing;
    }

    private static IParameterSymbol? IdParameter(IMethodSymbol ctor, INamedTypeSymbol idAttribute) =>
        ctor.Parameters.FirstOrDefault(parameter => parameter.GetAttributes()
            .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, idAttribute)));

    // A constant as invariant text, or the value the constructor was handed for the parameter it forwards; none otherwise.
    private static string? ValueOf(IOperation value, IReadOnlyDictionary<string, string?> incoming)
    {
        if (value.ConstantValue.HasValue)
        {
            return value.ConstantValue.Value is { } constant ? Convert.ToString(constant, CultureInfo.InvariantCulture) : null;
        }

        return value switch
        {
            IParameterReferenceOperation reference => incoming.TryGetValue(reference.Parameter.Name, out var forwarded) ? forwarded : null,
            IConversionOperation conversion => ValueOf(conversion.Operand, incoming),
            _ => null,
        };
    }
}
