using System;
using System.Globalization;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Models;

namespace Fdw.SourceGenerators.Generators;

/// <summary>
/// Generates lookup methods for collection classes (Name, Id, etc.).
/// Uses conditional compilation for NET8+ vs netstandard2.0.
/// </summary>
public sealed class LookupMethodGenerator
{
    private readonly CollectionBuilderConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupMethodGenerator"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public LookupMethodGenerator(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Generates dynamic lookup methods based on [TypeLookup] attributes.
    /// Creates methods like Name(string name) and Id(TKey id).
    /// Uses separate dictionaries for each lookup property.
    /// </summary>
    public static IMethodBuilder[] GenerateDynamicLookupMethods<TId>(
        GenericTypeInfoModel<TId> definition,
        string returnType)
        where TId : struct
    {
        if (definition?.LookupProperties == null)
            return Array.Empty<IMethodBuilder>();

        // Get key type from definition (defaults to "int" if not specified)
        var keyType = definition.KeyType ?? "int";

        var methods = definition.LookupProperties
            .Select(lookup => GenerateLookupMethod(lookup, returnType, definition.TargetFramework, keyType))
            .ToArray();

        return methods;
    }

    private static IMethodBuilder GenerateLookupMethod(
        PropertyLookupInfoModel lookup,
        string returnType,
        string? targetFramework,
        string keyType)
    {
        // Use the custom lookup method name from the attribute if specified, otherwise use property name
        var methodName = lookup.LookupMethodName ?? lookup.PropertyName;

        // Parameter name should be based on the property name, not the method name
        // For [TypeLookup("ById")] on property "Id", parameter should be "id" not "byid"
        var parameterName = lookup.PropertyName.ToLower(CultureInfo.InvariantCulture);

        string methodBody;
        bool useExpressionBody;

        // Check if this is the Id property with the primary key type
        if (string.Equals(lookup.PropertyType, keyType, StringComparison.Ordinal) &&
            string.Equals(lookup.PropertyName, "Id", StringComparison.Ordinal))
        {
            // For ID lookups, use the primary key directly
            methodBody = $"_all.TryGetValue({parameterName}, out var result) ? result : _empty";
            useExpressionBody = true;
        }
        else
        {
            // For non-ID lookups, use separate dictionary
            var dictionaryName = $"_by{lookup.PropertyName}";
            methodBody = $"{dictionaryName}.TryGetValue({parameterName}, out var result) ? result : _empty";
            useExpressionBody = true;
        }

        var method = new MethodBuilder()
            .WithName(methodName)
            .WithReturnType(returnType)
            .WithAccessModifier("public")
            .AsStatic()
            .WithParameter(lookup.PropertyType, parameterName)
            .WithXmlDoc($"Gets a type option by its {lookup.PropertyName} using {(string.Equals(lookup.PropertyName, "Id", StringComparison.Ordinal) ? "primary key lookup" : "secondary lookup dictionary")}.")
            .WithParamDoc(parameterName, $"The {lookup.PropertyName} value to search for.")
            .WithReturnDoc($"The type option with the specified {lookup.PropertyName}, or empty instance if not found.");

        if (useExpressionBody)
        {
            method.WithExpressionBody(methodBody);
        }
        else
        {
            method.WithBody(methodBody);
        }

        return method;
    }
}
