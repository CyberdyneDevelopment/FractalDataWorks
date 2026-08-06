using System.ComponentModel.DataAnnotations;
using Fdw.Configuration;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$.$implName$;

/// <summary>
/// Configuration for $implName$ $serviceName$ services.
/// </summary>
public sealed class $implName$$serviceName$Configuration : ConfigurationBase<$implName$$serviceName$Configuration>, I$serviceName$Configuration
{
    public static string SectionName => "Services:$serviceName$:$implName$";

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // TODO: Add implementation-specific configuration properties
}
