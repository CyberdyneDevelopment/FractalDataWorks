using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Generators;
using Fdw.SourceGenerators.Models;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Builders;

/// <summary>
/// Orchestrates the generation of collection classes using specialized generators.
/// This class follows the Single Responsibility Principle by delegating to focused generators.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public sealed class GenericCollectionBuilder<TId> : IGenericCollectionBuilder<TId>
    where TId : struct
{
    private readonly CollectionBuilderConfiguration _config;
    private readonly FieldGenerator<TId> _fieldGenerator;
    private readonly LookupMethodGenerator _lookupMethodGenerator;
    private readonly EmptyClassGenerator _emptyClassGenerator;
    private readonly StaticConstructorGenerator<TId> _staticConstructorGenerator;
    private readonly ValuePropertyGenerator<TId> _valuePropertyGenerator;

    private CollectionGenerationMode _mode;
    private GenericTypeInfoModel<TId>? _definition;
    private IList<GenericValueInfoModel<TId>>? _values;
    private string? _returnType;
    private Compilation? _compilation;
    private bool _isUserClassStatic;
    private bool _isUserClassAbstract;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericCollectionBuilder{TId}"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public GenericCollectionBuilder(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        // Initialize generators
        _fieldGenerator = new FieldGenerator<TId>(config);
        _lookupMethodGenerator = new LookupMethodGenerator(config);
        _emptyClassGenerator = new EmptyClassGenerator(config);
        _staticConstructorGenerator = new StaticConstructorGenerator<TId>(config);
        _valuePropertyGenerator = new ValuePropertyGenerator<TId>(config);
    }

    /// <summary>
    /// Configures the generation mode for the collection.
    /// </summary>
    /// <param name="mode">The generation mode to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IGenericCollectionBuilder<TId> Configure(CollectionGenerationMode mode)
    {
        _mode = mode;
        return this;
    }

    /// <summary>
    /// Sets the type definition for the collection.
    /// </summary>
    /// <param name="definition">The type definition containing collection metadata.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IGenericCollectionBuilder<TId> WithDefinition(GenericTypeInfoModel<TId> definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        return this;
    }

    /// <summary>
    /// Sets the collection values (options/implementations) to include.
    /// </summary>
    /// <param name="values">The list of value types to include in the collection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IGenericCollectionBuilder<TId> WithValues(IList<GenericValueInfoModel<TId>> values)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        return this;
    }

    /// <summary>
    /// Sets the return type for collection methods.
    /// </summary>
    /// <param name="returnType">The fully qualified return type name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IGenericCollectionBuilder<TId> WithReturnType(string returnType)
    {
        if (string.IsNullOrEmpty(returnType))
            throw new ArgumentException("Return type cannot be null or empty.", nameof(returnType));

        _returnType = returnType;
        return this;
    }

    /// <summary>
    /// Sets the compilation context for type resolution.
    /// </summary>
    /// <param name="compilation">The Roslyn compilation context.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IGenericCollectionBuilder<TId> WithCompilation(Compilation compilation)
    {
        _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        return this;
    }

    /// <summary>
    /// Sets the modifiers from the user's declared partial class.
    /// The generated partial must match what the user declared.
    /// </summary>
    public IGenericCollectionBuilder<TId> WithUserClassModifiers(bool isStatic, bool isAbstract)
    {
        _isUserClassStatic = isStatic;
        _isUserClassAbstract = isAbstract;
        return this;
    }

    /// <summary>
    /// Builds the complete collection class source code.
    /// </summary>
    /// <returns>The generated C# source code for the collection class.</returns>
#pragma warning disable MA0051 // Sequential code generation orchestration calling specialized generators
    public string Build()
    {
        ValidateConfiguration();

        var classBuilder = new ClassBuilder()
            .WithNamespace(_definition!.Namespace)
            .WithName(_definition.CollectionName)
            .WithAccessModifier("public")
            .AsPartial();

        // Match the user's declared class modifiers
        // The generated partial must match what the user declared
        if (_isUserClassStatic)
        {
            classBuilder.AsStatic();
        }

        // Note: We don't add abstract to the generated partial even if the user's class is abstract
        // because you can't have a partial class where one part is abstract and the other isn't


        // Collect unique namespaces from all discovered types
        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System",
            "System.Collections.Generic",
            "System.Collections.Frozen",
            "System.Linq"
        };

        // Add namespace for the return type (e.g., ITransformationType, ISecretManagerType)
        if (!string.IsNullOrEmpty(_returnType))
        {
            var lastDotIndex = _returnType!.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                var returnTypeNamespace = _returnType.Substring(0, lastDotIndex);
                // Don't add the collection's own namespace (already in context)
                if (!string.Equals(returnTypeNamespace, _definition!.Namespace, StringComparison.Ordinal))
                {
                    namespaces.Add(returnTypeNamespace);
                }
            }
        }

        // Add namespace for the collection base type
        if (!string.IsNullOrEmpty(_definition!.CollectionBaseType))
        {
            var lastDotIndex = _definition.CollectionBaseType!.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                var baseTypeNamespace = _definition.CollectionBaseType.Substring(0, lastDotIndex);
                // Don't add the collection's own namespace (already in context)
                if (!string.Equals(baseTypeNamespace, _definition.Namespace, StringComparison.Ordinal))
                {
                    namespaces.Add(baseTypeNamespace);
                }
            }
        }

        // Extract namespaces from all value types
        foreach (var value in _values!.Where(v => v.Include && !v.IsAbstract && !v.IsStatic))
        {
            if (!string.IsNullOrEmpty(value.FullTypeName))
            {
                var lastDotIndex = value.FullTypeName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var typeNamespace = value.FullTypeName.Substring(0, lastDotIndex);
                    // Don't add the collection's own namespace (already in context)
                    if (!string.Equals(typeNamespace, _definition!.Namespace, StringComparison.Ordinal))
                    {
                        namespaces.Add(typeNamespace);
                    }
                }
            }
        }

        // Add strategy-specific namespaces
#pragma warning disable FDW018
        switch (_definition!.CollectionStrategy)
        {
            case CollectionStrategy.Immutable:
                namespaces.Add("System.Collections.Frozen");
                break;
            case CollectionStrategy.Mutable:
                namespaces.Add("System.Collections.Concurrent");
                break;
            case CollectionStrategy.Factory:
                // Dictionary is in System.Collections.Generic, already included by default
                break;
        }
#pragma warning restore FDW018

        // Add any additional required namespaces from the definition
        // This includes namespaces for generic type arguments that may not be detected by string parsing
        if (_definition.RequiredNamespaces != null)
        {
            foreach (var requiredNs in _definition.RequiredNamespaces)
            {
                if (!string.IsNullOrEmpty(requiredNs) &&
                    !string.Equals(requiredNs, _definition.Namespace, StringComparison.Ordinal))
                {
                    namespaces.Add(requiredNs);
                }
            }
        }

        // Add using directives
        classBuilder.WithUsings(namespaces.ToArray());

        // Get key type from definition (defaults to "int" if not specified)
        var keyType = _definition!.KeyType ?? "int";

        // Generate fields using FieldGenerator
        var allField = FieldGenerator<TId>.GenerateAllField(keyType, _returnType!, _definition!.CollectionStrategy);
        classBuilder.WithField(allField);

        // Always generate _empty field (initialized in static constructor)
        var emptyField = FieldGenerator<TId>.GenerateEmptyField(_returnType!);
        classBuilder.WithField(emptyField);

        // Generate lookup dictionary fields (with conditional compilation)
        var lookupFields = FieldGenerator<TId>.GenerateLookupDictionaryFields(_definition!, _returnType!);
        foreach (var field in lookupFields)
        {
            classBuilder.WithField(field);
        }

        // Generate lookup methods using LookupMethodGenerator
        var lookupMethods = LookupMethodGenerator.GenerateDynamicLookupMethods<TId>(_definition!, _returnType!);
        foreach (var method in lookupMethods)
        {
            classBuilder.WithMethod(method);
        }

        // Generate All() method
        var allMethod = new MethodBuilder()
            .WithName("All")
            .WithReturnType($"IReadOnlyList<{_returnType}>")
            .WithAccessModifier("public")
            .AsStatic()
            .WithExpressionBody("_all.Values.ToList()")
            .WithXmlDoc("Gets all collection items.")
            .WithReturnDoc("A read-only list of all items in the collection.");

        classBuilder.WithMethod(allMethod);

        // Generate NotFound() method
        var notFoundMethod = new MethodBuilder()
            .WithName("NotFound")
            .WithReturnType(_returnType!)
            .WithAccessModifier("public")
            .AsStatic()
            .WithExpressionBody("_empty")
            .WithXmlDoc("Gets the empty/not-found instance for this collection.")
            .WithReturnDoc("The empty instance representing a not-found value.");

        classBuilder.WithMethod(notFoundMethod);

        // Generate Register() method for Mutable and Factory strategies
        if (_definition!.CollectionStrategy == CollectionStrategy.Mutable ||
            _definition!.CollectionStrategy == CollectionStrategy.Factory)
        {
            var registerBody = new StringBuilder();
            registerBody.AppendLine("if (item == null) throw new ArgumentNullException(nameof(item));");
            registerBody.AppendLine($"_all.TryAdd(item.Id, item);");

            // Also add to lookup dictionaries if they exist
            if (_definition!.LookupProperties != null && _definition!.LookupProperties.Length > 0)
            {
                var nonIdLookups = _definition!.LookupProperties
                    .Where(l => !string.Equals(l.PropertyName, "Id", StringComparison.Ordinal) ||
                               !string.Equals(l.PropertyType, keyType, StringComparison.Ordinal))
                    .ToList();

                foreach (var lookup in nonIdLookups)
                {
                    registerBody.AppendLine($"_by{lookup.PropertyName}.TryAdd(item.{lookup.PropertyName}, item);");
                }
            }

            // Strip namespace from return type if it matches the definition's namespace
            var registerParamType = _returnType!;
            if (registerParamType.StartsWith(_definition!.Namespace + ".", StringComparison.Ordinal))
            {
                registerParamType = registerParamType.Substring(_definition!.Namespace.Length + 1);
            }

            var registerMethod = new MethodBuilder()
                .WithName("Register")
                .WithReturnType("void")
                .WithAccessModifier("public")
                .AsStatic()
                .WithParameter(registerParamType, "item")
                .WithBody(registerBody.ToString())
                .WithXmlDoc("Registers a new item in the collection at runtime.")
                .WithParamDoc("item", "The item to register.");

            classBuilder.WithMethod(registerMethod);
        }

        // Generate static properties for each value
        var valueProperties = ValuePropertyGenerator<TId>.GenerateValueProperties(
            _values!,
            _returnType!,
            _definition!.UseMethods);
        foreach (var property in valueProperties)
        {
            classBuilder.WithProperty(property);
        }

        // Generate static constructor
        var staticConstructor = StaticConstructorGenerator<TId>.GenerateStaticConstructor(
            _definition!,
            _values!,
            _returnType!,
            _compilation!);
        classBuilder.WithConstructor(staticConstructor);

        // Build the class
        return classBuilder.Build();
    }
#pragma warning restore MA0051

    /// <summary>
    /// Gets the generated Empty class code.
    /// </summary>
#pragma warning disable MA0051 // Sequential string building for generated empty class code
    public string GetEmptyClassCode()
    {
        if (_definition == null || _compilation == null)
            return string.Empty;

        // Use ClassName which contains the base type from TypeCollectionAttribute first parameter
        // This can be a class like "ConnectionStateBase" or an interface like "IAuthenticationCommand"
        var baseTypeName = _definition.ClassName;
        if (string.IsNullOrEmpty(baseTypeName))
            return string.Empty;

        // For generic types, FullTypeName will have the format "Namespace.Type<T1,T2>"
        // We need to convert this to metadata format "Namespace.Type`2" for lookup
        INamedTypeSymbol? baseTypeSymbol = null;

        // Try using FullTypeName first (handles both generic and non-generic)
        if (!string.IsNullOrEmpty(_definition.FullTypeName))
        {
            var fullTypeName = _definition.FullTypeName;

            // Check if it's a generic type (contains '<')
            var genericIndex = fullTypeName.IndexOf('<');
            if (genericIndex > 0)
            {
                // Extract the base name without type parameters
                var baseName = fullTypeName.Substring(0, genericIndex);

                // Count type parameters to get arity
                var typeParamSection = fullTypeName.Substring(genericIndex);
                var commaCount = typeParamSection.Count(c => c == ',');
                var arity = commaCount + 1; // Number of commas + 1 = number of parameters

                // Construct metadata name: "Namespace.TypeName`Arity"
                var metadataName = $"{baseName}`{arity}";
                baseTypeSymbol = _compilation.GetTypeByMetadataName(metadataName);
            }
            else
            {
                // Non-generic type - use as-is
                baseTypeSymbol = _compilation.GetTypeByMetadataName(fullTypeName);
            }
        }

        // Fallback: try combining namespace with ClassName
        if (baseTypeSymbol == null)
        {
            var fullyQualifiedTypeName = baseTypeName.Contains(".")
                ? baseTypeName
                : $"{_definition.Namespace}.{baseTypeName}";

            baseTypeSymbol = _compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
        }

        // Check if the base type is actually a class (not an interface)
        // If it's an interface or not found, skip Empty class generation
        if (baseTypeSymbol == null || baseTypeSymbol.TypeKind != Microsoft.CodeAnalysis.TypeKind.Class)
            return string.Empty;

        // For generic base types, skip Empty class generation
        // We cannot instantiate an open generic type, so _empty will be null!
        if (GenericTypeHelper.IsGenericType(baseTypeSymbol))
            return string.Empty;

        return EmptyClassGenerator.GenerateEmptyClass(
            _definition,
            baseTypeName,
            _definition.Namespace,
            _compilation);
    }
#pragma warning restore MA0051

    private void ValidateConfiguration()
    {
        if (_definition == null)
            throw new InvalidOperationException("Definition must be set before building.");

        if (_values == null)
            throw new InvalidOperationException("Values must be set before building.");

        if (string.IsNullOrEmpty(_returnType))
            throw new InvalidOperationException("Return type must be set before building.");

        if (_compilation == null)
            throw new InvalidOperationException("Compilation must be set before building.");
    }
}
