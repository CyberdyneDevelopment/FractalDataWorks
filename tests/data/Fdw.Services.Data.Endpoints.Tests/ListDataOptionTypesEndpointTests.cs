using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Connections.TestDouble;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5665: the option types route answers every option type of <see cref="DataOptions"/> with that type's own options, read
/// from the type collections, so an option type or option a package adds shows up with no change to the route or the console.
/// </summary>
public sealed class ListDataOptionTypesEndpointTests
{
    [Fact]
    public void TheRouteIsGatedByReadingADataStore()
    {
        var endpoint = new OptionTypes();

        endpoint.RegisteredRoute.ShouldBe("/data-option-types");
        endpoint.RegisteredPolicy.ShouldBe("datastores:read");
    }

    [Fact]
    public async Task SelectListIsAnsweredWithIncludeThenExclude()
    {
        var listed = await new OptionTypes().Load(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(t => t.Name).ShouldContain("SelectList");
        var selectList = listed.Value!.Single(t => string.Equals(t.Name, "SelectList", System.StringComparison.Ordinal));
        selectList.Options.ShouldBe(["Include", "Exclude"]);
    }

    // The probe type is declared in a referenced test library, as a package would add one: nothing in the route names it.
    [Fact]
    public async Task AnOptionTypeAPackageAddsIsAnsweredWithNoOtherChange()
    {
        var listed = await new OptionTypes().Load(TestContext.Current.CancellationToken);

        listed.Value!.Select(t => t.Name).ShouldContain(ProbeDataOptionType.ProbeName);
        listed.Value!.Single(t => string.Equals(t.Name, ProbeDataOptionType.ProbeName, System.StringComparison.Ordinal)).Options.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheTypesAreAnsweredInNameOrder()
    {
        var listed = await new OptionTypes().Load(TestContext.Current.CancellationToken);

        var names = listed.Value!.Select(t => t.Name).ToList();
        names.Count.ShouldBeGreaterThan(1);
        names.ShouldBe([.. names.Order(System.StringComparer.Ordinal)]);
    }

    private sealed class OptionTypes : ListDataOptionTypesEndpointBase
    {
        public OptionTypes()
            : base(NullLogger<ListDataOptionTypesEndpointBase>.Instance)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public Task<IGenericResult<List<DataOptionTypePayload>>> Load(CancellationToken cancellationToken) => LoadItems(cancellationToken);
    }
}
