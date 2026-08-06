using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Fdw.Collections.SourceGenerators.Shared;

namespace Fdw.Collections.SourceGenerators;

/// <summary>
/// Generator for mutable TypeCollections using ConcurrentDictionary with Register() method.
/// </summary>
[Generator]
public class MutableTypeCollectionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Fdw.Collections.Attributes.MutableTypeCollectionAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover [MutableTypeCollection] classes
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
        var resolvedBaseType = CodeGeneration.ResolveClosedGenericType(baseType, classSymbol, 0, "MutableTypeCollectionBase");
        var resolvedInterfaceType = CodeGeneration.ResolveClosedGenericType(interfaceType, classSymbol, 1, "MutableTypeCollectionBase");

        // Extract named arguments
        var parentCollection = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "TypeOption");
        var childName = CodeGeneration.GetNamedArgument<string>(attribute.NamedArguments, "TypeOptionName");
        var restrictToCurrentCompilation = CodeGeneration.GetNamedArgument(attribute.NamedArguments, "RestrictToCurrentCompilation", false);

        var baseConstructorParams = ExtractBaseConstructorParameters(baseType, classSymbol);

        return new TypeCollectionModel(
            ClassName: classSymbol.Name,
            Namespace: classSymbol.ContainingNamespace.ToDisplayString(),
            FullName: classSymbol.ToDisplayString(),
            BaseTypeName: resolvedBaseType,
            InterfaceTypeName: resolvedInterfaceType,
            MatchKey: TypeOptionDiscovery.GetMatchKey(collectionType),
            Kind: CollectionKind.Mutable,
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

        if (baseType is INamedTypeSymbol namedBaseType && namedBaseType.IsUnboundGenericType)
        {
            var current = classSymbol.BaseType;
            while (current != null)
            {
                if (current.Name.StartsWith("MutableTypeCollectionBase", StringComparison.Ordinal) &&
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

        // Why: Build replacement map to filter out replaced types from static constructor registration.
        var replacementMap = ReplacesDiscovery.BuildReplacementMap(compilation, context);

        foreach (var collection in collections)
        {
            var options = allOptions
                .Where(o => string.Equals(o.CollectionMatchKey, collection.MatchKey, StringComparison.Ordinal))
                .ToImmutableArray();

            // Why: Remove replaced types — for mutable collections the static constructor should only
            // register the replacement, not the original that has been overridden.
            options = ReplacesDiscovery.FilterReplacedTypeOptions(options, replacementMap, context);

            // Find child collections for this parent
            var childCollections = collections
                .Where(c => string.Equals(c.ParentCollectionMatchKey, collection.MatchKey, StringComparison.Ordinal) && c.ChildName != null)
                .Select(c => new ChildCollectionModel(c.ChildName!, c.FullName, c.ClassName))
                .ToImmutableArray();

            // Validate
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
                $"{collection.ClassName}.MutableTypeCollection.g.cs",
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private static void ValidateOptions(
        SourceProductionContext context,
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options)
    {
        // No options is OK for mutable collections (they can be added at runtime)

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

        // Check for duplicate lookup property values
        ValidateLookupPropertyValues(context, collection.ClassName, options);
    }

    private static void ValidateLookupPropertyValues(
        SourceProductionContext context,
        string collectionName,
        ImmutableArray<TypeOptionModel> options)
    {
        if (options.Length == 0)
            return;

        var lookupProps = options[0].LookupProperties;

        foreach (var prop in lookupProps)
        {
            if (string.Equals(prop.PropertyName, "Id", StringComparison.Ordinal) ||
                string.Equals(prop.PropertyName, "Name", StringComparison.Ordinal))
                continue;

            var valueGroups = options
                .Select(o => (Option: o, Value: o.LookupProperties
                    .FirstOrDefault(p => string.Equals(p.PropertyName, prop.PropertyName, StringComparison.Ordinal))
                    .ExtractedValue))
                .Where(x => x.Value != null)
                .GroupBy(x => x.Value, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicate in valueGroups)
            {
                var types = string.Join(", ", duplicate.Select(x => x.Option.FullTypeName));
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeCollectionGeneratorDiagnostics.DuplicateLookupValue,
                    Location.None,
                    collectionName,
                    prop.PropertyName,
                    duplicate.Key,
                    types));
            }
        }
    }

#pragma warning disable MA0051 // Source generator emits complete MutableTypeCollection class — splitting scatters the template
    // Code generation template for MutableTypeCollection — sequential string building
#pragma warning disable FDW006, FDW007
    private static string GenerateCode(
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options,
        ImmutableArray<ChildCollectionModel> childCollections,
        ImmutableArray<AbstractMemberModel> abstractMembers)
    {
        var sb = new StringBuilder();

        CodeGeneration.GenerateUsings(sb, CollectionKind.Mutable);

        sb.AppendLine($"namespace {collection.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {collection.ClassName}");
        sb.AppendLine("    {");

        // Lookup dictionaries from [TypeLookup] attributes
        var lookupGroups = options
            .SelectMany(o => o.LookupProperties)
            .GroupBy(p => p.MethodName, StringComparer.Ordinal)
            .ToList();

        // All items list - the source of truth
        sb.AppendLine($"        private static readonly List<{collection.InterfaceTypeName}> _all = new();");

        // Lookup dictionaries - rebuilt from _all on any modification
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            sb.AppendLine($"        private static Dictionary<{prop.PropertyType}, {collection.InterfaceTypeName}> {fieldName} = new();");
        }

        sb.AppendLine();

        // Static constructor - register compile-time options
        sb.AppendLine($"        static {collection.ClassName}()");
        sb.AppendLine("        {");
        foreach (var option in options)
        {
            sb.AppendLine($"            _all.Add(new {option.FullTypeName}());");
        }
        sb.AppendLine("            RebuildLookups();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // RebuildLookups - rebuilds all lookup dictionaries from _all
        sb.AppendLine($"        private static void RebuildLookups()");
        sb.AppendLine("        {");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            sb.AppendLine($"            {fieldName} = _all.ToDictionary(x => x.{prop.PropertyName});");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // RegisterMember method
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Registers a type option with the collection at runtime.");
        sb.AppendLine($"        /// Idempotent - does nothing if an option with the same Name is already registered.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static void RegisterMember({collection.InterfaceTypeName} option)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (option == null) throw new ArgumentNullException(nameof(option));");
        // Why: dedup by Name, not GetType(). Runtime-instanced collections register many distinct
        // members of the SAME class (e.g. one ConfiguredDataSetType per configured DataSet); a
        // GetType() check collapses them to one. Name is the unique key for a type option.
        sb.AppendLine("            // Idempotent: skip if already registered (by reference or Name)");
        sb.AppendLine("            foreach (var existing in _all)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (ReferenceEquals(existing, option) || string.Equals(existing.Name, option.Name, global::System.StringComparison.Ordinal))");
        sb.AppendLine("                    return;");
        sb.AppendLine("            }");
        sb.AppendLine("            _all.Add(option);");
        sb.AppendLine("            RebuildLookups();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Unregister method
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Unregisters a type option at runtime.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static bool Unregister({collection.InterfaceTypeName} option)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (option == null) throw new ArgumentNullException(nameof(option));");
        sb.AppendLine("            var removed = _all.Remove(option);");
        sb.AppendLine("            if (removed) RebuildLookups();");
        sb.AppendLine("            return removed;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Find the ById lookup for static accessors
        var byIdLookup = lookupGroups.FirstOrDefault(g => string.Equals(g.Key, "ById", StringComparison.Ordinal));
        var byIdFieldName = byIdLookup != null ? $"_{CodeGeneration.ToCamelCase(byIdLookup.Key)}" : "_byId";

        // Static accessors (singleton - return registered instance)
        // Note: We use linear search by Name since we don't know the actual Id value at compile time
        foreach (var option in options)
        {
            var fieldName = $"_{CodeGeneration.ToCamelCase(option.OptionName)}";
            sb.AppendLine($"        private static {option.FullTypeName}? {fieldName};");
            sb.AppendLine($"        /// <summary>Gets the {option.OptionName} singleton instance.</summary>");
            sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName} =>");
            sb.AppendLine($"            {fieldName} ??= ({option.FullTypeName})(_all.FirstOrDefault(x => x.Name == \"{option.OptionName}\") ?? NotFound)!;");
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

                sb.AppendLine($"        /// <summary>Creates a new instance of {option.OptionName} with the specified parameters.</summary>");
                sb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}({parameters}) =>");
                sb.AppendLine($"            new {option.FullTypeName}({arguments});");
                sb.AppendLine();
            }
        }

        // Lookup methods from [TypeLookup] - using TryGetValue for compatibility
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";

            sb.AppendLine($"        /// <summary>Looks up a type option by {prop.PropertyName}. Returns NotFound if not found.</summary>");
            sb.AppendLine($"        public static {collection.InterfaceTypeName} {group.Key}({prop.PropertyType} value) =>");
            sb.AppendLine($"            {fieldName}.TryGetValue(value, out var result) ? result : NotFound;");
            sb.AppendLine();
        }

        // Generate fallback ByName method if no [TypeLookup] ByName exists. Why: a mutable collection
        // without a declared ByName lookup still must resolve runtime-registered members by name (the
        // unique key). A linear scan over _all is correct and O(n) over a small set; the previous
        // stub returned NotFound unconditionally, silently breaking every ByName caller.
        if (!lookupGroups.Any(g => string.Equals(g.Key, "ByName", StringComparison.Ordinal)))
        {
            sb.AppendLine($"        /// <summary>Looks up a type option by name. Returns NotFound if not found.</summary>");
            sb.AppendLine($"        public static {collection.InterfaceTypeName} ByName(string? name) =>");
            sb.AppendLine($"            _all.FirstOrDefault(x => string.Equals(x.Name, name, global::System.StringComparison.Ordinal)) ?? NotFound;");
            sb.AppendLine();
        }

        // Generate fallback ById method if no [TypeLookup] ById exists. Why: same as ByName — resolve
        // runtime-registered members by id via the boxed object Id (key-type-agnostic) rather than the
        // previous unconditional NotFound stub.
        if (!lookupGroups.Any(g => string.Equals(g.Key, "ById", StringComparison.Ordinal)))
        {
            sb.AppendLine($"        /// <summary>Looks up a type option by id. Returns NotFound if not found.</summary>");
            sb.AppendLine($"        public static {collection.InterfaceTypeName} ById(int id) =>");
            sb.AppendLine($"            _all.FirstOrDefault(x => object.Equals(((global::Fdw.Collections.ITypeOption)x).Id, id)) ?? NotFound;");
            sb.AppendLine();
        }

        // All method
        sb.AppendLine($"        /// <summary>Gets all registered type options.</summary>");
        sb.AppendLine($"        public static IReadOnlyCollection<{collection.InterfaceTypeName}> All() => _all;");
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

        // NotFound sentinel - skip if a TypeOption named "NotFound" already exists
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
#pragma warning restore FDW006, FDW007
}
