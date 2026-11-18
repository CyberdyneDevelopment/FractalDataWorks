using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Options;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6342: the data option tree route answers the layers of the DataConfigurationRules root to any depth, read from the registered collections, with what each leaf takes and the
/// kinds a value, an owner and a custodian state can be. Checked over the real collections, not stand-ins: the answer is what a host registering them gives.
/// </summary>
public sealed class DataOptionTreeRouteTests
{
    private static readonly string[] ComparisonKinds =
        ["Between", "Equal", "GreaterThan", "GreaterThanOrEqual", "In", "LessThan", "LessThanOrEqual", "NotEqual", "NotIn"];

    [Fact]
    public async Task TheTreeRootIsTheDataConfigurationRulesCollectionAndStatesNoDisplayName()
    {
        var tree = await Read();

        tree.Root.Name.ShouldBe("DataConfigurationRules");
        tree.Root.DisplayName.ShouldBeNull();
        tree.Root.Leaf.ShouldBeNull();
        tree.Root.Children.ShouldContain(node => node.Name == "Comparison");
    }

    [Fact]
    public async Task TheComparisonLayerHoldsTheNineKindsAsLeavesThatEndInTo()
    {
        var comparison = (await Read()).Root.Children.Single(node => node.Name == "Comparison");

        comparison.Leaf.ShouldBeNull();
        comparison.Children.Select(kind => kind.Name).ShouldBe(ComparisonKinds);
        comparison.Children.ShouldAllBe(kind => kind.Leaf != null && kind.Leaf.Method == "To" && kind.Children.Count == 0);
    }

    [Fact]
    public async Task EachLeafStatesHowManyValuesItTakes()
    {
        var kinds = (await Read()).Root.Children.Single(node => node.Name == "Comparison").Children.ToDictionary(kind => kind.Name, kind => kind.Leaf!);

        (kinds["In"].MinValues, kinds["In"].MaxValues).ShouldBe((1, null));
        (kinds["Equal"].MinValues, kinds["Equal"].MaxValues).ShouldBe((1, 1));
        (kinds["Between"].MinValues, kinds["Between"].MaxValues).ShouldBe((2, 2));
    }

    [Fact]
    public async Task ANodeWithChildrenStatesNoLeafAndALeafHasNoChildrenAtEveryDepth()
    {
        var nodes = Walk((await Read()).Root).Select(entry => entry.Node).ToList();

        nodes.Where(node => node.Leaf is null).ShouldAllBe(node => node.Children.Count > 0 || node.Name == "DataConfigurationRules");
        nodes.Where(node => node.Leaf is not null).ShouldAllBe(node => node.Children.Count == 0);
    }

    [Fact]
    public async Task ChildrenAreInNameOrderAtEveryLevel()
    {
        foreach (var (_, node) in Walk((await Read()).Root))
            node.Children.Select(child => child.Name).ShouldBe([.. node.Children.Select(child => child.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)]);
    }

    // A path the tree states is a path the rule service accepts: every leaf, with its method, resolves to the kind that has its name.
    [Fact]
    public async Task EveryLeafsPathAndMethodResolveThroughTheRulePathResolver()
    {
        var leaves = Walk((await Read()).Root).Where(entry => entry.Node.Leaf is not null).ToList();

        leaves.ShouldNotBeEmpty();
        foreach (var (path, node) in leaves)
        {
            var resolved = RulePaths.Resolve(NullLogger.Instance, path, node.Leaf!.Method);

            resolved.IsSuccess.ShouldBeTrue($"{string.Join(':', path)}.{node.Leaf.Method}: {resolved.CurrentMessage}");
            resolved.Value!.Name.ShouldBe(node.Name);
        }
    }

    // The console shows a level's description as the hint under its picker: the framework's placeholder ("Type option: In") is noise there.
    [Fact]
    public async Task EveryLayerAndKindStatesItsOwnDisplayNameAndDescription()
    {
        var below = Walk((await Read()).Root).Select(entry => entry.Node).Where(node => node.Name != "DataConfigurationRules").ToList();

        below.ShouldNotBeEmpty();
        below.ShouldAllBe(node => !string.IsNullOrWhiteSpace(node.DisplayName) && !string.IsNullOrWhiteSpace(node.Description) && !node.Description!.StartsWith("Type option:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheValueKindsStateTheMemberOfAValueThatHoldsTheirContent()
    {
        var kinds = (await Read()).ValueKinds.ToDictionary(kind => kind.Name, kind => kind.Member);

        kinds["Literal"].ShouldBe("value");
        kinds["Parameter"].ShouldBe("parameter");
    }

    [Fact]
    public async Task TheOwnerKindsAndTheCustodianStatesComeFromTheirCollectionsWithTheAttentionFlag()
    {
        var tree = await Read();

        tree.OwnerKinds.Select(kind => kind.Name).ShouldBe(["Organization", "Workspace"]);
        tree.CustodianStates.Select(state => (state.Name, state.NeedsAttention)).ShouldBe([("Holder", false), ("NoLongerHolder", true)]);
    }

    private static async Task<DataOptionTreePayload> Read()
    {
        var endpoint = Factory.Create<Tree>();
        await endpoint.HandleAsync(EmptyRequest.Instance, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        return endpoint.Response;
    }

    // Every node below the root with the names that lead to it, root first.
    private static IEnumerable<(string[] Path, DataOptionTreeNodePayload Node)> Walk(DataOptionTreeNodePayload root)
    {
        var stack = new Stack<(string[] Path, DataOptionTreeNodePayload Node)>([([root.Name], root)]);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Node.Children)
                stack.Push(([.. current.Path, child.Name], child));
        }
    }

    private sealed class Tree : GetDataOptionTreeEndpointBase
    {
        public Tree()
            : base(NullLogger.Instance)
        {
        }
    }
}
