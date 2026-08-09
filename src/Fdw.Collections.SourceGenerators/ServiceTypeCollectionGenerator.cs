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

namespace Fdw.Collections.SourceGenerators;

/// <summary>
/// Generator for immutable ServiceTypeCollections using FrozenDictionary.
/// </summary>
[Generator]
public class ServiceTypeCollectionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Fdw.Collections.ServiceTypeCollectionAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover [ServiceTypeCollection] classes
        var collectionsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: ExtractCollectionModel)
            .Where(static m => m != null)
            .Select(static (m, _) => m!.Value);

        // Discover all ServiceTypeOptions
        var optionsProvider = context.CompilationProvider
            .Combine(collectionsProvider.Collect())
            .Select(static (pair, _) =>
            {
                var (compilation, collections) = pair;
                var restrictToCurrentCompilation = collections.Any(c => c.RestrictToCurrentCompilation);
                return ServiceTypeOptionDiscovery.DiscoverAll(compilation, restrictToCurrentCompilation);
            });

        // Combine collections, options, and compilation for abstract member extraction
        var combinedProvider = collectionsProvider.Collect()
            .Combine(optionsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combinedProvider, Execute);
    }

#pragma warning disable MA0051 // Source generator model extraction requires cohesive Roslyn symbol inspection
    // Attribute parameter extraction via Roslyn — sequential symbol inspection
#pragma warning disable FDW006, FDW007
    private static ServiceTypeCollectionModel? ExtractCollectionModel(
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

        // Handle unbound generics by extracting closed types from the class's base type
        // The base class (ServiceTypeCollectionBase<TBase, TInterface, ...>) has the closed generic types
        var resolvedBaseType = ResolveClosedGenericType(baseType, classSymbol, 0);
        var resolvedInterfaceType = ResolveClosedGenericType(interfaceType, classSymbol, 1);

        // Extract optional parent collection and name (constructor args 3 and 4)
        var parentCollection = attribute.ConstructorArguments.Length > 3
            ? attribute.ConstructorArguments[3].Value as ITypeSymbol
            : null;
        var childName = attribute.ConstructorArguments.Length > 4
            ? attribute.ConstructorArguments[4].Value?.ToString()
            : null;

        // Extract named arguments using helper
        var restrictToCurrentCompilation = CodeGeneration.GetNamedArgument(attribute.NamedArguments, "RestrictToCurrentCompilation", false);
        var generateProvider = CodeGeneration.GetNamedArgument(attribute.NamedArguments, "GenerateProvider", false);
        var serviceInterface = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "ServiceInterface");
        var configurationInterface = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "ConfigurationInterface");
        var configurationType = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "ConfigurationType");
        var providerType = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "ProviderType");
        var providerInterface = CodeGeneration.GetNamedArgument<ITypeSymbol>(attribute.NamedArguments, "ProviderInterface");
        var serviceCategory = CodeGeneration.GetNamedArgument<string>(attribute.NamedArguments, "ServiceCategory");

        var baseConstructorParams = ExtractBaseConstructorParameters(baseType);

        // Why no more manual-method detection: Configure/Register/Initialize are now ALWAYS generated as
        // swappable static delegate fields (ConfigurationMethod/RegistrationMethod/InitializeMethod) with
        // a runtime setter (Configuration/Registration/Initialization) — a domain that needs custom phase
        // behavior (e.g. Multitenancy's self-selecting Configure) overrides the field via an explicit
        // static constructor instead of hand-writing the method, so there is nothing left to detect or
        // skip. See MultitenancyTypes for the reference pattern.

        return new ServiceTypeCollectionModel(
            ClassName: classSymbol.Name,
            Namespace: classSymbol.ContainingNamespace.ToDisplayString(),
            FullName: classSymbol.ToDisplayString(),
            BaseTypeName: resolvedBaseType,
            InterfaceTypeName: resolvedInterfaceType,
            MatchKey: ServiceTypeOptionDiscovery.GetMatchKey(collectionType),
            Kind: CollectionKind.Immutable,
            RestrictToCurrentCompilation: restrictToCurrentCompilation,
            ParentCollectionMatchKey: parentCollection != null ? ServiceTypeOptionDiscovery.GetMatchKey(parentCollection) : null,
            ChildName: childName,
            GenerateProvider: generateProvider,
            ServiceInterfaceTypeName: serviceInterface?.ToDisplayString(),
            ConfigurationInterfaceTypeName: configurationInterface?.ToDisplayString(),
            ConfigurationTypeName: configurationType?.ToDisplayString(),
            ProviderTypeName: providerType?.ToDisplayString(),
            ProviderInterfaceTypeName: providerInterface?.ToDisplayString(),
            ServiceCategory: serviceCategory,
            BaseConstructorParameters: baseConstructorParams
        );
    }
#pragma warning restore FDW006, FDW007

    private static ImmutableArray<ParameterModel> ExtractBaseConstructorParameters(ITypeSymbol baseType)
    {
        if (baseType is not INamedTypeSymbol namedType)
            return ImmutableArray<ParameterModel>.Empty;

        // Find the protected constructor with fewest parameters (prefer parameterless for Empty sentinel)
        var ctor = namedType.InstanceConstructors
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

    /// <summary>
    /// Resolves an unbound generic type to its closed form from the class's base type.
    /// When the attribute contains an unbound generic, we need the closed generic
    /// from the class's base type (ServiceTypeCollectionBase).
    /// </summary>
    private static string ResolveClosedGenericType(ITypeSymbol attributeType, INamedTypeSymbol classSymbol, int typeArgumentIndex)
    {
        // If not an unbound generic, return as-is
        if (attributeType is not INamedTypeSymbol namedType || !namedType.IsUnboundGenericType)
        {
            return attributeType.ToDisplayString();
        }

        // Find the ServiceTypeCollectionBase<...> base class
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.Name.StartsWith("ServiceTypeCollectionBase", StringComparison.Ordinal) &&
                current.TypeArguments.Length > typeArgumentIndex)
            {
                return current.TypeArguments[typeArgumentIndex].ToDisplayString();
            }

            current = current.BaseType;
        }

        // Fallback to original if we can't resolve
        return attributeType.ToDisplayString();
    }

    private static void Execute(
        SourceProductionContext context,
        ((ImmutableArray<ServiceTypeCollectionModel> Collections, ImmutableArray<ServiceTypeOptionModel> Options) Data, Compilation Compilation) source)
    {
        var (collections, allOptions) = source.Data;
        var compilation = source.Compilation;

        // Why: Build replacement map to filter out replaced ServiceTypeOptions from static constructor registration.
        var replacementMap = ReplacesDiscovery.BuildReplacementMap(compilation, context);

        foreach (var collection in collections)
        {
            var options = allOptions
                .Where(o => string.Equals(o.CollectionMatchKey, collection.MatchKey, StringComparison.Ordinal))
                .ToImmutableArray();

            // Why: Remove replaced types so the static constructor only registers the replacement.
            options = ReplacesDiscovery.FilterReplacedServiceTypeOptions(options, replacementMap, context);

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

            var code = GenerateCode(collection, options, childCollections, abstractMembers, userDeclaredMembers);
            context.AddSource(
                $"{collection.ClassName}.ServiceTypeCollection.g.cs",
                SourceText.From(code, Encoding.UTF8));

            // Generate provider if requested
            if (collection.GenerateProvider &&
                collection.ServiceInterfaceTypeName != null &&
                collection.ConfigurationInterfaceTypeName != null)
            {
                var providerCode = GenerateProviderCode(collection, options);
                context.AddSource(
                    $"{collection.ClassName}.Provider.g.cs",
                    SourceText.From(providerCode, Encoding.UTF8));
            }
        }
    }

    private static void ValidateOptions(
        SourceProductionContext context,
        ServiceTypeCollectionModel collection,
        ImmutableArray<ServiceTypeOptionModel> options)
    {
        // Empty collections are valid - ServiceTypeOptions may be added in other packages
        if (options.Length == 0)
        {
            return;
        }

        // Check for Id collisions (extremely rare with Guids)
        // Check for names the generated collection already uses for its own members
        foreach (var reserved in options.Where(o => ReservedMemberNames.IsReserved(o.OptionName)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.ReservedServiceTypeOptionName,
                Location.None,
                reserved.FullTypeName,
                reserved.OptionName));
        }

        var idGroups = options.GroupBy(o => o.GeneratedId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var collision in idGroups)
        {
            var types = string.Join(", ", collision.Select(o => o.FullTypeName));
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.ServiceTypeIdCollision,
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
                TypeCollectionGeneratorDiagnostics.DuplicateServiceTypeName,
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
        ImmutableArray<ServiceTypeOptionModel> options)
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
                    TypeCollectionGeneratorDiagnostics.DuplicateServiceTypeLookupValue,
                    Location.None,
                    collectionName,
                    prop.PropertyName,
                    duplicate.Key,
                    types));
            }
        }
    }

#pragma warning disable MA0051 // Source generator emits complete ServiceTypeCollection class — splitting scatters the template
    private static string GenerateCode(
        ServiceTypeCollectionModel collection,
        ImmutableArray<ServiceTypeOptionModel> options,
        ImmutableArray<ChildCollectionModel> childCollections,
        ImmutableArray<AbstractMemberModel> abstractMembers,
        HashSet<string>? userDeclaredMembers = null)
    {
        var sb = new StringBuilder();
        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System",
            "System.Collections.Generic",
            "System.Runtime.CompilerServices",
            "System.Linq",
            "System.Threading",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Extensions",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging",
            "Fdw.Collections",
            "Fdw.Configuration.Abstractions",
            "Fdw.ServiceTypes",
            "Fdw.ServiceTypes.Logging",
            "Fdw.Types"
        };

        // Add namespaces needed for provider pattern (discovered from types, not hardcoded)
        if (collection.HasExplicitProviderType)
        {
            namespaces.Add("Fdw.Configuration.Abstractions");

            // Extract namespaces from the types specified in the attribute
            if (collection.ProviderInterfaceTypeName is { } providerInterfaceTypeName &&
                GetNamespaceFromFullTypeName(providerInterfaceTypeName) is { Length: > 0 } ns1)
                namespaces.Add(ns1);

            if (collection.ConfigurationInterfaceTypeName is { } configurationInterfaceTypeName &&
                GetNamespaceFromFullTypeName(configurationInterfaceTypeName) is { Length: > 0 } ns2)
                namespaces.Add(ns2);

            if (collection.ServiceInterfaceTypeName is { } serviceInterfaceTypeName &&
                GetNamespaceFromFullTypeName(serviceInterfaceTypeName) is { Length: > 0 } ns3)
                namespaces.Add(ns3);

            if (collection.ProviderTypeName is { } providerTypeName &&
                GetNamespaceFromFullTypeName(providerTypeName) is { Length: > 0 } ns4)
                namespaces.Add(ns4);
        }
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

        // Pending registrations list - uses non-generic IServiceType for cross-assembly compatibility
        bodySb.AppendLine("        // Pending registrations (before freeze)");
        bodySb.AppendLine("        private static readonly object _lookupGate = new();");
        bodySb.AppendLine();

        // Why: per-option / per-collection idempotence + order control now lives in the shared
        // ServiceTypePhaseState registry (keyed by the IServiceCollection / IServiceProvider scope, never
        // process-wide, so independent containers each get a full registration), covering all three phases
        // uniformly. The old per-collection _registeredOptionNames table is superseded by it.

        // Nullable frozen dictionaries
        bodySb.AppendLine("        // Frozen dictionaries (populated on first access)");
        bodySb.AppendLine("#if NETSTANDARD2_0");
        bodySb.AppendLine($"        private static ImmutableDictionary<Guid, {collection.InterfaceTypeName}>? _all;");
        bodySb.AppendLine("#else");
        bodySb.AppendLine($"        private static FrozenDictionary<Guid, {collection.InterfaceTypeName}>? _all;");
        bodySb.AppendLine("#endif");

        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            bodySb.AppendLine("#if NETSTANDARD2_0");
            bodySb.AppendLine($"        private static ImmutableDictionary<{prop.PropertyType}, {collection.InterfaceTypeName}>? {fieldName};");
            bodySb.AppendLine("#else");
            bodySb.AppendLine($"        private static FrozenDictionary<{prop.PropertyType}, {collection.InterfaceTypeName}>? {fieldName};");
            bodySb.AppendLine("#endif");
        }
        bodySb.AppendLine();

        // Why a [ModuleInitializer] and NOT a static constructor: the three phase methods live on
        // ServiceTypeCollectionBase, so a call like `ConnectionTypes.Register(builder)` binds to the
        // INHERITED static — and C# does not run the derived type's static constructor for that. The
        // registry would stay empty and every Get() would miss. A module initializer runs at assembly
        // load, before anything can read the collection, so registration cannot be skipped.
        //
        // Collecting service types is ALL this does; it does not touch the phase funcs. A collection's
        // Register body — including the domain provider its own declaration names — is set by the class
        // carrying [ServiceTypeCollection], in its static constructor, so the body is written where the
        // collection is declared and an application can replace it wholesale.
        bodySb.AppendLine("        [ModuleInitializer]");
        bodySb.AppendLine($"        internal static void RegisterDiscoveredOptions()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            // Register options discovered at compile time in this assembly");
        foreach (var option in options)
        {
            bodySb.AppendLine($"            RegisterMember(new {option.FullTypeName}());");
        }
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // EnsureFrozen method
        bodySb.AppendLine("        /// <summary>");
        bodySb.AppendLine("        /// Builds this collection's typed lookup dictionaries from the base registry.");
        bodySb.AppendLine("        /// </summary>");
        bodySb.AppendLine("        private static void EnsureLookups()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            if (_all != null) return;");
        bodySb.AppendLine();
        bodySb.AppendLine("            lock (_lookupGate)");
        bodySb.AppendLine("            {");
        bodySb.AppendLine("                if (_all != null) return;");
        bodySb.AppendLine();
        bodySb.AppendLine($"                var items = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<{collection.InterfaceTypeName}>(Options));");
        bodySb.AppendLine();
        bodySb.AppendLine("#if NETSTANDARD2_0");
        bodySb.AppendLine("                _all = items.ToImmutableDictionary(x => x.Id);");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            bodySb.AppendLine($"                {fieldName} = items.ToImmutableDictionary(x => x.{prop.PropertyName});");
        }
        bodySb.AppendLine("#else");
        bodySb.AppendLine("                _all = items.ToFrozenDictionary(x => x.Id);");
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";
            bodySb.AppendLine($"                {fieldName} = items.ToFrozenDictionary(x => x.{prop.PropertyName});");
        }
        bodySb.AppendLine("#endif");
        bodySb.AppendLine("            }");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // Find the ById lookup for static accessors
        var byIdLookup = lookupGroups.FirstOrDefault(g => string.Equals(g.Key, "ById", StringComparison.Ordinal));
        var byIdFieldName = byIdLookup != null ? $"_{CodeGeneration.ToCamelCase(byIdLookup.Key)}" : "_all";

        // Static property accessors (singleton) - only for compile-time discovered options
        foreach (var option in options)
        {
            var fieldName = $"_option{CodeGeneration.ToPascalCase(option.OptionName)}";
            bodySb.AppendLine($"        private static {option.FullTypeName}? {fieldName};");
            bodySb.AppendLine($"        /// <summary>Gets the {option.OptionName} singleton instance.</summary>");
            bodySb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            get");
            bodySb.AppendLine("            {");
            bodySb.AppendLine("                EnsureLookups();");
            bodySb.AppendLine($"                return {fieldName} ??= ({option.FullTypeName}){byIdFieldName}![new Guid(\"{option.GeneratedId}\")];");
            bodySb.AppendLine("            }");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();

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

                bodySb.AppendLine($"        /// <summary>Creates a new instance of {option.OptionName} with the specified parameters.</summary>");
                bodySb.AppendLine($"        public static {option.FullTypeName} {option.OptionName}({parameters}) =>");
                bodySb.AppendLine($"            new {option.FullTypeName}({arguments});");
                bodySb.AppendLine();
            }
        }

        // Lookup methods from [TypeLookup] - now call EnsureFrozen
        foreach (var group in lookupGroups)
        {
            var prop = group.First();
            var fieldName = $"_{CodeGeneration.ToCamelCase(group.Key)}";

            bodySb.AppendLine($"        /// <summary>Looks up a service type by {prop.PropertyName}. Returns NotFound if not found.</summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>ServiceTypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static {collection.InterfaceTypeName} {group.Key}({prop.PropertyType} value)");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            EnsureLookups();");
            bodySb.AppendLine($"            return {fieldName}!.TryGetValue(value, out var result) ? result : NotFound;");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // Generate fallback ByName method if provider is requested but no ByName lookup exists
        // This looks up by iterating _all and matching by Name since we don't have a dedicated _byName dictionary
        if (collection.GenerateProvider && !lookupGroups.Any(g => string.Equals(g.Key, "ByName", StringComparison.Ordinal)))
        {
            bodySb.AppendLine($"        /// <summary>Looks up a service type by name. Returns NotFound if not found.</summary>");
            bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>ServiceTypeCollectionGenerator</c>. To override, define a static method");
            bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            bodySb.AppendLine($"        public static {collection.InterfaceTypeName} ByName(string? name)");
            bodySb.AppendLine("        {");
            bodySb.AppendLine("            if (string.IsNullOrEmpty(name)) return NotFound;");
            bodySb.AppendLine("            EnsureLookups();");
            bodySb.AppendLine("            foreach (var item in _all!.Values)");
            bodySb.AppendLine("            {");
            bodySb.AppendLine("                if (string.Equals(item.Name, name, StringComparison.Ordinal))");
            bodySb.AppendLine("                    return item;");
            bodySb.AppendLine("            }");
            bodySb.AppendLine("            return NotFound;");
            bodySb.AppendLine("        }");
            bodySb.AppendLine();
        }

        // All method - now returns frozen dictionary after EnsureFrozen
        bodySb.AppendLine($"        /// <summary>Gets all registered service types keyed by Id.</summary>");
        bodySb.AppendLine($"        /// <remarks>Auto-generated by <c>ServiceTypeCollectionGenerator</c>. To override, define a static method");
        bodySb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
        bodySb.AppendLine("#if NETSTANDARD2_0");
        bodySb.AppendLine($"        public static ImmutableDictionary<Guid, {collection.InterfaceTypeName}> All()");
        bodySb.AppendLine("#else");
        bodySb.AppendLine($"        public static FrozenDictionary<Guid, {collection.InterfaceTypeName}> All()");
        bodySb.AppendLine("#endif");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            EnsureLookups();");
        bodySb.AppendLine("            return _all!;");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();

        // ServiceCategory property (if specified)
        if (!string.IsNullOrEmpty(collection.ServiceCategory))
        {
            bodySb.AppendLine("        /// <summary>");
            bodySb.AppendLine("        /// The service category for database configuration loading.");
            bodySb.AppendLine("        /// Used by MsSqlConfigurationSource to load configurations from cfg.* tables.");
            bodySb.AppendLine("        /// </summary>");
            bodySb.AppendLine($"        public static string ServiceCategory => \"{collection.ServiceCategory}\";");
            bodySb.AppendLine();
        }

        // GetMetadata + FNV-1a hash emission (extracted to keep GenerateCode under the FDW006 line budget)
        AppendMetadataMethod(bodySb, collection);

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

            // NotFound sentinel class using shared generation (ServiceTypeCollectionModel implements required interface)
            CodeGeneration.GenerateNotFoundSentinelForServiceType(bodySb, collection, abstractMembers, collection.InterfaceTypeName, userDeclaredMembers);
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

    // Emits this collection's phase-2 entry point, which adds the domain provider to DI and then runs
    // the base's Register — the option sweep, its logging and its run-order numbering.
    //
    // Why it shadows the inherited static rather than replacing RegisterFunc: the provider registration
    // is invariant. An application may legitimately replace the option sweep, and when it does the
    // provider must still be registered; a body swapped in via Registration(...) would take the provider
    // with it. Shadowing puts the invariant wiring OUTSIDE the swappable body, which is the same reason
    // ServiceTypeBase makes its option-level invokers virtual.
    //
    // Why `new` is safe here: every caller names the collection concretely — `ConnectionTypes.Register(...)`,
    // or a method group bound into ServiceTypeCollectionDescriptor — so the call binds to this method at
    // compile time. Nothing dispatches these phases through the base type.
    private static void AppendProviderRegisterOverride(StringBuilder bodySb, ServiceTypeCollectionModel collection)
    {
        bodySb.AppendLine("        /// <summary>Phase 2 — registers this domain's provider, then sweeps the options.</summary>");
        bodySb.AppendLine("        /// <param name=\"builder\">The host application builder.</param>");
        bodySb.AppendLine("        /// <param name=\"loggerFactory\">The host's logger factory, when one is available.</param>");
        bodySb.AppendLine("        /// <returns>The builder on success; a failure carrying the reason otherwise.</returns>");
        bodySb.AppendLine("        public static new Fdw.Results.IGenericResult<IHostApplicationBuilder> Register(");
        bodySb.AppendLine("            IHostApplicationBuilder builder,");
        bodySb.AppendLine("            ILoggerFactory? loggerFactory = null)");
        bodySb.AppendLine("        {");
        AppendScopedProviderRegistration(bodySb, collection, "AddScoped");
        bodySb.AppendLine($"            return ServiceTypeCollectionBase<{collection.BaseTypeName}, {collection.InterfaceTypeName}>.Register(builder, loggerFactory);");
        bodySb.AppendLine("        }");
        bodySb.AppendLine();
    }

    // Why: extracted from GenerateCode so that method stays within the FDW006 executable-line budget.
    // Emits the scoped-provider resolver lambda (shared by the Register and Configure emission paths).
    // The per-scope factory wiring is wrapped in a fail-loud try/catch: a throw here was previously
    // SILENT — the process went dark after the last provider Debug line (e.g. "Parent configuration
    // provider registered") with no exception logged — leaving a scoped-lifetime crash/freeze
    // impossible to diagnose. The catch logs the exception via MessageLogging and rethrows (mirrors
    // the singleton Initialize() path's log-and-rethrow), so the real cause surfaces.
    // <paramref name="addMethod"/> is "AddScoped" (Register path) or "TryAddScoped" (Configure path).
    private static void AppendScopedProviderRegistration(StringBuilder bodySb, ServiceTypeCollectionModel collection, string addMethod)
    {
        bodySb.AppendLine($"                builder.Services.{addMethod}<{collection.ProviderInterfaceTypeName}>(sp =>");
        bodySb.AppendLine($"            {{");
        bodySb.AppendLine($"                var provider = new {collection.ProviderTypeName}(");
        bodySb.AppendLine($"                    sp,");
        bodySb.AppendLine($"                    sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger<{collection.ProviderTypeName}>()");
        bodySb.AppendLine($"                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<{collection.ProviderTypeName}>.Instance);");
        bodySb.AppendLine($"                try");
        bodySb.AppendLine($"                {{");
        if (collection.ConfigurationTypeName != null)
        {
            bodySb.AppendLine($"                    if (sp.GetService<Fdw.Services.Abstractions.IServiceConfigurationProvider<{collection.ConfigurationTypeName}>>() is {{}} cfg) provider.Register(cfg);");
        }
        bodySb.AppendLine($"                }}");
        bodySb.AppendLine($"                catch (System.Exception ex)");
        bodySb.AppendLine($"                {{");
        bodySb.AppendLine($"                    var stLogger = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(\"{collection.ClassName}\");");
        bodySb.AppendLine($"                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, \"{collection.ClassName}\");");
        bodySb.AppendLine($"                    throw;");
        bodySb.AppendLine($"                }}");
        bodySb.AppendLine($"                return provider;");
        bodySb.AppendLine($"            }});");
    }

    // Why: extracted from GenerateCode so that method stays within the FDW006 executable-line budget.
    // Emits the GetMetadata() accessor plus the private ComputeFnv1aHash helper for one collection.
    private static void AppendMetadataMethod(StringBuilder bodySb, ServiceTypeCollectionModel collection)
    {
        bodySb.AppendLine("        private static Fdw.Types.TypeCollectionMetadata? _metadata;");
        bodySb.AppendLine();
        bodySb.AppendLine("        /// <summary>Gets metadata describing this ServiceTypeCollection.</summary>");
        bodySb.AppendLine("        public static Fdw.Types.TypeCollectionMetadata GetMetadata()");
        bodySb.AppendLine("        {");
        bodySb.AppendLine("            EnsureLookups();");
        bodySb.AppendLine("            return _metadata ??= new Fdw.Types.TypeCollectionMetadata");
        bodySb.AppendLine("            {");
        bodySb.AppendLine($"                Id = ComputeFnv1aHash(\"{collection.FullName}\"),");
        bodySb.AppendLine($"                Name = \"{collection.ClassName}\",");
        bodySb.AppendLine($"                FullName = \"{collection.FullName}\",");

        // Map internal CollectionKind to Fdw.Types.CollectionKinds TypeCollection
        // ServiceType collections are always Service or MutableService
        // Pragma: source generators cannot use TypeCollections (bootstrapping problem)
#pragma warning disable FDW018
        var collectionKindValue = collection.Kind switch
        {
            CollectionKind.Immutable => "Fdw.Types.CollectionKinds.Service",
            CollectionKind.Mutable => "Fdw.Types.CollectionKinds.MutableService",
            _ => "Fdw.Types.CollectionKinds.Service"
        };
#pragma warning restore FDW018
        bodySb.AppendLine($"                CollectionKind = {collectionKindValue},");

        // ServiceCategory (if specified)
        if (!string.IsNullOrEmpty(collection.ServiceCategory))
        {
            bodySb.AppendLine($"                ServiceCategory = \"{collection.ServiceCategory}\",");
        }

        bodySb.AppendLine("                Options = _all!.Values.Select(o => new Fdw.Types.TypeOptionMetadata");
        bodySb.AppendLine("                {");
        bodySb.AppendLine("                    Id = ComputeFnv1aHash(o.Name),");
        bodySb.AppendLine("                    Name = o.Name,");
        bodySb.AppendLine($"                    TypeCollectionId = ComputeFnv1aHash(\"{collection.FullName}\"),");
        bodySb.AppendLine("                    FullTypeName = o.GetType().FullName ?? o.GetType().Name");
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
    }

#pragma warning disable MA0051 // Source generator emits complete provider class — splitting scatters the template
    private static string GenerateProviderCode(
        ServiceTypeCollectionModel collection,
        ImmutableArray<ServiceTypeOptionModel> options)
    {
        var sb = new StringBuilder();
        var providerName = collection.ClassName.Replace("Types", "FactoryProvider");
        var interfaceName = $"I{providerName}";

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Microsoft.Extensions.Configuration;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Hosting;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();
        sb.AppendLine($"namespace {collection.Namespace}");
        sb.AppendLine("{");

        // Generate interface
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Provides factory resolution for {collection.ClassName} by configuration name.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public interface {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Gets a factory for the specified configuration name.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        /// <typeparam name=\"TFactory\">The factory type to resolve.</typeparam>");
        sb.AppendLine($"        /// <param name=\"configurationName\">The name of the configuration section.</param>");
        sb.AppendLine($"        /// <returns>The factory instance, or null if not found.</returns>");
        sb.AppendLine($"        TFactory? GetFactory<TFactory>(string configurationName) where TFactory : class;");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Gets all configured connection names.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        IReadOnlyCollection<string> GetConfiguredNames();");
        sb.AppendLine();

        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate implementation
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Default implementation of {interfaceName}.");
        sb.AppendLine($"    /// </summary>");
        // Why type-level here: wholly generated, no hand-written partial part to collaterally exclude.
        sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.AppendLine($"    public sealed class {providerName} : {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IServiceProvider _serviceProvider;");
        sb.AppendLine("        private readonly IConfiguration _configuration;");
        sb.AppendLine($"        private readonly ILogger<{providerName}> _logger;");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Initializes a new instance of the <see cref=\"{providerName}\"/> class.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {providerName}(");
        sb.AppendLine("            IServiceProvider serviceProvider,");
        sb.AppendLine("            IConfiguration configuration,");
        sb.AppendLine($"            ILogger<{providerName}> logger)");
        sb.AppendLine("        {");
        sb.AppendLine("            _serviceProvider = serviceProvider;");
        sb.AppendLine("            _configuration = configuration;");
        sb.AppendLine("            _logger = logger;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // GetFactory method
        sb.AppendLine($"        /// <inheritdoc />");
        sb.AppendLine($"        public TFactory? GetFactory<TFactory>(string configurationName) where TFactory : class");
        sb.AppendLine("        {");
        sb.AppendLine($"            var section = _configuration.GetSection(configurationName);");
        sb.AppendLine("            if (!section.Exists())");
        sb.AppendLine("            {");
        sb.AppendLine($"                _logger.LogWarning(\"Configuration section '{{Name}}' not found\", configurationName);");
        sb.AppendLine("                return null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            var typeName = section[\"Type\"];");
        sb.AppendLine("            if (string.IsNullOrEmpty(typeName))");
        sb.AppendLine("            {");
        sb.AppendLine($"                _logger.LogWarning(\"Configuration section '{{Name}}' missing 'Type' property\", configurationName);");
        sb.AppendLine("                return null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        // Why this throws rather than warning and returning null: an unrecognized discriminator is a
        // configuration DEFECT, not "no service configured". ConnectionConfigurationJsonConverter
        // already treats the identical condition as fatal at schema load, and warn-and-null left the
        // same class of typo to surface later as an unrelated null-service fault. Nothing legitimately
        // probes this — GetFactory has no call sites outside this generator — so there is no caller
        // whose null-tolerance is being taken away.
        //
        // Why the registered set is in the message: the overwhelmingly common cause of a miss is a
        // missing package reference whose module initializer never ran, and the registered set is the
        // single datum that distinguishes that from a plain typo. An EMPTY set is itself the
        // diagnosis — it means no module initializer ran at all.
        //
        // Why the set is built with a foreach rather than LINQ: this file's emitted usings are fixed
        // (System, System.Collections.Generic, and the Microsoft.Extensions.* set) and deliberately
        // exclude System.Linq — GetConfiguredNames below builds its list the same way.
        sb.AppendLine($"            var serviceType = {collection.ClassName}.ByName(typeName);");
        sb.AppendLine($"            if (serviceType == {collection.ClassName}.NotFound)");
        sb.AppendLine("            {");
        sb.AppendLine("                var registeredTypeNames = new List<string>();");
        sb.AppendLine($"                foreach (var registeredType in {collection.ClassName}.All().Values)");
        sb.AppendLine("                {");
        sb.AppendLine("                    registeredTypeNames.Add(registeredType.Name);");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                throw new InvalidOperationException(");
        sb.AppendLine("                    \"Unknown service type '\" + typeName + \"' in configuration '\" + configurationName");
        sb.AppendLine($"                    + \"'. Registered {collection.ClassName} service types: \"");
        sb.AppendLine("                    + (registeredTypeNames.Count == 0");
        sb.AppendLine("                        ? \"(none - no module initializer has registered any option)\"");
        sb.AppendLine("                        : string.Join(\", \", registeredTypeNames))");
        sb.AppendLine("                    + \". Reference the package that provides that [ServiceTypeOption] so its module\"");
        sb.AppendLine("                    + \" initializer registers it before configuration is loaded.\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return _serviceProvider.GetService<TFactory>();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // GetConfiguredNames method
        sb.AppendLine($"        /// <inheritdoc />");
        sb.AppendLine("        public IReadOnlyCollection<string> GetConfiguredNames()");
        sb.AppendLine("        {");
        sb.AppendLine("            var names = new List<string>();");
        sb.AppendLine($"            foreach (var type in {collection.ClassName}.All().Values)");
        sb.AppendLine("            {");
        sb.AppendLine("                var section = _configuration.GetSection(type.Name);");
        sb.AppendLine("                if (section.Exists())");
        sb.AppendLine("                {");
        sb.AppendLine("                    names.Add(type.Name);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            return names;");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate extension method for registration
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Extension methods for registering {providerName}.");
        sb.AppendLine($"    /// </summary>");
        // Why type-level here: wholly generated, no hand-written partial part to collaterally exclude.
        sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.AppendLine($"    public static class {providerName}Extensions");
        sb.AppendLine("    {");
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Adds the {interfaceName} to the service collection.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static IHostApplicationBuilder Add{providerName}(this IHostApplicationBuilder builder)");
        sb.AppendLine("        {");
        sb.AppendLine($"            builder.Services.AddSingleton<{interfaceName}, {providerName}>();");
        sb.AppendLine($"            {collection.ClassName}.Register(builder);");
        sb.AppendLine("            return builder;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Extracts the namespace from a fully qualified type name.
    /// </summary>
    private static string? GetNamespaceFromFullTypeName(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
            return null;

        // Handle generic types - take the part before '<'
        var genericIndex = fullTypeName.IndexOf('<');
        var typeName = genericIndex > 0 ? fullTypeName.Substring(0, genericIndex) : fullTypeName;

        // Find the last dot to separate namespace from type name
        var lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex <= 0)
            return null;

        return typeName.Substring(0, lastDotIndex);
    }

    private static HashSet<string>? DetectUserDeclaredNotFoundMembers(Compilation compilation, ServiceTypeCollectionModel collection)
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
