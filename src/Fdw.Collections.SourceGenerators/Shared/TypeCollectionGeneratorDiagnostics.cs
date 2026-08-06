using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Shared;

#pragma warning disable RS2000 // Add analyzer diagnostic IDs to analyzer release

/// <summary>
/// Diagnostic descriptors for TypeCollection generators.
/// </summary>
internal static class TypeCollectionGeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor IdHashCollision = new(
        id: "TC001",
        title: "Id Hash Collision",
        messageFormat: "Collection '{0}' has types with same generated Id ({1}): {2}",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multiple TypeOptions have the same auto-generated Id hash. Consider using explicit Ids.");

    public static readonly DiagnosticDescriptor InterfaceNotImplemented = new(
        id: "TC002",
        title: "Interface Not Implemented",
        messageFormat: "Type '{0}' must implement '{1}' to be a TypeOption of '{2}'",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypeOption must implement the interface specified in the TypeCollection attribute.");

    public static readonly DiagnosticDescriptor NoTypeOptionsFound = new(
        id: "TC003",
        title: "No TypeOptions Found",
        messageFormat: "TypeCollection '{0}' has no TypeOptions",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The TypeCollection has no discovered TypeOptions. Ensure TypeOptions reference this collection.");

    public static readonly DiagnosticDescriptor CollectionNotFound = new(
        id: "TC004",
        title: "Collection Not Found",
        messageFormat: "TypeOption '{0}' references collection '{1}' which was not found",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The TypeOption references a TypeCollection that doesn't exist or isn't accessible.");

    public static readonly DiagnosticDescriptor NoLookupProperties = new(
        id: "TC005",
        title: "No TypeLookup Properties",
        messageFormat: "TypeCollection '{0}' base type has no [TypeLookup] properties - only ById() and ByName() will be generated",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Consider adding [TypeLookup] attributes to base class properties for custom lookup methods.");

    public static readonly DiagnosticDescriptor DuplicateOptionName = new(
        id: "TC007",
        title: "Duplicate Option Name",
        messageFormat: "Collection '{0}' has multiple TypeOptions named '{1}': {2}",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypeOption names must be unique within a collection.");

    public static readonly DiagnosticDescriptor DuplicateLookupValue = new(
        id: "TC008",
        title: "Duplicate Lookup Value",
        messageFormat: "Collection '{0}' has multiple TypeOptions with {1} = '{2}': {3}",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypeLookup properties must have unique values within a collection to enable dictionary-based lookup.");

    // ServiceType-specific diagnostics
    public static readonly DiagnosticDescriptor ServiceTypeIdCollision = new(
        id: "ST001",
        title: "ServiceType Id Collision",
        messageFormat: "ServiceTypeCollection '{0}' has types with same generated Id ({1}): {2}",
        category: "ServiceType",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multiple ServiceTypeOptions have the same auto-generated Id. This should be extremely rare with Guid.");

    public static readonly DiagnosticDescriptor ServiceTypeInterfaceNotImplemented = new(
        id: "ST002",
        title: "ServiceType Interface Not Implemented",
        messageFormat: "ServiceType '{0}' must implement both base class and interface (dual inheritance pattern)",
        category: "ServiceType",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ServiceTypes require dual inheritance - both base class and interface must be implemented.");

    public static readonly DiagnosticDescriptor NoServiceTypeOptionsFound = new(
        id: "ST003",
        title: "No ServiceTypeOptions Found",
        messageFormat: "ServiceTypeCollection '{0}' has no ServiceTypeOptions",
        category: "ServiceType",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The ServiceTypeCollection has no discovered ServiceTypeOptions.");

    public static readonly DiagnosticDescriptor DuplicateServiceTypeName = new(
        id: "ST004",
        title: "Duplicate ServiceType Name",
        messageFormat: "ServiceTypeCollection '{0}' has multiple ServiceTypeOptions named '{1}': {2}",
        category: "ServiceType",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ServiceTypeOption names must be unique within a collection.");

    public static readonly DiagnosticDescriptor DuplicateServiceTypeLookupValue = new(
        id: "ST005",
        title: "Duplicate ServiceType Lookup Value",
        messageFormat: "ServiceTypeCollection '{0}' has multiple ServiceTypeOptions with {1} = '{2}': {3}",
        category: "ServiceType",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypeLookup properties must have unique values within a collection to enable dictionary-based lookup.");

    // [Replaces] attribute diagnostics
    public static readonly DiagnosticDescriptor DuplicateReplacesTarget = new(
        id: "TC010",
        title: "Duplicate [Replaces] Target",
        messageFormat: "Multiple types replace '{0}': {1}. Only one replacement per original type is allowed.",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Two or more types declare [Replaces] targeting the same original type. Only one replacement is allowed.");

    public static readonly DiagnosticDescriptor ReplacedTypeNotFound = new(
        id: "TC011",
        title: "Replaced Type Not Found",
        messageFormat: "Type '{0}' declares [Replaces(typeof({1}))], but '{1}' was not found as a TypeOption in any referenced assembly. The replacement will be registered anyway.",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The type targeted by [Replaces] was not found. The replacement type will still be registered.");

    // Empty sentinel diagnostics
    public static readonly DiagnosticDescriptor UnknownConstructorParameterType = new(
        id: "TC009",
        title: "Unknown Constructor Parameter Type",
        messageFormat: "TypeCollection '{0}' base class has constructor parameter '{1}' of type '{2}' which cannot be safely defaulted for the Empty sentinel. Consider adding a protected parameterless constructor or using a nullable type.",
        category: "TypeCollection",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Empty sentinel class needs to call the base constructor, but reference types without known defaults will be passed as null, which may cause runtime errors if the constructor validates for null.");
}
