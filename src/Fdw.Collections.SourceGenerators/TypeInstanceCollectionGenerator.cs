using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Fdw.Collections.SourceGenerators.Shared;
using Fdw.Conventions;

namespace Fdw.Collections.SourceGenerators;

/// <summary>
/// Generator for factory-based TypeCollections that create new instances.
/// </summary>
[Generator]
public class TypeInstanceCollectionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Fdw.Collections.Attributes.TypeInstanceCollectionAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover [TypeInstanceCollection] classes
        var collectionsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: ExtractCollectionModel)
            .Where(static m => m != null)
            .Select(static (m, _) => m!.Value);

        // Discover all TypeOptions
        var optionsProvider = context.CompilationProvider
            .Combine(collectionsProvider.Collect())
            .Select(static (pair, _) =>
            {
                var (compilation, collections) = pair;
                var restrictToCurrentCompilation = collections.Any(c => c.RestrictToCurrentCompilation);
                return TypeOptionDiscovery.DiscoverAll(compilation, restrictToCurrentCompilation);
            });

        // Combine collections, options, and compilation for abstract member extraction
        var combinedProvider = collectionsProvider.Collect()
            .Combine(optionsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combinedProvider, Execute);
    }

    private static TypeCollectionModel? ExtractCollectionModel(
        GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var classSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes[0];

        if (attribute.ConstructorArguments.Length < 3)
            return null;

        var baseType = attribute.ConstructorArguments[0].Value as ITypeSymbol;
        var interfaceType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
        var collectionType = attribute.ConstructorArguments[2].Value as ITypeSymbol;

        if (baseType == null || interfaceType == null || collectionType == null)
            return null;

        // Resolve unbound generic types from the class's base type
        var resolvedBaseType = CodeGeneration.ResolveClosedGenericType(baseType, classSymbol, 0, "TypeInstanceCollectionBase");
        var resolvedInterfaceType = CodeGeneration.ResolveClosedGenericType(interfaceType, classSymbol, 1, "TypeInstanceCollectionBase");

        // Extract optional parent collection and name (constructor args 3 and 4)
        var parentCollection = attribute.ConstructorArguments.Length > 3
            ? attribute.ConstructorArguments[3].Value as ITypeSymbol
            : null;
        var childName = attribute.ConstructorArguments.Length > 4
            ? attribute.ConstructorArguments[4].Value?.ToString()
            : null;

        var restrictToCurrentCompilation = attribute.NamedArguments
            .FirstOrDefault(kvp => string.Equals(kvp.Key, "RestrictToCurrentCompilation", StringComparison.Ordinal))
            .Value.Value as bool? ?? false;

        var baseConstructorParams = ExtractBaseConstructorParameters(baseType, classSymbol);

        return new TypeCollectionModel(
            ClassName: classSymbol.Name,
            Namespace: classSymbol.ContainingNamespace.ToDisplayString(),
            FullName: classSymbol.ToDisplayString(),
            BaseTypeName: resolvedBaseType,
            InterfaceTypeName: resolvedInterfaceType,
            MatchKey: TypeOptionDiscovery.GetMatchKey(collectionType),
            Kind: CollectionKind.Factory,
            RestrictToCurrentCompilation: restrictToCurrentCompilation,
            ParentCollectionMatchKey: parentCollection != null ? TypeOptionDiscovery.GetMatchKey(parentCollection) : null,
            ChildName: childName,
            BaseConstructorParameters: baseConstructorParams
        );
    }

    private static ImmutableArray<ParameterModel> ExtractBaseConstructorParameters(ITypeSymbol baseType, INamedTypeSymbol classSymbol)
    {
        // Try to get the resolved base type from the class's inheritance chain
        INamedTypeSymbol? resolvedBaseType = null;

        // If baseType is unbound generic, get the closed version from class's base type
        if (baseType is INamedTypeSymbol namedBaseType && namedBaseType.IsUnboundGenericType)
        {
            var current = classSymbol.BaseType;
            while (current != null)
            {
                if (current.Name.StartsWith("TypeInstanceCollectionBase", StringComparison.Ordinal) &&
                    current.TypeArguments.Length > 0)
                {
                    resolvedBaseType = current.TypeArguments[0] as INamedTypeSymbol;
                    break;
                }
                current = current.BaseType;
            }
        }
        else
        {
            resolvedBaseType = baseType as INamedTypeSymbol;
        }

        if (resolvedBaseType == null)
            return ImmutableArray<ParameterModel>.Empty;

        // Find the protected constructor with fewest parameters (prefer parameterless for Empty sentinel)
        var ctor = resolvedBaseType.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Protected)
            .OrderBy(c => c.Parameters.Length)
            .FirstOrDefault();

        if (ctor == null)
            return ImmutableArray<ParameterModel>.Empty;

        return ctor.Parameters
            .Select(p => new ParameterModel(
                Name: p.Name,
                Type: p.Type.ToDisplayString(),
                HasDefaultValue: p.HasExplicitDefaultValue,
                DefaultValue: string.Empty
            ))
            .ToImmutableArray();
    }

    private static void Execute(
        SourceProductionContext context,
        ((ImmutableArray<TypeCollectionModel> Collections, ImmutableArray<TypeOptionModel> Options) Data, Compilation Compilation) source)
    {
        var (collections, allOptions) = source.Data;
        var compilation = source.Compilation;

        foreach (var collection in collections)
        {
            var options = allOptions
                .Where(o => string.Equals(o.CollectionMatchKey, collection.MatchKey, StringComparison.Ordinal))
                .ToImmutableArray();

            // Find child collections for this parent
            var childCollections = collections
                .Where(c => string.Equals(c.ParentCollectionMatchKey, collection.MatchKey, StringComparison.Ordinal) && c.ChildName != null)
                .Select(c => new ChildCollectionModel(c.ChildName!, c.FullName, c.ClassName))
                .ToImmutableArray();

            // Validate (but don't error on empty - that's expected for abstractions packages)
            ValidateOptions(context, collection, options);

            // Look up the type symbols to extract abstract members
            var baseTypeSymbol = compilation.GetTypeByMetadataName(collection.BaseTypeName)
                ?? CodeGeneration.GetTypeByFullName(compilation, collection.BaseTypeName);
            var interfaceTypeSymbol = compilation.GetTypeByMetadataName(collection.InterfaceTypeName)
                ?? CodeGeneration.GetTypeByFullName(compilation, collection.InterfaceTypeName);

            var abstractMembers = CodeGeneration.ExtractAbstractMembers(baseTypeSymbol, interfaceTypeSymbol);

            // Generate
            var code = GenerateCode(collection, options, childCollections, abstractMembers);
            context.AddSource(
                $"{collection.ClassName}.TypeInstanceCollection.g.cs",
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private static void ValidateOptions(
        SourceProductionContext context,
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options)
    {
        // Check for Id collisions
        var idGroups = options.GroupBy(o => o.GeneratedId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var collision in idGroups)
        {
            var types = string.Join(", ", collision.Select(o => o.FullTypeName));
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.IdHashCollision,
                Location.None,
                collection.ClassName,
                collision.Key,
                types));
        }

        // Check for name duplicates
        var nameGroups = options.GroupBy(o => o.OptionName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var duplicate in nameGroups)
        {
            var types = string.Join(", ", duplicate.Select(o => o.FullTypeName));
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.DuplicateOptionName,
                Location.None,
                collection.ClassName,
                duplicate.Key,
                types));
        }
    }

#pragma warning disable MA0051 // Source generator emits complete TypeInstanceCollection class — splitting scatters the template
    [ConventionOverride(MaxMethodLines = 127)]  // Code generation template for TypeInstanceCollection — sequential string building
    private static string GenerateCode(
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options,
        ImmutableArray<ChildCollectionModel> childCollections,
        ImmutableArray<AbstractMemberModel> abstractMembers)
    {
        var sb = new StringBuilder();

        CodeGeneration.GenerateUsings(sb, CollectionKind.Factory);

        sb.AppendLine($"namespace {collection.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {collection.ClassName}");
        sb.AppendLine("    {");

        // Factory dictionaries
        sb.AppendLine($"        private static readonly Dictionary<int, Func<{collection.InterfaceTypeName}>> _factoriesById = new();");
        sb.AppendLine($"        private static readonly Dictionary<string, Func<{collection.InterfaceTypeName}>> _factoriesByName = new();");
        sb.AppendLine($"        private static readonly HashSet<string> _registeredTypeNames = new();");
        sb.AppendLine();

        // Static constructor - register compile-time factories
        sb.AppendLine($"        static {collection.ClassName}()");
        sb.AppendLine("        {");
        foreach (var option in options)
        {
            sb.AppendLine($"            RegisterFactory({option.GeneratedId}, \"{option.OptionName}\", () => new {option.FullTypeName}());");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // RegisterFactory method (internal)
        sb.AppendLine($"        private static void RegisterFactory(int id, string name, Func<{collection.InterfaceTypeName}> factory)");
        sb.AppendLine("        {");
        sb.AppendLine("            _factoriesById[id] = factory;");
        sb.AppendLine("            _factoriesByName[name] = factory;");
        sb.AppendLine("            _registeredTypeNames.Add(name);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Register method (public - for runtime additions)
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Registers a factory function for creating instances at runtime.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static void Register<T>(int id, string name) where T : {collection.InterfaceTypeName}, new()");
        sb.AppendLine("        {");
        sb.AppendLine("            RegisterFactory(id, name, () => new T());");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Register with custom factory
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Registers a custom factory function for creating instances.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static void Register(int id, string name, Func<{collection.InterfaceTypeName}> factory)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (factory == null) throw new ArgumentNullException(nameof(factory));");
        sb.AppendLine("            RegisterFactory(id, name, factory);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Static factory methods (create new instances)
        foreach (var option in options)
        {
            // Parameterless factory method
            sb.AppendLine($"        /// <summary>Creates a new instance of {option.TypeName}.</summary>");
            sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}() =>");
            sb.AppendLine($"            new {option.FullTypeName}();");
            sb.AppendLine();

            // Method overloads for parameterized constructors
            foreach (var ctor in option.Constructors.Where(c => c.HasParameters))
            {
                var parameters = string.Join(", ",
                    ctor.Parameters.Select(p =>
                        p.HasDefaultValue
                            ? $"{p.Type} {p.Name} = {p.DefaultValue}"
                            : $"{p.Type} {p.Name}"));

                var arguments = string.Join(", ",
                    ctor.Parameters.Select(p => p.Name));

                sb.AppendLine($"        /// <summary>Creates a new instance of {option.TypeName} with parameters.</summary>");
                sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}({parameters}) =>");
                sb.AppendLine($"            new {option.FullTypeName}({arguments});");
                sb.AppendLine();
            }
        }

        // CreateById method
        sb.AppendLine($"        /// <summary>Creates a new instance by Id. Returns NotFound if not found.</summary>");
        sb.AppendLine($"        public static {collection.InterfaceTypeName} CreateById(int id) =>");
        sb.AppendLine("            _factoriesById.TryGetValue(id, out var factory) ? factory() : NotFound;");
        sb.AppendLine();

        // CreateByName method
        sb.AppendLine($"        /// <summary>Creates a new instance by name. Returns NotFound if not found.</summary>");
        sb.AppendLine($"        public static {collection.InterfaceTypeName} CreateByName(string name) =>");
        sb.AppendLine("            _factoriesByName.TryGetValue(name, out var factory) ? factory() : NotFound;");
        sb.AppendLine();

        // GetRegisteredNames method
        sb.AppendLine($"        /// <summary>Gets all registered type names.</summary>");
        sb.AppendLine("        public static IReadOnlyCollection<string> GetRegisteredNames() =>");
        sb.AppendLine("            _registeredTypeNames;");
        sb.AppendLine();

        // CreateAll method (creates new instance of each)
        sb.AppendLine($"        /// <summary>Creates a new instance of each registered type.</summary>");
        sb.AppendLine($"        public static IEnumerable<{collection.InterfaceTypeName}> CreateAll()");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var factory in _factoriesById.Values)");
        sb.AppendLine("            {");
        sb.AppendLine("                yield return factory();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Child collection accessors
        if (childCollections.Length > 0)
        {
            sb.AppendLine("        #region Child Collections");
            sb.AppendLine();

            // Generate typed accessor property for each child collection (returns Type for static access)
            foreach (var child in childCollections)
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets the {child.ChildName} child collection type.");
                sb.AppendLine($"        /// Use {child.ChildFullTypeName} directly for static member access.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static System.Type {child.ChildName} => typeof({child.ChildFullTypeName});");
                sb.AppendLine();
            }

            // Generate ChildCollections property returning all child types
            sb.AppendLine("        /// <summary>Gets all child collection types.</summary>");
            sb.AppendLine("        public static IReadOnlyCollection<System.Type> ChildCollectionTypes { get; } = new System.Type[]");
            sb.AppendLine("        {");
            foreach (var child in childCollections)
            {
                sb.AppendLine($"            typeof({child.ChildFullTypeName}),");
            }
            sb.AppendLine("        };");
            sb.AppendLine();

            sb.AppendLine("        #endregion");
            sb.AppendLine();
        }

        // NotFound sentinel (singleton) - skip if a TypeOption named "NotFound" already exists
        var hasNotFoundOption = options.Any(o => string.Equals(o.OptionName, "NotFound", StringComparison.Ordinal));
        if (!hasNotFoundOption)
        {
            sb.AppendLine($"        private static readonly {collection.InterfaceTypeName} _notFound = new NotFound{collection.ClassName}();");
            sb.AppendLine($"        /// <summary>Gets a sentinel instance returned when lookups fail.</summary>");
            sb.AppendLine($"        public static {collection.InterfaceTypeName} NotFound => _notFound;");
            sb.AppendLine();

            CodeGeneration.GenerateNotFoundSentinel(sb, collection, abstractMembers, collection.InterfaceTypeName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
