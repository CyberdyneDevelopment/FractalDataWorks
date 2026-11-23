using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// The descriptions services of a test of the authorization providers that is not about descriptions (FDW-6208): the providers fill and write a description on every read and save, and
/// this double holds none: a fill puts no text on a thing, a write and a removal succeed and keep nothing. Only what those paths call is answered; anything else is a test that needs
/// the real service, so it is refused.
/// </summary>
internal sealed class NoDescriptions : IDescriptionReader, IDescriptionWriter
{
    /// <inheritdoc />
    public Task<IGenericResult> Fill<T>(
        string type, string subType, IReadOnlyList<T> things, Func<T, Guid> idOf, Action<T, string> fill, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult.Success());

    /// <inheritdoc />
    public Task<IGenericResult> Describe(
        string type, string subType, Guid thingId, string thingName, string? text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult.Success());

    /// <inheritdoc />
    public Task<IGenericResult> DeleteAbout(string type, Guid thingId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult.Success());

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadMany(
        string type, string subType, IReadOnlyCollection<Guid> thingIds, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyCollection<Guid>>> Find(string type, string subType, string text, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<DescriptionVersion>>> Versions(string type, string subType, Guid thingId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadOwnedBy(
        string type, string subType, IReadOnlyCollection<Guid> ownerIds, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult> Save(
        DescriptionKey key, string thingName, DescriptionOwner? owner, string text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult> Delete(DescriptionKey key, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult> DeleteOwnedBy(string type, Guid ownerId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult> DeleteOwnedExcept(
        string type, string subType, string language, Guid ownerId, IReadOnlyCollection<Guid> keepThingIds, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IGenericResult> DescribeOwned(
        string type, string subType, DescriptionOwner owner, IReadOnlyList<OwnedDescription> things, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public IGenericResult<string> Language() => throw new NotSupportedException();
}
