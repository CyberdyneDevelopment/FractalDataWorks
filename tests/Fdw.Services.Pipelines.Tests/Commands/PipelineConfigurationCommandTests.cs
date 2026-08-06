using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Commands;

/// <summary>
/// Covers <see cref="PipelineConfigurationCommand"/>'s ConfigurationCommands TypeOption identity:
/// the table/container name it declares and the cache-invalidation tag derived from it.
/// </summary>
[Trait("Category", "Configuration")]
public sealed class PipelineConfigurationCommandTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorDeclaresPipelineTableAndConfigType()
    {
        var command = new PipelineConfigurationCommand();

        command.TableName.ShouldBe("Pipeline");
        command.ContainerName.ShouldBe("Pipeline");
        command.ConfigType.ShouldBe(typeof(PipelineConfiguration));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void CacheTagCombinesPathNameAndTableName()
    {
        var command = new PipelineConfigurationCommand();

        command.CacheTag("pipe").ShouldBe("pipe.Pipeline");
    }
}
