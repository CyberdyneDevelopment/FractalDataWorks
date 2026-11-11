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
    protected EtlRowMapperConfiguration()
    {
    }

    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the mapper's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the mapper kind this record is. EtlMapper has no domain record to state it, so the
    /// option states its own.
    /// </summary>
    public virtual string MapperType => string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether connection pooling is enabled for this mapper.
    /// Left unstated, the mapper type's declared value applies (FDW-3956).
    /// </summary>
    public bool? EnablePooling { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pooled mapper instances.
    /// Left unstated, the mapper type's declared value applies (FDW-3956).
    /// </summary>
    public int? MaxPoolSize { get; set; }

}
