using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Moq;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// A strict descriptions writer and reader for the lineage tests that build the pipeline provider only because lineage resolves pipelines through it: the provider fills a
/// pipeline's description and its parameters' on a read (FDW-7323), and these tests hold no text. The writer has no setup, so any write throws.
/// </summary>
internal static class PipelineDescriptionDoubles
{
    /// <summary>A writer that must never be called.</summary>
    public static IDescriptionWriter Writer() => new Mock<IDescriptionWriter>(MockBehavior.Strict).Object;

    /// <summary>A reader whose every answer is success with no text held.</summary>
    public static IDescriptionReader Reader()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("Pipeline", "Text", It.IsAny<IReadOnlyList<IPipelineImplementationConfiguration>>(),
                It.IsAny<Func<IPipelineImplementationConfiguration, Guid>>(), It.IsAny<Action<IPipelineImplementationConfiguration, string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        reader
            .Setup(r => r.ReadOwnedBy("PipelineParameter", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(new Dictionary<Guid, DescriptionEntry>()));
        return reader.Object;
    }
}
