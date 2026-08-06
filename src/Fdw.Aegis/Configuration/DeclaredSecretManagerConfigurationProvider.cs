using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// Read-only <see cref="IServiceConfigurationProvider{TConfig}"/> over the
/// <c>SecretManagers</c> block declared in <c>aegisSchema.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists: <c>ISecretManagerProvider.Get(string name, ct)</c> resolves through the domain
/// provider's PARENT configuration provider — that is the seam that turns a logical name into a
/// configuration record. In a normal host that parent reads ConfigurationDb; the Aegis gateway is
/// deliberately ConfigurationDb-free, so it registers this in-memory parent instead. The resolution
/// path is otherwise identical, which is the point: <see cref="AegisInjector"/> asks the provider by
/// name and stays ignorant of both the secret SOURCE kind and where its configuration came from.
/// </para>
/// <para>
/// Writes fail loud rather than silently no-op: the declared schema is a read-only startup input, so
/// a caller reaching Save/Delete has mistaken this for a mutable store and must be told.
/// </para>
/// </remarks>
public sealed class DeclaredSecretManagerConfigurationProvider : IServiceConfigurationProvider<SecretManagerConfiguration>
{
    private readonly IReadOnlyList<SecretManagerConfiguration> _declared;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredSecretManagerConfigurationProvider"/> class.
    /// </summary>
    /// <param name="declared">The secret managers declared by the loaded schema.</param>
    public DeclaredSecretManagerConfigurationProvider(IReadOnlyList<SecretManagerConfiguration> declared)
        => _declared = declared ?? throw new ArgumentNullException(nameof(declared));

    /// <inheritdoc />
    public Task<IGenericResult<SecretManagerConfiguration>> Get(string name, CancellationToken ct = default)
    {
        for (var i = 0; i < _declared.Count; i++)
        {
            if (string.Equals(_declared[i].Name, name, StringComparison.Ordinal))
                return Task.FromResult<IGenericResult<SecretManagerConfiguration>>(
                    GenericResult<SecretManagerConfiguration>.Success(_declared[i]));
        }

        return Task.FromResult<IGenericResult<SecretManagerConfiguration>>(
            GenericResult<SecretManagerConfiguration>.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("SecretManagerName", name)));
    }

    /// <inheritdoc />
    public Task<IGenericResult<SecretManagerConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        for (var i = 0; i < _declared.Count; i++)
        {
            if (_declared[i].Id == id)
                return Task.FromResult<IGenericResult<SecretManagerConfiguration>>(
                    GenericResult<SecretManagerConfiguration>.Success(_declared[i]));
        }

        return Task.FromResult<IGenericResult<SecretManagerConfiguration>>(
            GenericResult<SecretManagerConfiguration>.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("SecretManagerId", id.ToString())));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<SecretManagerConfiguration>>> Get(CancellationToken ct = default)
        => Task.FromResult<IGenericResult<IReadOnlyList<SecretManagerConfiguration>>>(
            GenericResult<IReadOnlyList<SecretManagerConfiguration>>.Success(_declared));

    /// <inheritdoc />
    public async Task<IGenericResult<IGenericConfiguration>> GetUntyped(Guid id, CancellationToken ct = default)
    {
        var result = await Get(id, ct).ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? GenericResult<IGenericConfiguration>.Success(result.Value)
            : result.ToNewResult<IGenericConfiguration>();
    }

    /// <inheritdoc />
    public Task<IGenericResult<SecretManagerConfiguration>> Save(SecretManagerConfiguration record, CancellationToken ct = default)
        => Task.FromResult<IGenericResult<SecretManagerConfiguration>>(
            GenericResult<SecretManagerConfiguration>.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Operation", nameof(Save))));

    /// <inheritdoc />
    public Task<IGenericResult> SaveUntyped(IGenericConfiguration record, CancellationToken ct = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Operation", nameof(SaveUntyped))));

    /// <inheritdoc />
    public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Operation", nameof(Delete))));

    /// <inheritdoc />
    public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Operation", nameof(Delete))));

    /// <inheritdoc />
    public Task<IGenericResult> DeleteUntyped(Guid id, CancellationToken ct = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Operation", nameof(DeleteUntyped))));
}
