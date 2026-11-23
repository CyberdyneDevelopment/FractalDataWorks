using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6119: <c>GET /datasets?q=</c> finds text in a data set's name, display name and abbreviation as <c>?match=</c> says, in one query on the domain
/// table (all three sit on the domain row, FDW-6129). The ids it finds narrow the list the category, data set type and tag selections already make.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetListSearchTests
{
    private static ListSearch Search(string query)
    {
        // An id search reads the most identifiers one call may state from the host's gateway provider (FDW-7106), so the request carries one.
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(g => g.KeyChunkSize).Returns(1000);
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(gateways.Object).BuildServiceProvider() };
        context.Request.QueryString = new QueryString(query);
        return ListSearch.Parse(context, NullLogger.Instance).Value!;
    }

    [Fact]
    public void ARequestWithNoSearchAsksNoQueryAndKeepsEveryDataSet()
    {
        DataSetListSearch.Query(Search(string.Empty)).ShouldBeNull();
    }

    [Fact]
    public void TextIsFoundInTheNameTheDisplayNameAndTheAbbreviation()
    {
        var query = DataSetListSearch.Query(Search("?q=gas"))!;

        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.OfType<FilterCondition>().Select(c => c.PropertyName).ShouldBe(["Name", "DisplayName", "Abbreviation"]);
        group.Nodes.OfType<FilterCondition>().ShouldAllBe(c => (string?)c.Value == "gas");
    }

    [Fact]
    public void TheQueryReadsEveryMatchInNameOrderWithNoTotal()
    {
        var query = DataSetListSearch.Query(Search("?q=gas"))!;

        query.Skip.ShouldBe(0);
        query.Take.ShouldBe(int.MaxValue - 1);
        query.IncludeTotal.ShouldBeFalse();
        query.Ordering!.OrderedFields.Single().PropertyName.ShouldBe("Name");
    }

    [Fact]
    public void AnIdSearchIsAConditionOnTheIdOfTheRecord()
    {
        var id = Guid.NewGuid();

        var query = DataSetListSearch.Query(Search($"?id={id:D}"))!;

        var condition = query.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Id");
    }

    [Fact]
    public void OnlyTheDataSetsTheSearchFoundAreKeptInTheOrderTheyWereRead()
    {
        var a = new SimpleDataSetConfiguration { Id = Guid.NewGuid(), Name = "A" };
        var b = new CompoundDataSetConfiguration { Id = Guid.NewGuid(), Name = "B" };
        var c = new FederatedDataSetConfiguration { Id = Guid.NewGuid(), Name = "C" };

        var kept = DataSetListSearch.Keep([a, b, c], new HashSet<Guid> { c.Id, a.Id });

        kept.ShouldBe([a, c]);
    }

    [Fact]
    public void WithNoSearchEveryDataSetIsKept()
    {
        var a = new SimpleDataSetConfiguration { Id = Guid.NewGuid(), Name = "A" };

        DataSetListSearch.Keep([a], null).ShouldBe([a]);
    }

    [Fact]
    public async Task ASearchWithATokenAlreadyCancelledEndsWithTheCodedCancelledResultAndAsksNoOne()
    {
        var records = new Mock<IDataSetRecordProvider>(MockBehavior.Strict);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var found = await DataSetListSearch.Found(records.Object, Search("?q=gas"), NullLogger.Instance, source.Token);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("DATASETS-10010");
        records.Verify(r => r.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ASearchCancelledWhileItIsReadEndsWithTheCodedCancelledResult()
    {
        using var source = new CancellationTokenSource();
        var records = new Mock<IDataSetRecordProvider>(MockBehavior.Strict);
        records
            .Setup(r => r.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async (DomainQuery _, CancellationToken token) =>
            {
                await source.CancelAsync();
                token.ThrowIfCancellationRequested();
                return GenericResult<ConfigurationPage<DataSetDomainConfiguration>>.Failure(new Fdw.Messages.GenericMessage("not reached"));
            });

        var found = await DataSetListSearch.Found(records.Object, Search("?q=gas"), NullLogger.Instance, source.Token);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("DATASETS-10010");
    }

    [Fact]
    public async Task ARequestThatSearchesNothingIsNotAskedAndIsNotCancelledByTheToken()
    {
        var records = new Mock<IDataSetRecordProvider>(MockBehavior.Strict);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var found = await DataSetListSearch.Found(records.Object, Search(string.Empty), NullLogger.Instance, source.Token);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBeNull();
        records.Verify(r => r.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
