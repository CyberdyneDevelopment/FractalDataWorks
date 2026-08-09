using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Shared code generation utilities.
/// </summary>
internal static class CodeGeneration
{
    /// <summary>
    /// Gets a named argument value from an attribute.
    /// </summary>
    public static T? GetNamedArgument<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs, string name)
        where T : class
    {
        return namedArgs
            .FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal))
            .Value.Value as T;
    }

    /// <summary>
    /// Gets a named argument value from an attribute, with a default value.
    /// </summary>
    public static T GetNamedArgument<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs, string name, T defaultValue)
        where T : struct
    {
        var value = namedArgs
            .FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal))
            .Value.Value;

        return value is T typedValue ? typedValue : defaultValue;
    }

    /// <summary>
    /// Converts a name to camelCase.
    /// </summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Converts a name to PascalCase.
    /// </summary>
    /// <remarks>
    /// Used to build compound identifiers whose first segment is a fixed generator-owned prefix,
    /// e.g. the <c>_option</c> singleton fields. Upper-casing the user-supplied segment is what
    /// keeps such an identifier out of the namespace a bare <c>_{camelCase(name)}</c> occupies —
    /// see <see cref="ReservedMemberNames"/> for why that separation matters.
    /// </remarks>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Generates static property accessors for TypeOptions.
    /// </summary>
    public static void GenerateStaticAccessors(
        StringBuilder sb,
        ImmutableArray<TypeOptionModel> options,
        bool isSingleton)
    {
        foreach (var option in options)
        {
            var fieldName = $"_{ToCamelCase(option.OptionName)}";

            if (isSingleton)
            {
                // Singleton: lazy initialization
                sb.AppendLine($"        private static {option.FullTypeName}? {fieldName};");
                sb.AppendLine($"        /// <summary>Gets the {option.OptionName} singleton instance.</summary>");
                sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName} =>");
                sb.AppendLine($"            {fieldName} ??= new {option.FullTypeName}();");
            }
            else
            {
                // Factory: create new instance
                sb.AppendLine($"        /// <summary>Creates a new instance of {option.OptionName}.</summary>");
                sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}() =>");
                sb.AppendLine($"            new {option.FullTypeName}();");
            }
            sb.AppendLine();

            // Method overloads for parameterized constructors
            // Skip constructors where all parameters have defaults (would conflict with parameterless property/method)
            foreach (var ctor in option.Constructors.Where(c => c.HasParameters && c.Parameters.Any(p => !p.HasDefaultValue)))
            {
                var parameters = string.Join(", ",
                    ctor.Parameters.Select(p =>
                        p.HasDefaultValue
                            ? $"{p.Type} {p.Name} = {p.DefaultValue}"
                            : $"{p.Type} {p.Name}"));

                var arguments = string.Join(", ",
                    ctor.Parameters.Select(p => p.Name));

                sb.AppendLine($"        /// <summary>Creates a new instance of {option.OptionName} with the specified parameters.</summary>");
                sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}({parameters}) =>");
                sb.AppendLine($"            new {option.FullTypeName}({arguments});");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates the NotFound sentinel class.
    /// </summary>
    public static void GenerateNotFoundSentinel(
        StringBuilder sb,
        TypeCollectionModel collection,
        ImmutableArray<AbstractMemberModel> abstractMembers,
        string? interfaceTypeName = null,
        HashSet<string>? userDeclaredMembers = null)
    {
        var baseArgs = GenerateDefaultArguments(collection.BaseConstructorParameters);

        sb.AppendLine($"        private partial class NotFound{collection.ClassName} : {collection.BaseTypeName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            public NotFound{collection.ClassName}() : base({baseArgs}) {{ }}");

        // Generate stubs for abstract members
        if (abstractMembers.Length > 0)
        {
            GenerateAbstractMemberStubs(sb, abstractMembers, interfaceTypeName, userDeclaredMembers);
        }

        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates stub implementations for abstract members.
    /// </summary>
    public static void GenerateAbstractMemberStubs(
        StringBuilder sb,
        ImmutableArray<AbstractMemberModel> abstractMembers,
        string? interfaceTypeName,
        HashSet<string>? userDeclaredMembers = null)
    {
        foreach (var member in abstractMembers)
        {
            // Skip members the user has already declared in their partial class
            if (userDeclaredMembers is not null && userDeclaredMembers.Contains(member.Name))
                continue;

            if (member.IsProperty)
            {
                GeneratePropertyStub(sb, member);
            }
            else if (member.IsMethod)
            {
                GenerateMethodStub(sb, member, interfaceTypeName);
            }
        }
    }

    /// <summary>
    /// Generates a property stub.
    /// </summary>
    // Code generation template for property syntax — exhaustive cases for explicit interfaces
#pragma warning disable FDW006, FDW007
    private static void GeneratePropertyStub(StringBuilder sb, AbstractMemberModel member)
    {
        var defaultValue = GetDefaultReturnExpression(member.ReturnType, null, null);

        // Handle explicit interface implementation
        if (member.ExplicitInterfaceType != null)
        {
            var interfacePrefix = $"{member.ExplicitInterfaceType}.";
            if (member.HasGetter && member.HasSetter)
            {
                sb.AppendLine($"            {member.ReturnType} {interfacePrefix}{member.Name} {{ get => {defaultValue}; set {{ }} }}");
            }
            else if (member.HasGetter)
            {
                sb.AppendLine($"            {member.ReturnType} {interfacePrefix}{member.Name} => {defaultValue};");
            }
            else if (member.HasSetter)
            {
                sb.AppendLine($"            {member.ReturnType} {interfacePrefix}{member.Name} {{ set {{ }} }}");
            }
            return;
        }

        var modifier = member.IsOverride ? "override " : "";

        if (member.HasGetter && member.HasSetter)
        {
            sb.AppendLine($"            public {modifier}{member.ReturnType} {member.Name} {{ get => {defaultValue}; set {{ }} }}");
        }
        else if (member.HasGetter)
        {
            sb.AppendLine($"            public {modifier}{member.ReturnType} {member.Name} => {defaultValue};");
        }
        else if (member.HasSetter)
        {
            sb.AppendLine($"            public {modifier}{member.ReturnType} {member.Name} {{ set {{ }} }}");
        }
    }
#pragma warning restore FDW006, FDW007

    /// <summary>
    /// Generates a method stub.
    /// </summary>
    private static void GenerateMethodStub(StringBuilder sb, AbstractMemberModel member, string? interfaceTypeName)
    {
        var parameters = string.Join(", ", member.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var returnExpr = GetDefaultReturnExpression(member.ReturnType, member.MatchingParameterName, interfaceTypeName, member.TypeParameters);

        // Build type parameters string if any
        var typeParamsStr = !member.TypeParameters.IsDefault && member.TypeParameters.Length > 0
            ? $"<{string.Join(", ", member.TypeParameters)}>"
            : "";

        // Handle explicit interface implementation
        if (member.ExplicitInterfaceType != null)
        {
            var interfacePrefix = $"{member.ExplicitInterfaceType}.";
            if (string.Equals(member.ReturnType, "void", StringComparison.Ordinal) || string.Equals(member.ReturnType, "System.Void", StringComparison.Ordinal))
            {
                sb.AppendLine($"            void {interfacePrefix}{member.Name}{typeParamsStr}({parameters}) {{ }}");
            }
            else
            {
                sb.AppendLine($"            {member.ReturnType} {interfacePrefix}{member.Name}{typeParamsStr}({parameters}) => {returnExpr};");
            }
            return;
        }

        var modifier = member.IsOverride ? "override " : "";

        if (string.Equals(member.ReturnType, "void", StringComparison.Ordinal) || string.Equals(member.ReturnType, "System.Void", StringComparison.Ordinal))
        {
            sb.AppendLine($"            public {modifier}void {member.Name}{typeParamsStr}({parameters}) {{ }}");
        }
        else
        {
            sb.AppendLine($"            public {modifier}{member.ReturnType} {member.Name}{typeParamsStr}({parameters}) => {returnExpr};");
        }
    }

    /// <summary>
    /// Gets the appropriate default return expression for a method.
    /// </summary>
    public static string GetDefaultReturnExpression(string returnType, string? matchingParamName, string? interfaceTypeName, ImmutableArray<string> typeParameters = default)
    {
        // 1. If a parameter matches the return type, return it (identity transform)
        if (!string.IsNullOrEmpty(matchingParamName))
        {
            return matchingParamName!;
        }

        // 2. If return type is the interface itself (fluent pattern), return this
        if (interfaceTypeName != null &&
            (string.Equals(returnType, interfaceTypeName, StringComparison.Ordinal) ||
             returnType.EndsWith(interfaceTypeName, StringComparison.Ordinal)))
        {
            return "this";
        }

        // 3. Handle generic collection returns where T is a method type parameter
        if (!typeParameters.IsDefault && typeParameters.Length > 0)
        {
            foreach (var tp in typeParameters)
            {
                // Why: IReadOnlyList<T> and IList<T> need Array.Empty<T>() (which implements both).
                // IEnumerable<T> uses Enumerable.Empty<T>() which is lighter.
                // Check specific collection interfaces before the broad IEnumerable catch-all.
                // Use global:: to avoid ambiguity with types whose names start with "System".
                if (returnType.Contains($"IReadOnlyList<{tp}>") ||
                    returnType.Contains($"IList<{tp}>") ||
                    returnType.Contains($"IReadOnlyCollection<{tp}>") ||
                    returnType.Contains($"ICollection<{tp}>"))
                {
                    return $"global::System.Array.Empty<{tp}>()";
                }

                if (returnType.Contains($"IEnumerable<{tp}>") ||
                    returnType.EndsWith($"<{tp}>", StringComparison.Ordinal))
                {
                    return $"global::System.Linq.Enumerable.Empty<{tp}>()";
                }
            }
        }

        // 4. Handle special types
        return GetDefaultReturnForType(returnType);
    }

    /// <summary>
    /// Gets a default return expression for a given return type.
    /// </summary>
#pragma warning disable MA0051 // Exhaustive type-mapping switch must cover all CLR types in one place
    private static string GetDefaultReturnForType(string typeName)
    {
        // Handle nullable types - return null
        if (typeName.EndsWith("?", StringComparison.Ordinal))
        {
            return "default";
        }

        // Handle void
        if (string.Equals(typeName, "void", StringComparison.Ordinal) ||
            string.Equals(typeName, "System.Void", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        // Handle string
        if (string.Equals(typeName, "string", StringComparison.Ordinal) ||
            string.Equals(typeName, "System.String", StringComparison.Ordinal))
        {
            return "string.Empty";
        }

        // Handle bool
        if (string.Equals(typeName, "bool", StringComparison.Ordinal) ||
            string.Equals(typeName, "System.Boolean", StringComparison.Ordinal))
        {
            return "false";
        }

        // Handle arrays
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementType = typeName.Substring(0, typeName.Length - 2);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle Task
        if (string.Equals(typeName, "System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            string.Equals(typeName, "Task", StringComparison.Ordinal))
        {
            return "System.Threading.Tasks.Task.CompletedTask";
        }

        // Handle Task<T>
        if (typeName.StartsWith("System.Threading.Tasks.Task<", StringComparison.Ordinal))
        {
            var innerType = typeName.Substring(28, typeName.Length - 29);
            return $"System.Threading.Tasks.Task.FromResult<{innerType}>(default!)";
        }
        if (typeName.StartsWith("Task<", StringComparison.Ordinal))
        {
            var innerType = typeName.Substring(5, typeName.Length - 6);
            return $"System.Threading.Tasks.Task.FromResult<{innerType}>(default!)";
        }

        // Handle ValueTask
        if (string.Equals(typeName, "System.Threading.Tasks.ValueTask", StringComparison.Ordinal) ||
            string.Equals(typeName, "ValueTask", StringComparison.Ordinal))
        {
            return "default";
        }

        // Handle ValueTask<T>
        if (typeName.StartsWith("System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
        {
            var innerType = typeName.Substring(33, typeName.Length - 34);
            return $"new System.Threading.Tasks.ValueTask<{innerType}>(default!)";
        }
        if (typeName.StartsWith("ValueTask<", StringComparison.Ordinal))
        {
            var innerType = typeName.Substring(10, typeName.Length - 11);
            return $"new System.Threading.Tasks.ValueTask<{innerType}>(default!)";
        }

        // Handle Type
        if (string.Equals(typeName, "System.Type", StringComparison.Ordinal) ||
            string.Equals(typeName, "Type", StringComparison.Ordinal))
        {
            return "typeof(object)";
        }

        // Handle IReadOnlyDictionary
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyDictionary<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyDictionary<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var typeArgs = typeName.Substring(start, end - start);
            return $"new System.Collections.Generic.Dictionary<{typeArgs}>()";
        }

        // Handle IReadOnlyList
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyList<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IReadOnlyCollection
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyCollection<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IEnumerable
        if (typeName.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) ||
            typeName.StartsWith("IEnumerable<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle Guid
        if (string.Equals(typeName, "System.Guid", StringComparison.Ordinal) ||
            string.Equals(typeName, "Guid", StringComparison.Ordinal))
        {
            return "System.Guid.Empty";
        }

        // Handle TimeSpan
        if (string.Equals(typeName, "System.TimeSpan", StringComparison.Ordinal) ||
            string.Equals(typeName, "TimeSpan", StringComparison.Ordinal))
        {
            return "System.TimeSpan.Zero";
        }

        // Handle numeric types
        if (IsNumericType(typeName))
        {
            return "0";
        }

        // Default for reference types
        return "default!";
    }

    /// <summary>
    /// Checks if a type is numeric.
    /// </summary>
    // Exhaustive type mapping for numeric types
#pragma warning disable FDW006, FDW007
    private static bool IsNumericType(string typeName)
    {
        return typeName switch
        {
            "int" or "System.Int32" => true,
            "long" or "System.Int64" => true,
            "short" or "System.Int16" => true,
            "byte" or "System.Byte" => true,
            "sbyte" or "System.SByte" => true,
            "uint" or "System.UInt32" => true,
            "ulong" or "System.UInt64" => true,
            "ushort" or "System.UInt16" => true,
            "float" or "System.Single" => true,
            "double" or "System.Double" => true,
            "decimal" or "System.Decimal" => true,
            _ => false
        };
    }
#pragma warning restore FDW006, FDW007

    /// <summary>
    /// Generates default argument values for a constructor call.
    /// </summary>
    public static string GenerateDefaultArguments(ImmutableArray<ParameterModel> parameters)
    {
        if (parameters.Length == 0)
            return string.Empty;

        var args = parameters.Select(p => GetDefaultValueForType(p.Type));
        return string.Join(", ", args);
    }

    /// <summary>
    /// Gets parameters that cannot be safely defaulted (unknown reference types).
    /// </summary>
    public static ImmutableArray<ParameterModel> GetUnsafeParameters(ImmutableArray<ParameterModel> parameters)
    {
        return parameters
            .Where(p => !CanSafelyDefault(p.Type))
            .ToImmutableArray();
    }

    /// <summary>
    /// Determines if a type can be safely defaulted without risking null reference issues.
    /// </summary>
    private static bool CanSafelyDefault(string typeName)
    {
        // Nullable types are safe (null is a valid value)
        if (typeName.EndsWith("?", StringComparison.Ordinal))
            return true;

        // Arrays are safe (Array.Empty<T>() or new T[0])
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
            return true;

        // Check if it's a known safe type
        return typeName switch
        {
            // Strings
            "string" or "System.String" => true,
            // Integers
            "int" or "System.Int32" => true,
            "long" or "System.Int64" => true,
            "short" or "System.Int16" => true,
            "byte" or "System.Byte" => true,
            "sbyte" or "System.SByte" => true,
            "uint" or "System.UInt32" => true,
            "ulong" or "System.UInt64" => true,
            "ushort" or "System.UInt16" => true,
            // Floating point
            "float" or "System.Single" => true,
            "double" or "System.Double" => true,
            "decimal" or "System.Decimal" => true,
            // Boolean
            "bool" or "System.Boolean" => true,
            // Char
            "char" or "System.Char" => true,
            // Common value types
            "System.Guid" or "Guid" => true,
            "System.DateTime" or "DateTime" => true,
            "System.DateTimeOffset" or "DateTimeOffset" => true,
            "System.TimeSpan" or "TimeSpan" => true,
            // Type (now handled)
            "Type" or "System.Type" => true,
            // Common enums (value types - default gives 0)
            "System.Data.DbType" or "DbType" => true,
            // Unknown types - not safe
            _ => false
        };
    }

    /// <summary>
    /// Gets the appropriate default value for a type.
    /// </summary>
#pragma warning disable MA0051 // Exhaustive type-mapping switch must cover all CLR types in one place
    private static string GetDefaultValueForType(string typeName)
    {
        // Handle nullable types
        if (typeName.EndsWith("?", StringComparison.Ordinal))
            return "null";

        // Handle arrays
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementType = typeName.Substring(0, typeName.Length - 2);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IReadOnlyList<T>
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyList<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IReadOnlyCollection<T>
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyCollection<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IEnumerable<T>
        if (typeName.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) ||
            typeName.StartsWith("IEnumerable<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var elementType = typeName.Substring(start, end - start);
            return $"System.Array.Empty<{elementType}>()";
        }

        // Handle IReadOnlyDictionary<TKey, TValue>
        if (typeName.StartsWith("System.Collections.Generic.IReadOnlyDictionary<", StringComparison.Ordinal) ||
            typeName.StartsWith("IReadOnlyDictionary<", StringComparison.Ordinal))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            var typeArgs = typeName.Substring(start, end - start);
            return $"new System.Collections.Generic.Dictionary<{typeArgs}>()";
        }

        // Handle common types
        // NOTE: Use "_Empty" for strings instead of string.Empty to avoid issues
        // with base classes that validate non-empty names (e.g., DataCommandTranslatorBase)
        return typeName switch
        {
            "string" => "\"_Empty\"",
            "System.String" => "\"_Empty\"",
            "int" => "0",
            "System.Int32" => "0",
            "long" => "0L",
            "System.Int64" => "0L",
            "short" => "0",
            "System.Int16" => "0",
            "byte" => "0",
            "System.Byte" => "0",
            "sbyte" => "0",
            "System.SByte" => "0",
            "uint" => "0U",
            "System.UInt32" => "0U",
            "ulong" => "0UL",
            "System.UInt64" => "0UL",
            "ushort" => "0",
            "System.UInt16" => "0",
            "float" => "0f",
            "System.Single" => "0f",
            "double" => "0d",
            "System.Double" => "0d",
            "decimal" => "0m",
            "System.Decimal" => "0m",
            "bool" => "false",
            "System.Boolean" => "false",
            "char" => "'\\0'",
            "System.Char" => "'\\0'",
            "System.Guid" => "System.Guid.Empty",
            "Guid" => "System.Guid.Empty",
            "System.DateTime" => "default",
            "DateTime" => "default",
            "System.DateTimeOffset" => "default",
            "DateTimeOffset" => "default",
            "System.TimeSpan" => "default",
            "TimeSpan" => "default",
            "object" => "null!",
            "System.Object" => "null!",
            "Type" => "typeof(object)",
            "System.Type" => "typeof(object)",
            // Common enums - use default (gives 0 value)
            "System.Data.DbType" or "DbType" => "default(System.Data.DbType)",
            _ => $"default({typeName})!"
        };
    }

    /// <summary>
    /// Generates common using statements.
    /// </summary>
    public static void GenerateUsings(StringBuilder sb, CollectionKind kind)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");

#pragma warning disable FDW018 // Source generator internal enum — bootstrapping prevents TypeCollection usage
        switch (kind)
        {
            case CollectionKind.Immutable:
                sb.AppendLine("#if NETSTANDARD2_0");
                sb.AppendLine("using System.Collections.Immutable;");
                sb.AppendLine("#else");
                sb.AppendLine("using System.Collections.Frozen;");
                sb.AppendLine("#endif");
                break;
            case CollectionKind.Mutable:
                sb.AppendLine("using System.Collections.Concurrent;");
                break;
            case CollectionKind.Factory:
                // No special using needed
                break;
        }
#pragma warning restore FDW018

        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
    }

    /// <summary>
    /// Checks if an interface (or its base interfaces) has a property with the specified name.
    /// </summary>
    public static bool InterfaceHasProperty(ITypeSymbol? interfaceType, string propertyName)
    {
        if (interfaceType == null)
            return false;

        // Check the interface itself
        if (interfaceType is INamedTypeSymbol namedInterface)
        {
            // Check direct members
            foreach (var member in namedInterface.GetMembers())
            {
                if (member is IPropertySymbol property &&
                    string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // Check all inherited interfaces
            foreach (var baseInterface in namedInterface.AllInterfaces)
            {
                foreach (var member in baseInterface.GetMembers())
                {
                    if (member is IPropertySymbol property &&
                        string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts abstract members from the base type that need implementation in the Empty sentinel.
    /// </summary>
    public static ImmutableArray<AbstractMemberModel> ExtractAbstractMembers(
        ITypeSymbol? baseType,
        ITypeSymbol? interfaceType)
    {
        var members = ImmutableArray.CreateBuilder<AbstractMemberModel>();
        var processedSignatures = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var processedPropertyNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        // Get abstract methods and properties from base type
        if (baseType is INamedTypeSymbol namedBaseType)
        {
            ExtractAbstractMembersFromType(namedBaseType, members, processedSignatures, processedPropertyNames);

            // Also get interface members that the base type doesn't implement
            foreach (var iface in namedBaseType.AllInterfaces)
            {
                ExtractUnimplementedInterfaceMembers(iface, namedBaseType, members, processedSignatures, processedPropertyNames);
            }
        }

        // Also check interface type directly if different from base type's interfaces
        if (interfaceType is INamedTypeSymbol namedInterfaceType && baseType != null)
        {
            ExtractUnimplementedInterfaceMembers(namedInterfaceType, baseType as INamedTypeSymbol, members, processedSignatures, processedPropertyNames);
        }

        return members.ToImmutable();
    }

    private static void ExtractUnimplementedInterfaceMembers(
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol? baseType,
        ImmutableArray<AbstractMemberModel>.Builder members,
        System.Collections.Generic.HashSet<string> processedSignatures,
        System.Collections.Generic.HashSet<string> processedPropertyNames)
    {
        foreach (var member in interfaceType.GetMembers())
        {
            if (member is IMethodSymbol method &&
                method.MethodKind == MethodKind.Ordinary &&
                !method.IsStatic)
            {
                // Check if base type has a concrete implementation
                if (baseType != null && HasConcreteImplementation(baseType, method))
                    continue;

                // Include generic methods - we'll generate stubs with type parameters
                var signature = GetMethodSignature(method);
                if (processedSignatures.Add(signature))
                {
                    // Interface members don't need 'override'
                    members.Add(CreateAbstractMemberModel(method, isInterfaceImplementation: true));
                }
            }
            else if (member is IPropertySymbol property &&
                     !property.IsStatic)
            {
                // Skip properties with unresolved generic type parameters (properties can't be generic)
                if (HasUnresolvedTypeParameters(property.Type))
                {
                    continue;
                }

                // Check if base type has a concrete implementation
                if (baseType != null && HasConcreteImplementation(baseType, property))
                    continue;

                // Check if we've already seen a property with this name (different type = needs explicit implementation)
                string? explicitInterfaceType = null;
                if (processedPropertyNames.Contains(property.Name))
                {
                    // Already have a public property with this name, need explicit implementation
                    explicitInterfaceType = interfaceType.ToDisplayString();
                }
                else
                {
                    processedPropertyNames.Add(property.Name);
                }

                // Include return type in signature to track all properties
                var signature = $"prop:{property.Type.ToDisplayString()}:{property.Name}";
                if (processedSignatures.Add(signature))
                {
                    members.Add(CreateAbstractMemberModel(property, isInterfaceImplementation: true, explicitInterfaceType: explicitInterfaceType));
                }
            }
        }
    }

    private static bool HasConcreteImplementation(INamedTypeSymbol type, ISymbol interfaceMember)
    {
        // Walk up the type hierarchy looking for a concrete implementation
        var current = type;
        while (current != null)
        {
            // Look for a matching member by name
            var matchingMembers = current.GetMembers(interfaceMember.Name);
            foreach (var member in matchingMembers)
            {
                if (interfaceMember is IMethodSymbol interfaceMethod && member is IMethodSymbol method)
                {
                    // Check if signatures match and it's not abstract
                    if (!method.IsAbstract &&
                        method.Parameters.Length == interfaceMethod.Parameters.Length)
                    {
                        return true;
                    }
                }
                else if (interfaceMember is IPropertySymbol && member is IPropertySymbol property)
                {
                    if (!property.IsAbstract)
                    {
                        return true;
                    }
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    private static void ExtractAbstractMembersFromType(
        INamedTypeSymbol type,
        ImmutableArray<AbstractMemberModel>.Builder members,
        System.Collections.Generic.HashSet<string> processedSignatures,
        System.Collections.Generic.HashSet<string> processedPropertyNames)
    {
        // Walk up the inheritance chain
        var current = type;
        while (current != null)
        {
            // Only process class types (not interfaces)
            if (current.TypeKind != TypeKind.Class)
            {
                current = current.BaseType;
                continue;
            }

            foreach (var member in current.GetMembers())
            {
                if (member is IMethodSymbol method &&
                    method.IsAbstract &&
                    method.MethodKind == MethodKind.Ordinary &&
                    !method.IsStatic)
                {
                    // Include generic methods - we'll generate stubs with type parameters
                    var signature = GetMethodSignature(method);
                    if (processedSignatures.Add(signature))
                    {
                        // All abstract members from base class need 'override'
                        members.Add(CreateAbstractMemberModel(method, isInterfaceImplementation: false));
                    }
                }
                else if (member is IPropertySymbol property &&
                         property.IsAbstract &&
                         !property.IsStatic)
                {
                    // Skip properties with unresolved generic type parameters (properties can't be generic)
                    if (HasUnresolvedTypeParameters(property.Type))
                    {
                        continue;
                    }

                    // Track property name for duplicate detection in interface members later
                    processedPropertyNames.Add(property.Name);

                    // Include return type in signature to distinguish properties with same name but different types
                    var signature = $"prop:{property.Type.ToDisplayString()}:{property.Name}";
                    if (processedSignatures.Add(signature))
                    {
                        // All abstract properties from base class need 'override'
                        members.Add(CreateAbstractMemberModel(property, isInterfaceImplementation: false));
                    }
                }
            }

            current = current.BaseType;
        }
    }

    /// <summary>
    /// Checks if a type contains unresolved generic type parameters.
    /// </summary>
    private static bool HasUnresolvedTypeParameters(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
            return true;

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var arg in namedType.TypeArguments)
            {
                if (HasUnresolvedTypeParameters(arg))
                    return true;
            }
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return HasUnresolvedTypeParameters(arrayType.ElementType);
        }

        return false;
    }

    private static void ExtractInterfaceMembersToImplement(
        INamedTypeSymbol interfaceType,
        ITypeSymbol? baseType,
        ImmutableArray<AbstractMemberModel>.Builder members,
        System.Collections.Generic.HashSet<string> processedSignatures)
    {
        // Get all interfaces including inherited ones
        var allInterfaces = interfaceType.AllInterfaces.Prepend(interfaceType);

        foreach (var iface in allInterfaces)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member is IMethodSymbol method &&
                    method.MethodKind == MethodKind.Ordinary &&
                    !method.IsStatic)
                {
                    // Skip members with unresolved generic type parameters
                    if (HasUnresolvedTypeParameters(method.ReturnType) ||
                        method.Parameters.Any(p => HasUnresolvedTypeParameters(p.Type)))
                    {
                        continue;
                    }

                    // Check if base type already implements this
                    if (baseType != null && IsMemberImplementedByType(baseType, method))
                        continue;

                    var signature = GetMethodSignature(method);
                    if (processedSignatures.Add(signature))
                    {
                        members.Add(CreateAbstractMemberModel(method, isInterfaceImplementation: true));
                    }
                }
                else if (member is IPropertySymbol property &&
                         !property.IsStatic)
                {
                    // Skip properties with unresolved generic type parameters
                    if (HasUnresolvedTypeParameters(property.Type))
                    {
                        continue;
                    }

                    // Check if base type already implements this
                    if (baseType != null && IsMemberImplementedByType(baseType, property))
                        continue;

                    // Include return type in signature to distinguish properties with same name but different types
                    var signature = $"prop:{property.Type.ToDisplayString()}:{property.Name}";
                    if (processedSignatures.Add(signature))
                    {
                        members.Add(CreateAbstractMemberModel(property, isInterfaceImplementation: true));
                    }
                }
            }
        }
    }

    private static bool IsMemberImplementedByType(ITypeSymbol type, ISymbol interfaceMember)
    {
        var implementation = type.FindImplementationForInterfaceMember(interfaceMember);
        if (implementation == null)
            return false;

        // Check if the implementation is abstract (needs override in Empty)
        if (implementation is IMethodSymbol method && method.IsAbstract)
            return false;
        if (implementation is IPropertySymbol property && property.IsAbstract)
            return false;

        return true;
    }

    private static string GetMethodSignature(IMethodSymbol method)
    {
        var paramTypes = string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"method:{method.Name}({paramTypes})";
    }

    private static AbstractMemberModel CreateAbstractMemberModel(IMethodSymbol method, bool isInterfaceImplementation = false, string? explicitInterfaceType = null)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = method.Parameters.Select(p => new ParameterModel(
            Name: p.Name,
            Type: p.Type.ToDisplayString(),
            HasDefaultValue: p.HasExplicitDefaultValue,
            DefaultValue: p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() ?? "default" : string.Empty
        )).ToImmutableArray();

        // Capture method-level type parameters
        var typeParameters = method.TypeParameters
            .Select(tp => tp.Name)
            .ToImmutableArray();

        // Find parameter that matches return type (for identity transforms)
        string? matchingParam = null;
        foreach (var param in method.Parameters)
        {
            if (SymbolEqualityComparer.Default.Equals(param.Type, method.ReturnType))
            {
                matchingParam = param.Name;
                break;
            }
        }

        return new AbstractMemberModel(
            Name: method.Name,
            ReturnType: returnType,
            IsProperty: false,
            IsMethod: true,
            HasGetter: false,
            HasSetter: false,
            Parameters: parameters,
            MatchingParameterName: matchingParam,
            IsOverride: !isInterfaceImplementation, // Override for abstract base members, not for interface-only
            ExplicitInterfaceType: explicitInterfaceType,
            TypeParameters: typeParameters
        );
    }

    private static AbstractMemberModel CreateAbstractMemberModel(IPropertySymbol property, bool isInterfaceImplementation = false, string? explicitInterfaceType = null)
    {
        return new AbstractMemberModel(
            Name: property.Name,
            ReturnType: property.Type.ToDisplayString(),
            IsProperty: true,
            IsMethod: false,
            HasGetter: property.GetMethod != null,
            HasSetter: property.SetMethod != null,
            Parameters: ImmutableArray<ParameterModel>.Empty,
            MatchingParameterName: null,
            IsOverride: !isInterfaceImplementation, // Override for abstract base members, not for interface-only
            ExplicitInterfaceType: explicitInterfaceType,
            TypeParameters: ImmutableArray<string>.Empty
        );
    }

    /// <summary>
    /// Generates the NotFound sentinel class for ServiceTypes.
    /// </summary>
    public static void GenerateNotFoundSentinelForServiceType(
        StringBuilder sb,
        ServiceTypeCollectionModel collection,
        ImmutableArray<AbstractMemberModel> abstractMembers,
        string? interfaceTypeName = null,
        HashSet<string>? userDeclaredMembers = null)
    {
        var baseArgs = GenerateDefaultArguments(collection.BaseConstructorParameters);

        sb.AppendLine($"        private partial class NotFound{collection.ClassName} : {collection.BaseTypeName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            public NotFound{collection.ClassName}() : base({baseArgs}) {{ }}");

        // Generate stubs for abstract members
        if (abstractMembers.Length > 0)
        {
            GenerateAbstractMemberStubs(sb, abstractMembers, interfaceTypeName, userDeclaredMembers);
        }

        sb.AppendLine("        }");
    }

    /// <summary>
    /// Resolves an unbound generic type to its closed form from the class's base type.
    /// </summary>
    public static string ResolveClosedGenericType(
        ITypeSymbol attributeType,
        INamedTypeSymbol classSymbol,
        int typeArgumentIndex,
        string baseClassName)
    {
        // If not an unbound generic, return as-is
        if (attributeType is not INamedTypeSymbol namedType || !namedType.IsUnboundGenericType)
        {
            return attributeType.ToDisplayString();
        }

        // Find the base class in the inheritance chain
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.Name.StartsWith(baseClassName, StringComparison.Ordinal) &&
                current.TypeArguments.Length > typeArgumentIndex)
            {
                return current.TypeArguments[typeArgumentIndex].ToDisplayString();
            }

            current = current.BaseType;
        }

        // Fallback to original if we can't resolve
        return attributeType.ToDisplayString();
    }

    /// <summary>
    /// Gets a type symbol from a full type name, constructing closed generic types when needed.
    /// </summary>
    public static ITypeSymbol? GetTypeByFullName(Compilation compilation, string fullName)
    {
        // Handle generic types like "Foo<Bar, Baz>"
        var genericStart = fullName.IndexOf('<');
        if (genericStart < 0)
        {
            return compilation.GetTypeByMetadataName(fullName);
        }

        // For generic types, construct the closed generic type
        var baseName = fullName.Substring(0, genericStart);

        // Count arity by counting commas only at depth 0 (top level, not inside nested generics)
        var argsStart = genericStart + 1;
        var argsEnd = fullName.LastIndexOf('>');
        var argsString = (argsEnd > argsStart) ? fullName.Substring(argsStart, argsEnd - argsStart) : string.Empty;
        var arity = CountTopLevelArguments(argsString);

        var openType = compilation.GetTypeByMetadataName($"{baseName}`{arity}");

        if (openType == null)
            return null;

        // Use already-parsed argsString for type argument names
        if (argsEnd <= argsStart)
            return openType;

        var typeArgNames = ParseTypeArguments(argsString);

        if (typeArgNames.Length != arity)
            return openType;

        // Look up each type argument
        var typeArgs = new ITypeSymbol[arity];
        for (int i = 0; i < arity; i++)
        {
            var argType = GetWellKnownType(compilation, typeArgNames[i])
                ?? compilation.GetTypeByMetadataName(typeArgNames[i])
                ?? GetTypeByFullName(compilation, typeArgNames[i]);

            if (argType == null)
                return openType; // Can't resolve, return open type

            typeArgs[i] = argType;
        }

        // Construct the closed generic type
        return openType.Construct(typeArgs);
    }

    private static string[] ParseTypeArguments(string argsString)
    {
        var result = new System.Collections.Generic.List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < argsString.Length; i++)
        {
            var c = argsString[i];
            if (c == '<') depth++;
            else if (c == '>') depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(argsString.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        result.Add(argsString.Substring(start).Trim());
        return result.ToArray();
    }

    /// <summary>
    /// Counts the number of top-level type arguments (commas at depth 0 + 1).
    /// Handles nested generics like "Foo&lt;A, B&gt;, C" correctly as 2 args.
    /// </summary>
    private static int CountTopLevelArguments(string argsString)
    {
        if (string.IsNullOrWhiteSpace(argsString))
            return 0;

        var count = 1; // At least one argument if not empty
        var depth = 0;

        foreach (var c in argsString)
        {
            if (c == '<') depth++;
            else if (c == '>') depth--;
            else if (c == ',' && depth == 0) count++;
        }

        return count;
    }

    // Exhaustive well-known type lookup for Roslyn compilation
#pragma warning disable FDW006, FDW007
    private static ITypeSymbol? GetWellKnownType(Compilation compilation, string typeName)
    {
        return typeName switch
        {
            "object" => compilation.GetSpecialType(SpecialType.System_Object),
            "string" => compilation.GetSpecialType(SpecialType.System_String),
            "int" => compilation.GetSpecialType(SpecialType.System_Int32),
            "long" => compilation.GetSpecialType(SpecialType.System_Int64),
            "bool" => compilation.GetSpecialType(SpecialType.System_Boolean),
            "void" => compilation.GetSpecialType(SpecialType.System_Void),
            "byte" => compilation.GetSpecialType(SpecialType.System_Byte),
            "short" => compilation.GetSpecialType(SpecialType.System_Int16),
            "float" => compilation.GetSpecialType(SpecialType.System_Single),
            "double" => compilation.GetSpecialType(SpecialType.System_Double),
            "decimal" => compilation.GetSpecialType(SpecialType.System_Decimal),
            "char" => compilation.GetSpecialType(SpecialType.System_Char),
            _ => null
        };
    }
#pragma warning restore FDW006, FDW007
}
