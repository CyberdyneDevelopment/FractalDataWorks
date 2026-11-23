using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Moq;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>
/// A strict descriptions writer and reader that state their answers (success, no text held) and throw on any other call, for the calculation tests that are about something else: the calculation and
/// version providers describe each thing on a save and fill it on a read (FDW-6208), and <c>CalculationDescriptionsTests</c> covers what they ask.
/// </summary>
internal static class DescriptionDoubles
{
    /// <summary>A writer whose every call succeeds.</summary>
    public static IDescriptionWriter Writer()
    {
        var writer = new Mock<IDescriptionWriter>(MockBehavior.Strict);
        writer
            .Setup(w => w.Describe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        writer
            .Setup(w => w.DeleteAbout(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return writer.Object;
    }

    /// <summary>A reader that holds no description: every fill succeeds and leaves each thing as it is.</summary>
    public static IDescriptionReader Reader()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<ICalculationEntityImplementationConfiguration>>(),
                It.IsAny<Func<ICalculationEntityImplementationConfiguration, Guid>>(), It.IsAny<Action<ICalculationEntityImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        reader
            .Setup(r => r.Fill(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<CalculationEntityDomainConfiguration>>(),
                It.IsAny<Func<CalculationEntityDomainConfiguration, Guid>>(), It.IsAny<Action<CalculationEntityDomainConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return reader.Object;
    }
}
