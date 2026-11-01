using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// A record that holds a list of its own type is a tree (FDW-3004): its members hang from the member their parent link names, down to the
/// levels asked for, and links that lead back to an ancestor fail naming the member.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfigurationTreesTests
{
    private const string Link = "ParentId";

    // Read when a test runs, after ServicesTypeCollectionFixture has registered this assembly's mappers: a test assembly's own mappers are
    // not registered automatically, so a lookup before the fixture runs would capture the NotFound sentinel.
    private static IPocoMapper Mapper => PocoMapperCollection.ByName(nameof(TreeNodeConfiguration));

    private static List<IChildCascadeDescriptor> Trees()
        => Mapper.CascadeChildren.Where(d => ConfigurationTrees.IsTree(d, typeof(TreeNodeConfiguration))).ToList();

    private static TreeNodeConfiguration Node(string name, TreeNodeConfiguration? parent = null)
        => new() { Id = Guid.NewGuid(), Name = name, ParentId = parent?.Id };

    private static List<string> Names(IEnumerable<TreeNodeConfiguration> nodes) => nodes.Select(n => n.Name).ToList();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheTestRecordsMapperIsRegisteredAndDeclaresItsChildList()
    {
        ReferenceEquals(Mapper, PocoMapperCollection.NotFound).ShouldBeFalse(
            $"no mapper is registered for {nameof(TreeNodeConfiguration)}; ServicesTypeCollectionFixture registers this assembly's mappers by hand");
        Mapper.CascadeChildren.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AListOfTheOwnersOwnTypeIsATreeAndNothingElseIs()
    {
        var descriptor = Mapper.CascadeChildren.Single();

        ConfigurationTrees.IsTree(descriptor, typeof(TreeNodeConfiguration)).ShouldBeTrue();
        ConfigurationTrees.IsTree(descriptor, typeof(object)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MembersHangUnderTheirParentsAtEveryDepth()
    {
        var project = Node("project");
        var stageA = Node("stage-a", project);
        var stageB = Node("stage-b", project);
        var step = Node("step", stageA);
        var other = Node("other");
        var otherStage = Node("other-stage", other);
        TreeNodeConfiguration[] members = [project, stageA, stageB, step, other, otherStage];

        var result = ConfigurationTrees.Attach(project, members, Link, Trees(), Mapper, int.MaxValue, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Names(project.Children).ShouldBe(["stage-a", "stage-b"]);
        Names(stageA.Children).ShouldBe(["step"]);
        stageB.Children.ShouldBeEmpty();
        step.Children.ShouldBeEmpty();
        other.Children.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheLevelsAskedForLimitTheDepth()
    {
        var project = Node("project");
        var stage = Node("stage", project);
        var step = Node("step", stage);
        TreeNodeConfiguration[] members = [project, stage, step];

        var result = ConfigurationTrees.Attach(project, members, Link, Trees(), Mapper, 1, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Names(project.Children).ShouldBe(["stage"]);
        stage.Children.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ALinkThatLeadsBackToAnAncestorFails()
    {
        var project = Node("project");
        var stage = Node("stage", project);
        var step = Node("step", stage);
        project.ParentId = step.Id;
        TreeNodeConfiguration[] members = [project, stage, step];

        var result = ConfigurationTrees.Attach(project, members, Link, Trees(), Mapper, int.MaxValue, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ALinkFieldTheRecordDoesNotCarryFails()
    {
        var project = Node("project");

        var result = ConfigurationTrees.Attach(project, [project], "NoSuchField", Trees(), Mapper, int.MaxValue, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>A member of a tree: a record that holds a list of its own type, hung from its parent by ParentId.</summary>
    [GenerateMapper]
    public sealed class TreeNodeConfiguration : IImplementationConfiguration
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = "Tree";

        public string Implementation { get; set; } = "Default";

        public Guid? ParentId { get; set; }

        public IList<TreeNodeConfiguration> Children { get; set; } = [];
    }
}
