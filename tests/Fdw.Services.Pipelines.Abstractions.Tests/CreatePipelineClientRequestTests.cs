using Fdw.Services.Pipelines.Clients.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests;

/// <summary>
/// Guards against a defaulted <see cref="CreatePipelineClientRequest.PipelineType"/> masking a
/// missing required engine — a literal <c>"BatchCopy"</c> default previously defeated the server's
/// <c>PipelineType.NotEmpty()</c> validator, since an omitted value was never actually empty.
/// </summary>
public sealed class CreatePipelineClientRequestTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PipelineTypeHasNoDefaultEngine()
    {
        var request = new CreatePipelineClientRequest();

        request.PipelineType.ShouldBe(string.Empty);
    }
}
