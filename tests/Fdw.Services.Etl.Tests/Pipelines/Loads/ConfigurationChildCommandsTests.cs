using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
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
            .Where(mapper => ReferenceEquals(mapper.TargetType.Assembly, typeof(BatchCopyPipelineConfiguration).Assembly))
            .SelectMany(mapper => mapper.CascadeChildren.Select(child => (Owner: mapper, Child: child)))];

    [Fact]
    public void TheLoadConfigurationsChildCollectionsAreCascadeChildren()
    {
        var childTypes = EtlChildren().Select(pair => pair.Child.ChildType).ToList();

        childTypes.ShouldContain(typeof(PipelineLoadStepConfiguration));
        childTypes.ShouldContain(typeof(PipelineRunParameterConfiguration));
        childTypes.ShouldContain(typeof(PipelineLoadColumnConfiguration));
        childTypes.ShouldContain(typeof(PipelineLoadValueSourceConfiguration));
    }

    [Fact]
    public void AFieldMappingsTransformerChainIsACascadeChildWithACommand()
    {
        EtlChildren().Select(pair => pair.Child.ChildType).ShouldContain(typeof(PipelineTransformFieldMappingTransformationConfiguration));
        ConfigurationCommands.All().Select(command => command.ConfigType).ShouldContain(typeof(PipelineTransformFieldMappingTransformationConfiguration));
    }

    [Fact]
    public void ABranchStagesPathsAndThePathsStagesAreCascadeChildren()
    {
        var children = EtlChildren();

        // A Branch stage owns its paths, and a path owns the stages a record it took passes through (FDW-1120): each hop of
        // the stage tree is stage, path, stage, so no stage needs a key to its parent stage.
        children.Any(pair => pair.Owner.TargetType == typeof(PipelineTransformConfiguration) && pair.Child.ChildType == typeof(PipelineTransformPathConfiguration)).ShouldBeTrue();
        children.Any(pair => pair.Owner.TargetType == typeof(PipelineTransformPathConfiguration) && pair.Child.ChildType == typeof(PipelineTransformConfiguration)).ShouldBeTrue();
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
