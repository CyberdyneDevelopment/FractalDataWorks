using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Models;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Generators;

/// <summary>
/// Generates Empty classes for collection value types.
/// </summary>
public sealed class EmptyClassGenerator
{
    private readonly CollectionBuilderConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyClassGenerator"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public EmptyClassGenerator(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Generates an Empty class for a collection value type.
    /// </summary>
    /// <param name="definition">The type definition</param>
    /// <param name="returnType">The return type (e.g., "AuthenticationTypeBase" or fully qualified)</param>
    /// <param name="namespace">The namespace for the Empty class</param>
    /// <param name="compilation">The compilation context for type resolution</param>
    /// <returns>The generated Empty class code</returns>
#pragma warning disable MA0051 // Sequential type resolution and code generation with low complexity
    public static string GenerateEmptyClass<TId>(
        GenericTypeInfoModel<TId> definition,
        string returnType,
        string @namespace,
        Compilation compilation)
        where TId : struct
    {
        // Extract simple type name from potentially fully qualified type
        // E.g., "Fdw.Services.Connections.Abstractions.IConnectionState" => "IConnectionState"
        var simpleTypeName = returnType.Contains(".")
            ? returnType.Substring(returnType.LastIndexOf('.') + 1)
            : returnType;

        // Remove generic type parameters for the class name
        // E.g., "ConnectionTypeBase<TService, TConfiguration, TFactory>" => "ConnectionTypeBase"
        var genericStartIndex = simpleTypeName.IndexOf('<');
        if (genericStartIndex > 0)
        {
            simpleTypeName = simpleTypeName.Substring(0, genericStartIndex);
        }

        var emptyClassName = $"Empty{simpleTypeName}";
        var sb = new StringBuilder();

        // Add usings
        sb.AppendLine("using System;");

        // Add using for the base type's namespace if it's different from the current namespace
        if (!string.IsNullOrEmpty(definition.FullTypeName))
        {
            var baseTypeNamespace = definition.FullTypeName.Contains(".")
                ? definition.FullTypeName.Substring(0, definition.FullTypeName.LastIndexOf('.'))
                : @namespace;

            // Remove generic type parameters from namespace extraction
            var genericIndex = baseTypeNamespace.IndexOf('<');
            if (genericIndex > 0)
            {
                baseTypeNamespace = baseTypeNamespace.Substring(0, genericIndex);
                // Re-extract namespace after removing generics
                var lastDot = baseTypeNamespace.LastIndexOf('.');
                if (lastDot > 0)
                {
                    baseTypeNamespace = baseTypeNamespace.Substring(0, lastDot);
                }
            }

            if (!string.Equals(baseTypeNamespace, @namespace, StringComparison.Ordinal))
            {
                sb.AppendLine($"using {baseTypeNamespace};");
            }
        }

        sb.AppendLine();

        // Namespace
        if (!string.IsNullOrEmpty(@namespace))
        {
            sb.AppendLine($"namespace {@namespace};");
            sb.AppendLine();
        }

        // Find the base type symbol for analysis
        // First, try to get it from the definition's ClassName which has the metadata name
        INamedTypeSymbol? baseTypeSymbol = null;

        // Try multiple strategies to resolve the base type
        // Strategy 1: Try using the definition's FullTypeName with metadata format
        if (!string.IsNullOrEmpty(definition.FullTypeName))
        {
            // For generic types, extract the metadata name
            var metadataName = definition.FullTypeName;
            var genericIndex = metadataName.IndexOf('<');
            if (genericIndex > 0)
            {
                // Extract namespace and simple name
                var lastDot = metadataName.LastIndexOf('.', genericIndex > 0 ? genericIndex : metadataName.Length - 1);
                var namespacePart = lastDot > 0 ? metadataName.Substring(0, lastDot) : @namespace;
                var namePart = lastDot > 0 ? metadataName.Substring(lastDot + 1, genericIndex - lastDot - 1) : metadataName.Substring(0, genericIndex);

                // Count the generic parameters to get arity
                var typeParamCount = metadataName.Split(',').Length;
                metadataName = $"{namespacePart}.{namePart}`{typeParamCount}";
                baseTypeSymbol = compilation.GetTypeByMetadataName(metadataName);
            }
            else
            {
                baseTypeSymbol = compilation.GetTypeByMetadataName(definition.FullTypeName);
            }
        }

        // Strategy 2: Try combining namespace with simple type name
        if (baseTypeSymbol == null)
        {
            var fullyQualifiedTypeName = returnType.Contains(".")
                ? returnType
                : $"{@namespace}.{returnType}";

            // Remove generic parameters to get the base name
            var fallbackGenericIndex = fullyQualifiedTypeName.IndexOf('<');
            if (fallbackGenericIndex > 0)
            {
                var baseName = fullyQualifiedTypeName.Substring(0, fallbackGenericIndex);
                // Try to infer arity from the type parameter list
                var typeParams = fullyQualifiedTypeName.Substring(fallbackGenericIndex);
                var arity = typeParams.Split(',').Length;
                baseTypeSymbol = compilation.GetTypeByMetadataName($"{baseName}`{arity}");
            }
            else
            {
                baseTypeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
            }
        }

        // Generate class declaration with generic parameters if needed
        string classDeclaration;
        string baseInheritance;
        string typeParameterConstraints = string.Empty;

        if (baseTypeSymbol != null && GenericTypeHelper.IsGenericType(baseTypeSymbol))
        {
            // Generic base type - make Empty class generic too
            var typeParams = GenericTypeHelper.GetTypeParameterList(baseTypeSymbol);
            classDeclaration = $"public sealed class {emptyClassName}{typeParams}";
            baseInheritance = $"{simpleTypeName}{typeParams}";
            typeParameterConstraints = GenericTypeHelper.GetTypeParameterConstraints(baseTypeSymbol, "    ");
        }
        else
        {
            // Non-generic base type
            classDeclaration = $"public sealed class {emptyClassName}";
            baseInheritance = returnType;
        }

        // XML documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Empty null-object implementation of {simpleTypeName} with default values.");
        sb.AppendLine("/// </summary>");

        // Class declaration with inheritance
        sb.AppendLine($"{classDeclaration} : {baseInheritance}");

        // Add type parameter constraints if any
        if (!string.IsNullOrEmpty(typeParameterConstraints))
        {
            sb.Append(typeParameterConstraints);
        }

        sb.AppendLine("{");
        if (baseTypeSymbol != null)
        {
            // Find the protected or public constructor with minimum parameters
            var baseConstructor = baseTypeSymbol.Constructors
                .Where(c => !c.IsStatic &&
                           (c.DeclaredAccessibility == Accessibility.Protected ||
                            c.DeclaredAccessibility == Accessibility.Public))
                .OrderBy(c => c.Parameters.Length)
                .FirstOrDefault();

            if (baseConstructor != null)
            {
                var baseCallArgs = new List<string>();
                foreach (var param in baseConstructor.Parameters)
                {
                    var defaultValue = GetDefaultValueForTypeSymbol(param.Type);
                    baseCallArgs.Add(defaultValue);
                }

                // Constructor
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{emptyClassName}\"/> class with default values.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public {emptyClassName}()");
                sb.AppendLine($"        : base({string.Join(", ", baseCallArgs)})");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Implement all abstract methods
            GenerateAbstractMethodImplementations(sb, baseTypeSymbol, compilation);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
#pragma warning restore MA0051

    /// <summary>
    /// Generates implementations for all abstract methods in the inheritance chain.
    /// </summary>
    private static void GenerateAbstractMethodImplementations(StringBuilder sb, INamedTypeSymbol baseType, Compilation compilation)
    {
        var implementedMethods = new HashSet<string>(StringComparer.Ordinal);

        // Walk up the inheritance chain
        var currentType = baseType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IMethodSymbol method && method.IsAbstract && !method.IsStatic && method.MethodKind == MethodKind.Ordinary)
                {
                    // Create a unique signature for deduplication
                    var signature = GetMethodSignature(method);
                    if (implementedMethods.Contains(signature))
                        continue;

                    implementedMethods.Add(signature);
                    GenerateAbstractMethodImplementation(sb, method, compilation);
                }
            }

            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// Generates implementation for a single abstract method.
    /// </summary>
    private static void GenerateAbstractMethodImplementation(StringBuilder sb, IMethodSymbol method, Compilation compilation)
    {
        // XML documentation
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Empty implementation of {method.Name}.");
        sb.AppendLine("    /// </summary>");

        // Method signature
        var returnTypeString = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));

        sb.AppendLine($"    public override {returnTypeString} {methodName}({parameters})");
        sb.AppendLine("    {");

        // Generate return statement
        var returnStatement = GenerateReturnStatement(method, compilation);
        if (!string.IsNullOrEmpty(returnStatement))
        {
            sb.AppendLine($"        {returnStatement}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates appropriate return statement for a method.
    /// Logic:
    /// 1. If return type matches an input parameter type -> return that parameter
    /// 2. If IGenericResult&lt;T&gt; where T matches input parameter -> return GenericResult&lt;T&gt;.Success(parameter)
    /// 3. If string -> return string.Empty
    /// 4. Otherwise -> return default
    /// </summary>
    private static string GenerateReturnStatement(IMethodSymbol method, Compilation compilation)
    {
        var returnType = method.ReturnType;

        // Void methods
        if (returnType.SpecialType == SpecialType.System_Void)
            return string.Empty;

        // Check if return type matches any parameter type
        foreach (var param in method.Parameters)
        {
            if (SymbolEqualityComparer.Default.Equals(returnType, param.Type))
            {
                return $"return {param.Name};";
            }
        }

        // Check for IGenericResult<T> where T matches a parameter
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var genericDef = namedReturnType.OriginalDefinition.ToDisplayString();

            // Check if it's IGenericResult<T>
            if (genericDef.Contains("IGenericResult") && namedReturnType.TypeArguments.Length == 1)
            {
                var resultGenericType = namedReturnType.TypeArguments[0];

                // Find parameter matching the generic type
                foreach (var param in method.Parameters)
                {
                    if (SymbolEqualityComparer.Default.Equals(resultGenericType, param.Type))
                    {
                        var genericTypeDisplay = resultGenericType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return $"return global::Fdw.Results.GenericResult<{genericTypeDisplay}>.Success({param.Name});";
                    }
                }
            }
        }

        // String -> return string.Empty
        if (returnType.SpecialType == SpecialType.System_String)
            return "return string.Empty;";

        // Default for everything else
        return "return default;";
    }

    /// <summary>
    /// Creates a unique signature for a method (for deduplication).
    /// </summary>
    private static string GetMethodSignature(IMethodSymbol method)
    {
        var paramTypes = string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{method.Name}({paramTypes})";
    }

    /// <summary>
    /// Gets the default value for a type symbol.
    /// </summary>
    private static string GetDefaultValueForTypeSymbol(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.SpecialType == SpecialType.System_String)
            return "string.Empty";
        if (typeSymbol.SpecialType == SpecialType.System_Int32)
            return "0";
        if (typeSymbol.SpecialType == SpecialType.System_Boolean)
            return "false";
        if (typeSymbol.IsValueType)
            return "default";
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            return "null!";
        if (typeSymbol.IsReferenceType)
            return "null!";
        return "default";
    }
}
