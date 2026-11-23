using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Moq;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A strict descriptions writer and reader that state their answers (success, no text held) and throw on any other call, for the data store tests that are about
/// something else: the data store provider describes each store on a save and fills it on a read (FDW-6208), and <c>DataStoreDescriptionsTests</c> covers what it asks.
/// </summary>
internal static class DataStoreDescriptionDoubles
{
    /// <summary>A writer whose every call succeeds.</summary>
    public static IDescriptionWriter Writer()
    {
        var writer = new Mock<IDescriptionWriter>(MockBehavior.Strict);
        writer
            .Setup(w => w.Describe("DataStore", "Text", It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        writer
            .Setup(w => w.DeleteAbout("DataStore", It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        // The store's paths, containers, fields and keys, each kept in step with the store as owner (FDW-6589).
        writer
            .Setup(w => w.DescribeOwned(It.IsAny<string>(), "Text", It.IsAny<DescriptionOwner>(), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        writer
            .Setup(w => w.DeleteOwnedBy(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return writer.Object;
    }

    /// <summary>A reader that holds no description: every fill succeeds and leaves each store, and each of its children, as it is.</summary>
    public static IDescriptionReader Reader()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("DataStore", "Text", It.IsAny<IReadOnlyList<IDataStoreImplementationConfiguration>>(),
                It.IsAny<Func<IDataStoreImplementationConfiguration, Guid>>(), It.IsAny<Action<IDataStoreImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        reader
            .Setup(r => r.Fill("DataStore", "Text", It.IsAny<IReadOnlyList<DataStoreDomainConfiguration>>(),
                It.IsAny<Func<DataStoreDomainConfiguration, Guid>>(), It.IsAny<Action<DataStoreDomainConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        reader
            .Setup(r => r.ReadOwnedBy(It.IsAny<string>(), "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(new Dictionary<Guid, DescriptionEntry>()));
        return reader.Object;
    }
}
