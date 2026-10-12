using System;
using System.Text.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests;

/// <summary>
/// The pipeline detail read hands the UI its steps as <see cref="PipelineTransformClientRequest"/>, and
/// the UI keys each step by its id. The id must survive the read.
/// </summary>
public sealed class PipelineTransformClientRequestIdTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StepIdSurvivesADetailRead()
    {
        var id = Guid.CreateVersion7();

        var step = JsonSerializer.Deserialize<PipelineTransformClientRequest>($$"""{"id":"{{id}}","name":"MapA"}""", Web);

        step!.Id.ShouldBe(id);
    }
}
