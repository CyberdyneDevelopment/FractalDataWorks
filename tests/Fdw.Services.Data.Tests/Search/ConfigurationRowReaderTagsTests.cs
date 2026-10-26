using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>
/// FDW-2688: a read of a view is cached under the view, and nothing writes a view, so its cached page was never dropped by a change to the
/// tables it is made of. A reader told those tables tags its reads with the view and each of them; a reader told nothing reads as before.
/// </summary>
public sealed class ConfigurationRowReaderTagsTests
{
    private static string[] TagsOf(InMemoryConfigurationStore store)
        => (string[])store.Commands.ShouldHaveSingleItem().Metadata[CachePolicy.CacheInvalidationTagsKey];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AReaderToldTheTablesAViewReadsTagsAPageWithTheViewAndEachOfThem()
    {
        var store = new InMemoryConfigurationStore().Holds("GrantView", []);
        var reader = new ConfigurationRowReader(store.Gateways, "Store", "authz", "GrantView", NullLogger.Instance, ["UserRole", "Role"]);

        var page = await reader.Page(null, [], 0, 10, true, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        TagsOf(store).ShouldBe(new[] { "authz.GrantView", "authz.UserRole", "authz.Role" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACountIsTaggedTheSameWayAsThePage()
    {
        var store = new InMemoryConfigurationStore().Holds("GrantView", []);
        var reader = new ConfigurationRowReader(store.Gateways, "Store", "authz", "GrantView", NullLogger.Instance, ["UserRole"]);

        await reader.Count(null, TestContext.Current.CancellationToken);

        TagsOf(store).ShouldBe(new[] { "authz.GrantView", "authz.UserRole" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AReaderToldNothingLeavesTheGatewayToTagTheReadByItsOwnContainer()
    {
        var store = new InMemoryConfigurationStore().Holds("Plain", []);
        var reader = new ConfigurationRowReader(store.Gateways, "Store", "authz", "Plain", NullLogger.Instance);

        await reader.Page(null, [], 0, 10, true, TestContext.Current.CancellationToken);

        store.Commands.ShouldHaveSingleItem().Metadata.ContainsKey(CachePolicy.CacheInvalidationTagsKey).ShouldBeFalse();
    }
}
