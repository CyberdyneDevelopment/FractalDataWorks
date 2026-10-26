using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Guards the configuration cascade for this assembly (FDW-1130): every typed child collection a configuration
/// declares must resolve to a registered ConfigurationCommands option, or reading the parent fails loud with
/// NoChildCommandForType and the pipeline cannot load. The check walks the generated mappers' cascade children, the
/// same list the provider walks, so a child added later without a command fails here rather than on a host.
/// </summary>
public sealed class ConfigurationChildCommandsTests
{
    private static List<(IPocoMapper Owner, IChildCascadeDescriptor Child)> EtlChildren() =>
        [.. PocoMapperCollection.All()
            .Where(mapper => ReferenceEquals(mapper.TargetType.Assembly, typeof(PipelineTransformConfiguration).Assembly))
            .SelectMany(mapper => mapper.CascadeChildren.Select(child => (Owner: mapper, Child: child)))];

    [Fact]
    public void AMapsFieldTransformerChainIsACascadeChildWithACommand()
    {
        EtlChildren().Select(pair => pair.Child.ChildType).ShouldContain(typeof(PipelineComponentFieldTransformationConfiguration));
        ConfigurationCommands.All().Select(command => command.ConfigType).ShouldContain(typeof(PipelineComponentFieldTransformationConfiguration));
    }

    [Fact]
    public void EveryTypedChildCollectionHasARegisteredCommand()
    {
        var commanded = ConfigurationCommands.All().Select(command => command.ConfigType).ToHashSet();

        var missing = EtlChildren()
            .Where(pair => !pair.Child.IsPropertyCollection && !commanded.Contains(pair.Child.ChildType))
            .Select(pair => $"{pair.Owner.TargetType.Name}.{pair.Child.BoundPropertyName} -> {pair.Child.ChildTypeName}")
            .ToList();

        missing.ShouldBeEmpty(string.Join("; ", missing));
    }
}
