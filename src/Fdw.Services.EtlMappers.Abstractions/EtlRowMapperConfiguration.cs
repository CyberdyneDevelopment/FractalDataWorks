using Fdw.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.EtlMappers.Abstractions;

/// <summary>
/// Base configuration class for ETL row mappers.
/// </summary>
[ExcludeFromCodeCoverage]
public class EtlRowMapperConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlRowMapperConfiguration"/> class.
    /// Default constructor for IOptions binding.
    /// </summary>
    protected EtlRowMapperConfiguration() : this("EtlMapper", null, "EtlMappers")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlRowMapperConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "EtlMapper".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "Pooled", "Dynamic").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected EtlRowMapperConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName { get; set; }

    /// <inheritdoc />
    public string ServiceType { get; set; }

    /// <inheritdoc />
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the mapper type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public virtual string MapperType => ServiceOptionType ?? string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether connection pooling is enabled for this mapper.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of pooled mapper instances.
    /// Defaults to 1000.
    /// </summary>
    public int MaxPoolSize { get; set; } = 1000;

}
