using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Analyzes configuration classes marked with [ManagedConfiguration].
/// </summary>
public static class ConfigurationAnalyzer
{
    /// <summary>
    /// Analyzes a configuration class from generator attribute syntax context.
    /// </summary>
    public static ConfigurationModel? Analyze(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute == null)
            return null;

        return AnalyzeWithAttribute(classSymbol, attribute);
    }

    /// <summary>
    /// Analyzes a configuration class and creates a configuration model.
    /// </summary>
    public static ConfigurationModel AnalyzeWithAttribute(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        var model = new ConfigurationModel
        {
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name,
            FullTypeName = classSymbol.ToDisplayString()
        };

        // Extract values from [ManagedConfiguration] attribute
        ExtractAttributeValues(attribute, model);

        // Detect whether the base class also has [ManagedConfiguration].
        // Why: If the parent class generates GetDdlDefinition(), the child must emit
        // 'public new static' to avoid CS0108. This is a Roslyn-level check — the
        // attribute no longer carries this info since IDataNode now owns hierarchy.
        model.ParentHasManagedConfiguration = BaseClassHasManagedConfiguration(classSymbol);

        // Infer service category/type from class name if not specified
        InferServiceMetadata(model);

        // Analyze all public properties with getters and setters, walking the base-type
        // chain so a [ManagedConfiguration] class that inherits a plain (non-[ManagedConfiguration])
        // base (e.g. HttpConnectionConfiguration : HttpConnectionConfigurationBase) still picks up
        // the base's declared columns. See CollectInheritedProperties for the walk + stop rule.
        var allProperties = CollectInheritedProperties(classSymbol);

        // For DDL generation, exclude [NotMapped] properties
        var properties = allProperties.Where(p => !p.ExcludeFromDdl).ToList();
        model.Properties = properties;

        return model;
    }

    private static void ExtractAttributeValues(AttributeData attribute, ConfigurationModel model)
    {
        foreach (var namedArg in attribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "DisplayName":
                    model.DisplayName = namedArg.Value.Value as string;
                    break;
                case "Description":
                    model.Description = namedArg.Value.Value as string;
                    break;
                case "ServiceCategory":
                    model.ServiceCategory = namedArg.Value.Value as string;
                    break;
                case "ServiceType":
                    model.ServiceType = namedArg.Value.Value as string;
                    break;
                case "GenerateDdl":
                    model.GenerateDdl = namedArg.Value.Value is bool genDdl && genDdl;
                    break;
                case "GenerateValidator":
                    model.GenerateValidator = namedArg.Value.Value is bool genValidator && genValidator;
                    break;
                case "GenerateUi":
                    model.GenerateUi = namedArg.Value.Value is bool genUi && genUi;
                    break;
                case "OnDelete":
                    if (namedArg.Value.Value is string onDelete && !string.IsNullOrEmpty(onDelete))
                        model.OnDelete = onDelete;
                    break;
                case "DatabaseProvider":
                    if (namedArg.Value.Value is string dbProvider && !string.IsNullOrEmpty(dbProvider))
                        model.DatabaseProvider = dbProvider;
                    break;
                case "Temporal":
                    model.Temporal = namedArg.Value.Value is bool temporal && temporal;
                    break;
            }
        }
    }

    /// <summary>
    /// Collects public get/set scalar properties declared on <paramref name="classSymbol"/> AND any
    /// ancestor in its base-type chain, stopping at (and excluding) the first ancestor that itself
    /// carries [ManagedConfiguration] — that ancestor's properties belong to its own separate parent
    /// table (e.g. Connection vs HttpConnection), not this DDL.
    /// </summary>
    /// <remarks>
    /// Why: a [ManagedConfiguration] class commonly inherits a plain typed-body base (e.g.
    /// HttpConnectionConfiguration : HttpConnectionConfigurationBase) that declares the real columns
    /// (BaseUrl, Protocol, TimeoutSeconds, ConnectionId, …). The original GetMembers()-only enumeration
    /// only saw declared members, silently dropping every inherited column. This mirrors the walk shape
    /// of <see cref="BaseClassHasManagedConfiguration"/> but collects properties instead of testing for
    /// the attribute, and de-dups by name so a more-derived override wins over its base declaration.
    /// </remarks>
    private static List<PropertyModel> CollectInheritedProperties(INamedTypeSymbol classSymbol)
    {
        var seenPropertyNames = new HashSet<string>(System.StringComparer.Ordinal);
        var properties = new List<PropertyModel>();
        var current = classSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            // Why: stop before walking into a base that is itself [ManagedConfiguration] — its
            // columns belong to that base's own DDL/parent table, never this child's.
            if (!ReferenceEquals(current, classSymbol) && HasManagedConfigurationAttribute(current))
                break;

            foreach (var p in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (p.DeclaredAccessibility != Accessibility.Public ||
                    p.GetMethod == null ||
                    p.SetMethod == null ||
                    p.IsIndexer)
                    continue;

                // De-dup by name: the most-derived declaration (an override) wins over the
                // same-named base declaration encountered later in the walk.
                if (!seenPropertyNames.Add(p.Name))
                    continue;

                properties.Add(PropertyAnalyzer.Analyze(p));
            }

            current = current.BaseType;
        }

        return properties;
    }

    /// <summary>
    /// Returns true if <paramref name="typeSymbol"/> itself (not its base chain) carries [ManagedConfiguration].
    /// </summary>
    private static bool HasManagedConfigurationAttribute(INamedTypeSymbol typeSymbol)
    {
        const string attributeName = "ManagedConfigurationAttribute";
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (string.Equals(attr.AttributeClass?.Name, attributeName, System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if any class in the base class chain (excluding object) has [ManagedConfiguration].
    /// </summary>
    /// <remarks>
    /// Why: When both parent and child carry [ManagedConfiguration], both generate GetDdlDefinition().
    /// The child must emit 'public new static' to suppress CS0108. This replaces the old
    /// ParentHasManagedConfiguration attribute property — now derived from Roslyn symbol analysis
    /// because IDataNode owns structural metadata, not [ManagedConfiguration].
    /// </remarks>
    private static bool BaseClassHasManagedConfiguration(INamedTypeSymbol classSymbol)
    {
        const string attributeName = "ManagedConfigurationAttribute";
        var baseType = classSymbol.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            foreach (var attr in baseType.GetAttributes())
            {
                if (string.Equals(attr.AttributeClass?.Name, attributeName, System.StringComparison.Ordinal))
                    return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static void InferServiceMetadata(ConfigurationModel model)
    {
        var className = model.ClassName;

        // Infer ServiceCategory from suffix (e.g., WorkflowConfiguration -> Workflow)
        if (string.IsNullOrEmpty(model.ServiceCategory))
        {
            if (className.EndsWith("Configuration", System.StringComparison.Ordinal))
            {
                var nameWithoutSuffix = className.Substring(0, className.Length - 13);

                // Check for common category suffixes
                var categories = new[] { "Connection", "Workflow", "Authentication", "Storage", "Notification" };
                foreach (var category in categories)
                {
                    if (nameWithoutSuffix.EndsWith(category, System.StringComparison.Ordinal))
                    {
                        model.ServiceCategory = category;
                        break;
                    }
                }
            }
        }

        // Infer ServiceType from prefix (e.g., MsSqlConnectionConfiguration -> MsSql)
        if (string.IsNullOrEmpty(model.ServiceType) && !string.IsNullOrEmpty(model.ServiceCategory))
        {
            var categoryIndex = className.IndexOf(model.ServiceCategory, System.StringComparison.Ordinal);
            if (categoryIndex > 0)
            {
                model.ServiceType = className.Substring(0, categoryIndex);
            }
        }
    }
}
