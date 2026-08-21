using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Fdw.Collections.SourceGenerators.Shared;
using Fdw.Conventions;

namespace Fdw.Collections.SourceGenerators;

/// <summary>
/// Generator for immutable TypeCollections using FrozenDictionary.
/// </summary>
[Generator]
public class TypeCollectionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Fdw.Collections.Attributes.TypeCollectionAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover [TypeCollection] classes
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
        var resolvedBaseType = CodeGeneration.ResolveClosedGenericType(baseType, classSymbol, 0, "TypeCollectionBase");
        var resolvedInterfaceType = CodeGeneration.ResolveClosedGenericType(interfaceType, classSymbol, 1, "TypeCollectionBase");

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
            Kind: CollectionKind.Immutable,
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
                if (current.Name.StartsWith("TypeCollectionBase", StringComparison.Ordinal) &&
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

        // Why: Build a single replacement map from all [Replaces] attributes across the compilation.
        // This map is shared across all collections — filtering happens per-collection below.
        var replacementMap = ReplacesDiscovery.BuildReplacementMap(compilation, context);

        foreach (var collection in collections)
        {
            var options = allOptions
                .Where(o => string.Equals(o.CollectionMatchKey, collection.MatchKey, StringComparison.Ordinal))
                .ToImmutableArray();

            // Why: Remove replaced types so the static constructor doesn't register originals
            // that have been overridden by [Replaces] in this or a downstream assembly.
            options = ReplacesDiscovery.FilterReplacedTypeOptions(options, replacementMap, context);

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

            // Detect user-declared partial NotFound class members
            var userDeclaredMembers = DetectUserDeclaredNotFoundMembers(compilation, collection);

            // Check if interface has Name/Id properties (including inherited from base interfaces)
            var hasNameProperty = CodeGeneration.InterfaceHasProperty(interfaceTypeSymbol, "Name");
            var hasIdProperty = CodeGeneration.InterfaceHasProperty(interfaceTypeSymbol, "Id");

            // Generate
            var code = GenerateCode(collection, options, childCollections, abstractMembers, hasNameProperty, hasIdProperty, userDeclaredMembers);
            context.AddSource(
                $"{collection.ClassName}.TypeCollection.g.cs",
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private static void ValidateOptions(
        SourceProductionContext context,
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options)
    {
        // Empty collections are valid - TypeOptions may be added in other packages
        if (options.Length == 0)
        {
            return;
        }

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

        // Check for names the generated collection already uses for its own members
        foreach (var reserved in options.Where(o => ReservedMemberNames.IsReserved(o.OptionName)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.ReservedOptionName,
                Location.None,
                reserved.FullTypeName,
                reserved.OptionName));
        }

        // Check for duplicate lookup property values
        ValidateLookupPropertyValues(context, collection.ClassName, options);

        // Check for constructor parameters that can't be safely defaulted
        var unsafeParams = CodeGeneration.GetUnsafeParameters(collection.BaseConstructorParameters);
        foreach (var param in unsafeParams)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.UnknownConstructorParameterType,
                Location.None,
                collection.ClassName,
                param.Name,
                param.Type));
        }
    }

    private static void ValidateLookupPropertyValues(
        SourceProductionContext context,
        string collectionName,
        ImmutableArray<TypeOptionModel> options)
    {
        if (options.Length == 0)
            return;

        // Get all lookup properties from the first option (they're the same across all options)
        var lookupProps = options[0].LookupProperties;

        foreach (var prop in lookupProps)
        {
            // Skip Id and Name - they're validated separately
            if (string.Equals(prop.PropertyName, "Id", StringComparison.Ordinal) ||
                string.Equals(prop.PropertyName, "Name", StringComparison.Ordinal))
                continue;

            // Group options by their extracted value for this property
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

#pragma warning disable MA0051 // Source generator emits complete TypeCollection class — splitting scatters the template
    [ConventionOverride(MaxCyclomaticComplexity = 40, MaxMethodLines = 351)]  // Code generation template for TypeCollection — sequential string building
    private static string GenerateCode(
        TypeCollectionModel collection,
        ImmutableArray<TypeOptionModel> options,
        ImmutableArray<ChildCollectionModel> childCollections,
        ImmutableArray<AbstractMemberModel> abstractMembers,
        bool hasNameProperty,
        bool hasIdProperty,
        HashSet<string>? userDeclaredMembers = null)
    {
        var sb = new StringBuilder();
        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Threading"
        };
        var netstandardNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Collections.Immutable"
        };
        var modernNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Collections.Frozen"
        };

        // Build the class body first, then prepend usings
        var bodySb = new StringBuilder();
        bodySb.AppendLine($"namespace {collection.Namespace}");
        bodySb.AppendLine("{");
        bodySb.AppendLine($"    partial class {collection.ClassName}");
        bodySb.AppendLine("    {");

        // Lookup dictionaries from [TypeLookup] attributes
        var lookupGroups = options
            .SelectMany(o => o.LookupProperties)
            .GroupBy(p => p.MethodName, StringComparer.Ordinal)
            .ToList();

        // === DEFERRED FREEZE PATTERN ===

        // Pending registrations list
        bodySb.AppendLine("        // Pending registrations (before freeze)");
        bodySb.AppendLine($"        private static readonly List<{collection.InterfaceTypeName}> _pendingRegistrations = new();");
        bodySb.AppendLine("        private static readonly System.Collections.Generic.HashSet<System.Type> _registeredTypes = new();");
        bodySb.AppendLine("        private static readonly object _lock = new();");
        bodySb.AppendLine("        private static volatile bool _frozen;");
        bodySb.AppendLine();

        // Frozen array (populated on first access)
        bodySb.AppendLine($"        private static {collection.InterfaceTypeName}[]? _all;");
        bodySb.AppendLine();

        // Nullable frozen dictionaries for lookups
        bodySb.AppendLine("        // Frozen dictionaries (populated on first access)");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            // Why the value type follows IsUnique: a unique lookup maps a value to THE option, a
            // non-unique one maps it to every option that carries it. Both are dictionaries; only the
            // value side differs, so the lookup stays one hash probe either way.
            var valueType = prop.IsUnique
                ? collection.InterfaceTypeName
                : $"IReadOnlyList<{collection.InterfaceTypeName}>";
            bodySb.AppendLine("#if NETSTANDARD2_0");
            bodySb.AppendLine($"        private static ImmutableDictionary<{prop.PropertyType}, {valueType}>? {fieldName};");
            bodySb.AppendLine("#else");
            bodySb.AppendLine($"        private static FrozenDictionary<{prop.PropertyType}, {valueType}>? {fieldName};");
            bodySb.AppendLine("#endif");
        }
        bodySb.AppendLine();

        // Static constructor - registers compile-time discovered options
        bodySb.AppendLine($"        static {collection.ClassName}()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            // Register options discovered at compile time in this assembly");
        foreach (var option in options)
        {
            bodySb.AppendLine($"            RegisterMember(new {option.FullTypeName}());");
        }
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        AppendRegisterMember(bodySb, collection);

        // EnsureFrozen method
        bodySb.AppendLine("        /// <summary>");
        bodySb.AppendLine("        /// Ensures the collection is frozen. Called automatically on first access.");
        bodySb.AppendLine("        /// </summary>");
        bodySb.AppendLine("        private static void EnsureFrozen()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            if (_all != null) return;");
        bodySb.AppendLine();
        bodySb.AppendLine("            lock (_lock)");
        bodySb.AppendLine("            {");
        bodySb.AppendLine("                if (_all != null) return;");
        bodySb.AppendLine();
        bodySb.AppendLine("                _frozen = true;");
        bodySb.AppendLine("                var items = _pendingRegistrations.ToArray();");
        bodySb.AppendLine();
        bodySb.AppendLine("#if NETSTANDARD2_0");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            // Why a unique lookup keeps the plain dictionary build: ToImmutableDictionary throws on a duplicate
            // key, which is exactly the contract IsUnique declares. The throw is caught below and
            // re-raised with the option names, because the framework exception names neither.
            if (prop.IsUnique)
                bodySb.AppendLine($"                {fieldName} = items.ToImmutableDictionary(x => x.{prop.PropertyName});");
            else
                bodySb.AppendLine($"                {fieldName} = items.GroupBy(x => x.{prop.PropertyName}).ToImmutableDictionary(g => g.Key, g => (IReadOnlyList<{collection.InterfaceTypeName}>)g.ToArray());");
        }
        bodySb.AppendLine("#else");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            // Why a unique lookup keeps the plain dictionary build: ToFrozenDictionary throws on a duplicate
            // key, which is exactly the contract IsUnique declares. The throw is caught below and
            // re-raised with the option names, because the framework exception names neither.
            if (prop.IsUnique)
                bodySb.AppendLine($"                {fieldName} = items.ToFrozenDictionary(x => x.{prop.PropertyName});");
            else
                bodySb.AppendLine($"                {fieldName} = items.GroupBy(x => x.{prop.PropertyName}).ToFrozenDictionary(g => g.Key, g => (IReadOnlyList<{collection.InterfaceTypeName}>)g.ToArray());");
        }
        bodySb.AppendLine("#endif");
        bodySb.AppendLine("                // Set _all last — it's the sentinel for the lock-free fast path.");
        bodySb.AppendLine("                // All lookup dictionaries must be populated before _all is visible to other threads.");
        bodySb.AppendLine("                _all = items;");
        bodySb.AppendLine("            }");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // Find the ById lookup for static accessors
        var byIdLookup = lookupGroups.FirstOrDefault(g => string.Equals(g.Key, "ById", StringComparison.Ordinal));
        var byIdFieldName = byIdLookup != null ? $"_{CodeGeneration.ToCamelCase(byIdLookup.Key)}" : null;

        // Static property accessors (singleton) - only for compile-time discovered options
        // Note: We use linear search by Name since we don't know the actual Id value at compile time
        // (Id is determined by the TypeOption constructor, not by the generator)
        foreach (var option in options)
        {
            var fieldName = $"_option{CodeGeneration.ToPascalCase(option.OptionName)}";
            bodySb.AppendLine($"        private static {option.FullTypeName}? {fieldName};");
            bodySb.AppendLine($"        /// <summary>Gets the {option.OptionName} singleton instance.</summary>");
            bodySb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            get");
            bodySb.AppendLine("            {");
            bodySb.AppendLine("                EnsureFrozen();");
            // Use linear search by Name - Id values are runtime-determined
            bodySb.AppendLine($"                return {fieldName} ??= ({option.FullTypeName})_all!.First(x => x.Name == \"{option.OptionName}\");");
            bodySb.AppendLine("            }");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // Lookup methods from [TypeLookup] - now call EnsureFrozen
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";

            // Why the shape follows IsUnique: a unique lookup answers with THE option and NotFound when
            // there is none; a non-unique one answers with every match and an empty list when there are
            // none. Returning NotFound from a list-valued lookup would make "no match" and "one match"
            // the same shape to a caller that iterates.
            var returns = prop.IsUnique
                ? collection.InterfaceTypeName
                : $"IReadOnlyList<{collection.InterfaceTypeName}>";
            var miss = prop.IsUnique
                ? "NotFound"
                : $"System.Array.Empty<{collection.InterfaceTypeName}>()";
            var summary = prop.IsUnique
                ? $"Looks up the type option whose {prop.PropertyName} is the given value. Returns NotFound if there is none."
                : $"Looks up every type option whose {prop.PropertyName} is the given value. Returns an empty list if there are none.";

            bodySb.AppendLine($"        /// <summary>{summary}</summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static {returns} {group.Key}({prop.PropertyType} value)");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            EnsureFrozen();");
            bodySb.AppendLine($"            return {fieldName}!.TryGetValue(value, out var result) ? result : {miss};");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // hasNameProperty and hasIdProperty are passed as parameters (checked against interface, not abstractMembers)

        // Generate fallback ByName method if no ByName lookup exists (for cross-assembly discovery)
        if (!lookupGroups.Any(g => string.Equals(g.Key, "ByName", StringComparison.Ordinal)))
        {
            bodySb.AppendLine($"        /// <summary>Looks up a type option by name. Returns NotFound if not found.</summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static {collection.InterfaceTypeName} ByName(string? name)");
            bodySb.AppendLine("        {");
            if (hasNameProperty)
            {
                bodySb.AppendLine("            EnsureFrozen();");
                bodySb.AppendLine("            if (string.IsNullOrEmpty(name)) return NotFound;");
                bodySb.AppendLine("            foreach (var item in _all!)");
                bodySb.AppendLine("            {");
                bodySb.AppendLine("                if (string.Equals(item.Name, name, StringComparison.Ordinal))");
                bodySb.AppendLine("                    return item;");
                bodySb.AppendLine("            }");
                bodySb.AppendLine("            return NotFound;");
            }
            else
            {
                bodySb.AppendLine("            return NotFound;");
            }
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // Generate fallback ById method if no ById lookup exists (for cross-assembly discovery)
        if (!lookupGroups.Any(g => string.Equals(g.Key, "ById", StringComparison.Ordinal)))
        {
            bodySb.AppendLine($"        /// <summary>Looks up a type option by id. Returns NotFound if not found.</summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static {collection.InterfaceTypeName} ById(int id)");
            bodySb.AppendLine("        {");
            if (hasIdProperty)
            {
                bodySb.AppendLine("            EnsureFrozen();");
                bodySb.AppendLine("            foreach (var item in _all!)");
                bodySb.AppendLine("            {");
                bodySb.AppendLine("                if (item.Id.Equals(id))");
                bodySb.AppendLine("                    return item;");
                bodySb.AppendLine("            }");
                bodySb.AppendLine("            return NotFound;");
            }
            else
            {
                bodySb.AppendLine("            return NotFound;");
            }
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // All method - returns all registered options
        bodySb.AppendLine($"        /// <summary>Gets all registered type options.</summary>");
        bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
        bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
        bodySb.AppendLine($"        public static IReadOnlyCollection<{collection.InterfaceTypeName}> All()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            EnsureFrozen();");
        bodySb.AppendLine("            return _all!;");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // Compile-time category dictionary (maps type name to category)
        var optionsWithCategories = options.Where(o => !string.IsNullOrEmpty(o.Category)).ToList();
        if (optionsWithCategories.Count > 0)
        {
            bodySb.AppendLine("        // Category mappings discovered at compile time");
            bodySb.AppendLine("        private static readonly Dictionary<string, string> _categoryByTypeName = new(StringComparer.Ordinal)");
            bodySb.AppendLine("        {");
            foreach (var opt in optionsWithCategories)
            {
                bodySb.AppendLine($"            {{ \"{opt.FullTypeName}\", \"{opt.Category}\" }},");
            }
            bodySb.AppendLine("        };");
            bodySb.AppendLine();
        }

        // ByCategory method - returns options matching the category
        bodySb.AppendLine($"        /// <summary>Gets all type options in the specified category.</summary>");
        bodySb.AppendLine($"        /// <param name=\"category\">The category to filter by.</param>");
        bodySb.AppendLine($"        /// <returns>All options matching the category, or a list containing NotFound if none found.</returns>");
        bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
        bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
        bodySb.AppendLine($"        public static IReadOnlyList<{collection.InterfaceTypeName}> ByCategory(string? category)");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            EnsureFrozen();");
        bodySb.AppendLine("            if (string.IsNullOrEmpty(category)) return new[] { NotFound };");
        bodySb.AppendLine("            var results = new List<" + collection.InterfaceTypeName + ">();");
        bodySb.AppendLine("            foreach (var item in _all!)");
        bodySb.AppendLine("            {");
        bodySb.AppendLine("                var cat = GetCategory(item);");
        bodySb.AppendLine("                if (string.Equals(cat, category, StringComparison.Ordinal))");
        bodySb.AppendLine("                    results.Add(item);");
        bodySb.AppendLine("            }");
        bodySb.AppendLine("            return results.Count > 0 ? results : new[] { NotFound };");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // Categories property - returns all distinct categories
        bodySb.AppendLine("        /// <summary>Gets all distinct categories across registered type options.</summary>");
        bodySb.AppendLine("        public static IReadOnlyList<string> Categories");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            get");
        bodySb.AppendLine("            {");
        bodySb.AppendLine("                EnsureFrozen();");
        bodySb.AppendLine("                var categories = new HashSet<string>(StringComparer.Ordinal);");
        bodySb.AppendLine("                foreach (var item in _all!)");
        bodySb.AppendLine("                {");
        bodySb.AppendLine("                    var cat = GetCategory(item);");
        bodySb.AppendLine("                    if (cat != null && cat.Length > 0)");
        bodySb.AppendLine("                        categories.Add(cat);");
        bodySb.AppendLine("                }");
        bodySb.AppendLine("                var result = new List<string>(categories);");
        bodySb.AppendLine("                result.Sort(StringComparer.Ordinal);");
        bodySb.AppendLine("                return result;");
        bodySb.AppendLine("            }");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // GetCategory helper method - uses compile-time dictionary first, then falls back to reflection
        bodySb.AppendLine("        /// <summary>Gets the category for a type option.</summary>");
        bodySb.AppendLine($"        private static string? GetCategory({collection.InterfaceTypeName} option)");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            var typeName = option.GetType().FullName;");
        bodySb.AppendLine("            if (typeName == null) return null;");
        if (optionsWithCategories.Count > 0)
        {
            bodySb.AppendLine("            // Check compile-time discovered categories first");
            bodySb.AppendLine("            if (_categoryByTypeName.TryGetValue(typeName, out var category))");
            bodySb.AppendLine("                return category;");
        }
        bodySb.AppendLine("            // Fall back to reflection for dynamically registered options");
        bodySb.AppendLine("            var attrs = option.GetType().GetCustomAttributes(typeof(Fdw.Collections.Attributes.TypeOptionAttribute), false);");
        bodySb.AppendLine("            if (attrs.Length > 0 && attrs[0] is Fdw.Collections.Attributes.TypeOptionAttribute attr)");
        bodySb.AppendLine("                return attr.Category;");
        bodySb.AppendLine("            return null;");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // GetMetadata method - returns TypeCollectionMetadata
        bodySb.AppendLine("        private static Fdw.Types.TypeCollectionMetadata? _metadata;");
        bodySb.AppendLine();
        bodySb.AppendLine("        /// <summary>Gets metadata describing this TypeCollection.</summary>");
        bodySb.AppendLine("        public static Fdw.Types.TypeCollectionMetadata GetMetadata()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            EnsureFrozen();");
        bodySb.AppendLine("            return _metadata ??= new Fdw.Types.TypeCollectionMetadata");
        bodySb.AppendLine("            {");
        bodySb.AppendLine($"                Id = ComputeFnv1aHash(\"{collection.FullName}\"),");
        bodySb.AppendLine($"                Name = \"{collection.ClassName}\",");
        bodySb.AppendLine($"                FullName = \"{collection.FullName}\",");

        // Map internal CollectionKind to Fdw.Types.CollectionKinds TypeCollection
        // Pragma: source generators cannot use TypeCollections (bootstrapping problem)
#pragma warning disable FDW018
        var collectionKindValue = collection.Kind switch
        {
            CollectionKind.Immutable => "Fdw.Types.CollectionKinds.Immutable",
            CollectionKind.Mutable => "Fdw.Types.CollectionKinds.Mutable",
            CollectionKind.Factory => "Fdw.Types.CollectionKinds.Instance",
            _ => "Fdw.Types.CollectionKinds.Immutable"
        };
#pragma warning restore FDW018
        bodySb.AppendLine($"                CollectionKind = {collectionKindValue},");

        bodySb.AppendLine("                Options = _all!.Select(o =>");
        bodySb.AppendLine("                {");
        bodySb.AppendLine("                    // Convert ID to int - if already int, use as-is; otherwise hash the string representation");
        bodySb.AppendLine("                    int id;");

        // Only access Id property if it exists on the interface
        if (hasIdProperty)
        {
            bodySb.AppendLine("                    object idObj = o.Id;");
            bodySb.AppendLine("                    if (idObj is int intId)");
            bodySb.AppendLine("                        id = intId;");
            bodySb.AppendLine("                    else");
            bodySb.AppendLine("                        id = ComputeFnv1aHash(idObj?.ToString() ?? string.Empty);");
        }
        else
        {
            bodySb.AppendLine("                    // Interface does not have Id property - use type hash");
            bodySb.AppendLine("                    id = ComputeFnv1aHash(o.GetType().FullName ?? o.GetType().Name);");
        }

        bodySb.AppendLine();
        bodySb.AppendLine("                    return new Fdw.Types.TypeOptionMetadata");
        bodySb.AppendLine("                    {");
        bodySb.AppendLine("                        Id = id,");

        // Only access Name property if it exists on the interface
        if (hasNameProperty)
        {
            bodySb.AppendLine("                        Name = o.Name,");
        }
        else
        {
            bodySb.AppendLine("                        Name = o.GetType().Name,");
        }

        bodySb.AppendLine($"                        TypeCollectionId = ComputeFnv1aHash(\"{collection.FullName}\"),");
        bodySb.AppendLine("                        FullTypeName = o.GetType().FullName ?? o.GetType().Name");
        bodySb.AppendLine("                    };");
        bodySb.AppendLine("                }).ToList()");
        bodySb.AppendLine("            };");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // FNV-1a hash computation method
        bodySb.AppendLine("        /// <summary>Computes FNV-1a hash for string.</summary>");
        bodySb.AppendLine("        private static int ComputeFnv1aHash(string text)");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            unchecked");
        bodySb.AppendLine("            {");
        bodySb.AppendLine("                const int fnvOffsetBasis = unchecked((int)2166136261);");
        bodySb.AppendLine("                const int fnvPrime = 16777619;");
        bodySb.AppendLine("                int hash = fnvOffsetBasis;");
        bodySb.AppendLine("                foreach (char c in text)");
        bodySb.AppendLine("                {");
        bodySb.AppendLine("                    hash ^= c;");
        bodySb.AppendLine("                    hash *= fnvPrime;");
        bodySb.AppendLine("                }");
        bodySb.AppendLine("                return hash;");
        bodySb.AppendLine("            }");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // Child collection accessors
        if (childCollections.Length > 0)
        {
            bodySb.AppendLine("        #region Child Collections");
            bodySb.AppendLine();

            // Generate typed accessor property for each child collection (returns Type for static access)
            foreach (var child in childCollections)
            {
                bodySb.AppendLine($"        /// <summary>");
                bodySb.AppendLine($"        /// Gets the {child.ChildName} child collection type.");
                bodySb.AppendLine($"        /// Use {child.ChildFullTypeName} directly for static member access.");
                bodySb.AppendLine($"        /// </summary>");
                bodySb.AppendLine($"        public static System.Type {child.ChildName} => typeof({child.ChildFullTypeName});");
                bodySb.AppendLine();
            }

            // Generate ChildCollections property returning all child types
            bodySb.AppendLine("        /// <summary>Gets all child collection types.</summary>");
            bodySb.AppendLine("        public static IReadOnlyCollection<System.Type> ChildCollectionTypes { get; } = new System.Type[]");
            bodySb.AppendLine("        {");
            foreach (var child in childCollections)
            {
                bodySb.AppendLine($"            typeof({child.ChildFullTypeName}),");
            }
            bodySb.AppendLine("        };");
            bodySb.AppendLine();

            bodySb.AppendLine("        #endregion");
            bodySb.AppendLine();
        }

        // NotFound sentinel - skip if a TypeOption named "NotFound" already exists
        var hasNotFoundOption = options.Any(o => string.Equals(o.OptionName, "NotFound", StringComparison.Ordinal));
        if (!hasNotFoundOption)
        {
            bodySb.AppendLine($"        private static readonly {collection.InterfaceTypeName} _notFound = new NotFound{collection.ClassName}();");
            bodySb.AppendLine($"        /// <summary>Gets a sentinel instance returned when lookups fail.</summary>");
            bodySb.AppendLine($"        public static {collection.InterfaceTypeName} NotFound => _notFound;");
            bodySb.AppendLine();

            CodeGeneration.GenerateNotFoundSentinel(bodySb, collection, abstractMembers, collection.InterfaceTypeName, userDeclaredMembers);
        }

        bodySb.AppendLine("    }");
        bodySb.AppendLine("}");

        // Build final output: header + usings + body
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        // Write unconditional usings (sorted and deduped via HashSet)
        foreach (var ns in namespaces.OrderBy(n => n, StringComparer.Ordinal))
        {
            sb.AppendLine($"using {ns};");
        }

        // Write conditional usings for netstandard2.0 vs modern
        if (netstandardNamespaces.Count > 0 || modernNamespaces.Count > 0)
        {
            sb.AppendLine("#if NETSTANDARD2_0");
            foreach (var ns in netstandardNamespaces.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine("#else");
            foreach (var ns in modernNamespaces.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine("#endif");
        }

        sb.AppendLine();
        sb.Append(bodySb);

        return sb.ToString();
    }


    // Why extracted: GenerateCode sits at the FDW006 executable-line budget, and the RegisterMember
    // emission is a self-contained block with a single input — the same reason the metadata and
    // provider-registration emitters were extracted.
    private static void AppendRegisterMember(StringBuilder bodySb, TypeCollectionModel collection)
    {
            bodySb.AppendLine("        /// <summary>");
            bodySb.AppendLine("        /// Registers a type option with the collection. Must be called before first access to All() or any lookup method.");
            bodySb.AppendLine("        /// Typically called from module initializers in assemblies that define TypeOptions.");
            bodySb.AppendLine("        /// </summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static void RegisterMember({collection.InterfaceTypeName} type)");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            if (type == null) throw new ArgumentNullException(nameof(type));");
            bodySb.AppendLine();
            bodySb.AppendLine("            lock (_lock)");
            bodySb.AppendLine("            {");
            bodySb.AppendLine("                // Why membership is asked FIRST, and as a set: a member can be offered from more than");
            bodySb.AppendLine("                // one direction — its own assembly's module initializer and the cross-assembly");
            bodySb.AppendLine("                // registration an entry point emits. Re-offering one already present is a no-op at every");
            bodySb.AppendLine("                // point in the lifecycle, including after the collection closes, because the collection");
            bodySb.AppendLine("                // already holds it. Only a genuinely NEW member arriving after the close is an error.");
            bodySb.AppendLine("                // Asking the frozen question first turned the harmless case into a throw.");
            bodySb.AppendLine("                if (!_registeredTypes.Add(type.GetType()))");
            bodySb.AppendLine("                    return; // Already registered (idempotent)");
            bodySb.AppendLine();
            bodySb.AppendLine("                if (_frozen)");
            bodySb.AppendLine("                {");
            bodySb.AppendLine("                    // Rejected, so not a member: take the type back out, or a second attempt reads as");
            bodySb.AppendLine("                    // a duplicate and returns quietly — a loud failure turned silent.");
            bodySb.AppendLine("                    _registeredTypes.Remove(type.GetType());");
            bodySb.AppendLine($"                    throw new InvalidOperationException($\"Cannot register '{{type.GetType().Name}}': {collection.ClassName} is already frozen. Ensure all assemblies with TypeOptions are loaded before first access.\");");
            bodySb.AppendLine("                }");
            bodySb.AppendLine();
            bodySb.AppendLine("                _pendingRegistrations.Add(type);");
            bodySb.AppendLine("            }");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
    }

    private static HashSet<string>? DetectUserDeclaredNotFoundMembers(Compilation compilation, TypeCollectionModel collection)
    {
        // Find the collection class type symbol
        var collectionSymbol = compilation.GetTypeByMetadataName($"{collection.Namespace}.{collection.ClassName}")
            ?? CodeGeneration.GetTypeByFullName(compilation, $"{collection.Namespace}.{collection.ClassName}");
        if (collectionSymbol is null) return null;

        // Look for a nested type named NotFound{ClassName}
        var sentinelName = $"NotFound{collection.ClassName}";
        var nestedType = collectionSymbol.GetTypeMembers(sentinelName).FirstOrDefault();
        if (nestedType is null) return null;

        // Collect declared member names (properties and methods)
        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in nestedType.GetMembers())
        {
            if (member is IPropertySymbol prop && !prop.IsImplicitlyDeclared)
                members.Add(prop.Name);
            else if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary && !method.IsImplicitlyDeclared)
                members.Add(method.Name);
        }

        return members.Count > 0 ? members : null;
    }
}
