using System.Collections.Generic;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Analyzed model of a configuration class for code generation.
/// </summary>
public sealed class ConfigurationModel
{
    /// <summary>
    /// Gets or sets the namespace of the configuration class.
    /// </summary>
    public string Namespace { get; set; } = "";

    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// Gets or sets the full type name including namespace.
    /// </summary>
    public string FullTypeName { get; set; } = "";

    /// <summary>
    /// Gets or sets the properties of the configuration class.
    /// </summary>
    public IList<PropertyModel> Properties { get; set; } = new List<PropertyModel>();

    /// <summary>
    /// Gets or sets the explicit parent table name for shared primary key pattern.
    /// When set, the child table's Id column becomes both primary key and foreign key to the parent table.
    /// </summary>
    /// <remarks>
    /// This creates a 1:1 relationship where the child's Id references the parent's Id directly.
    /// When set:
    /// - Child's Id column is both PK and FK to parent
    /// - Name column is NOT generated (inherited from parent)
    /// - No separate FK column is needed
    /// </remarks>
    public string? ParentTableName { get; set; }

    /// <summary>
    /// Gets or sets the schema of the parent table.
    /// Defaults to the same value as <see cref="Schema"/> if not specified.
    /// </summary>
    public string? ParentSchema { get; set; }

    /// <summary>
    /// Gets or sets the explicit foreign key column name for the parent relationship.
    /// If specified, overrides the default {ParentTableName}Id naming convention.
    /// </summary>
    public string? ExplicitParentForeignKeyColumn { get; set; }

    /// <summary>
    /// Gets or sets the foreign key property name to parent.
    /// </summary>
    public string? ParentFkProperty { get; set; }

    /// <summary>
    /// Gets or sets whether the parent class also has [ManagedConfiguration] attribute.
    /// Used to determine if 'new' keyword is needed for GetDdlDefinition method.
    /// </summary>
    public bool ParentHasManagedConfiguration { get; set; }

    /// <summary>
    /// Gets or sets whether to generate DDL definition. Defaults to true.
    /// </summary>
    public bool GenerateDdl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate FluentValidation validator. Defaults to true.
    /// </summary>
    public bool GenerateValidator { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate UI form models. Defaults to true.
    /// </summary>
    public bool GenerateUi { get; set; } = true;

    /// <summary>
    /// Gets or sets the service category from [ManagedConfiguration].
    /// </summary>
    public string? ServiceCategory { get; set; }

    /// <summary>
    /// Gets or sets the service type from [ManagedConfiguration].
    /// </summary>
    public string? ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the schema name for the configuration table.
    /// Defaults to "cfg" if not specified.
    /// </summary>
    public string Schema { get; set; } = "cfg";

    /// <summary>
    /// Gets or sets the table name override.
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the display name for UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description for UI.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the foreign key delete behavior.
    /// </summary>
    public string OnDelete { get; set; } = "Cascade";

    /// <summary>
    /// Gets or sets the database provider.
    /// </summary>
    public string DatabaseProvider { get; set; } = "MsSql";

    /// <summary>
    /// Gets or sets whether this configuration is effective-dated (valid-time versioned).
    /// When true the DDL carries EffectiveStart/EffectiveEnd plus an as-of lookup index.
    /// </summary>
    public bool Temporal { get; set; }

    /// <summary>
    /// Gets or sets the property name on the parent configuration class that holds
    /// this child's collection. Used for hierarchical nested configuration loading.
    /// </summary>
    public string? ParentCollectionProperty { get; set; }

    /// <summary>
    /// Gets the effective table name (explicit or derived from class name).
    /// Strips "ConfigurationBase" or "Configuration" suffix from class name.
    /// </summary>
    public string GetEffectiveTableName()
    {
        if (!string.IsNullOrEmpty(TableName))
            return TableName!;

        var name = ClassName;

        // Strip "ConfigurationBase" first (for base classes like ConnectionConfigurationBase)
        if (name.EndsWith("ConfigurationBase", System.StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - 17); // "ConfigurationBase".Length = 17
        }
        // Then try "Configuration" (for concrete classes like MsSqlConnectionConfiguration)
        else if (name.EndsWith("Configuration", System.StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - 13); // "Configuration".Length = 13
        }

        return name;
    }

    /// <summary>
    /// Gets the effective display name (explicit or derived from class name).
    /// </summary>
    public string GetEffectiveDisplayName()
    {
        if (!string.IsNullOrEmpty(DisplayName))
            return DisplayName!;

        return ClassName;
    }

    /// <summary>
    /// Gets the FK column name for referencing the parent table (e.g., "ConnectionId").
    /// Uses explicit ParentForeignKeyColumn if specified, otherwise defaults to {ParentTableName}Id.
    /// The SqlServerConfigurationProvider hierarchy loading uses this to match parent Id → child FK.
    /// </summary>
    public string? GetParentForeignKeyColumn()
    {
        if (string.IsNullOrEmpty(ParentTableName))
            return null;

        // Use explicit FK column if specified (e.g., for shared PK patterns where FK is "Id")
        if (!string.IsNullOrEmpty(ExplicitParentForeignKeyColumn))
            return ExplicitParentForeignKeyColumn;

        // Default: append "Id" to parent table name
        return ParentTableName + "Id";
    }
}
