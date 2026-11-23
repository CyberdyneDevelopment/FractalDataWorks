using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Moq;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A strict descriptions writer and reader that state their answers (success, no text held) and throw on any other call, for the data set tests that build the
/// transformation provider only because a data set resolves field mappings through it: the provider describes each transformation on a save and fills it on a read
/// (FDW-6208), and <c>TransformationDescriptionsTests</c> covers what it asks.
/// </summary>
internal static class TransformationDescriptionDoubles
{
    /// <summary>A writer whose every call succeeds.</summary>
    public static IDescriptionWriter Writer()
    {
        var writer = new Mock<IDescriptionWriter>(MockBehavior.Strict);
        writer
            .Setup(w => w.Describe("Transformation", "Text", It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        writer
            .Setup(w => w.DeleteAbout("Transformation", It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return writer.Object;
    }

    /// <summary>A reader that holds no description: every fill succeeds and leaves each transformation as it is.</summary>
    public static IDescriptionReader Reader()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("Transformation", "Text", It.IsAny<IReadOnlyList<ITransformationImplementationConfiguration>>(),
                It.IsAny<Func<ITransformationImplementationConfiguration, Guid>>(), It.IsAny<Action<ITransformationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return reader.Object;
    }
}
