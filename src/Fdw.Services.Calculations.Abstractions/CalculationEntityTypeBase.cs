using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Abstract base class for all calculation entity types.
/// Uses MD5-based deterministic Guid for stable cross-assembly identity.
/// </summary>
public abstract class CalculationEntityTypeBase :
    TypeOptionBase<Guid, CalculationEntityTypeBase>,
    ICalculationEntityType
{
    /// <summary>
    /// Initializes a new instance of <see cref="CalculationEntityTypeBase"/>.
    /// </summary>
    /// <param name="name">The unique type name used as the registry key.</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="description">Description of what this calculation type does.</param>
    protected CalculationEntityTypeBase(string name, string displayName, string description)
        : base(GenerateId(name), name)
    {
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Gets the human-readable display name for this calculation entity type.
    /// </summary>
    public new string DisplayName { get; }

    /// <summary>
    /// Gets the description of this calculation entity type.
    /// </summary>
    public new string Description { get; }

    /// <inheritdoc />
    public abstract Type ConfigurationType { get; }

    /// <summary>
    /// Gets the container name for loading this type's configuration record from ConfigurationDb.
    /// Returns <c>null</c> when the type has no additional typed configuration table.
    /// </summary>
    public virtual string? TypedContainerName => null;

    /// <summary>
    /// Builds a typed configuration from a raw node configuration dictionary.
    /// Base returns <c>null</c> for types with no typed configuration table.
    /// </summary>
    public virtual IGenericConfiguration? CreateTypedConfiguration(
        IReadOnlyDictionary<string, object?> nodeConfiguration, Guid entityId)
        => null;

    /// <inheritdoc />
    public abstract void Configure(IServiceCollection services, IConfiguration configuration);

    /// <inheritdoc />
    public abstract IGenericResult ValidateConfiguration(IGenericConfiguration configuration);

    /// <inheritdoc />
    public abstract Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);

    private static Guid GenerateId(string name)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
#if NETSTANDARD2_0
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(nameBytes);
#else
        var hash = SHA256.HashData(nameBytes);
#endif
        // Take first 16 bytes of SHA256 for a deterministic stable Guid
        var guidBytes = new byte[16];
        Buffer.BlockCopy(hash, 0, guidBytes, 0, 16);
        return new Guid(guidBytes);
    }
}
