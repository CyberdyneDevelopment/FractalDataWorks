using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// Tests for <see cref="CatalogService"/> - business glossary + DataSet annotation CRUD and
/// cross-entity catalog search/relevance-scoring logic.
/// </summary>
public sealed class CatalogServiceTests
{
    private static CatalogService CreateService()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var termsMonitor = new Mock<IOptionsMonitor<List<GlossaryTermConfiguration>>>();
        var annotationsMonitor = new Mock<IOptionsMonitor<List<DataSetAnnotationConfiguration>>>();
        return new CatalogService(loggerFactory, termsMonitor.Object, annotationsMonitor.Object);
    }

    private static GlossaryTermConfiguration MakeTerm(string name, string definition = "def", string category = "Finance")
        => new() { Name = name, Definition = definition, Category = category };

    // ── SearchTerms ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task SearchTermsWithNoFiltersReturnsAllTerms()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);
        await service.CreateTerm(MakeTerm("Churn"), TestContext.Current.CancellationToken);

        var result = await service.SearchTerms(null, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task SearchTermsWithQueryFilterMatchesNameOrDefinition()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue", definition: "total income"), TestContext.Current.CancellationToken);
        await service.CreateTerm(MakeTerm("Churn", definition: "customer loss"), TestContext.Current.CancellationToken);

        var result = await service.SearchTerms("income", null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].Name.ShouldBe("Revenue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task SearchTermsWithCategoryFilterReturnsMatchingCategoryOnly()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue", category: "Finance"), TestContext.Current.CancellationToken);
        await service.CreateTerm(MakeTerm("Latency", category: "Engineering"), TestContext.Current.CancellationToken);

        var result = await service.SearchTerms(null, "engineering", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].Name.ShouldBe("Latency");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task SearchTermsWithNoMatchesReturnsEmpty()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var result = await service.SearchTerms("nonexistent", null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    // ── CreateTerm ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task CreateTermWithNewNameAssignsIdAndReturnsSuccess()
    {
        var service = CreateService();

        var result = await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateTermWithDuplicateNameCaseInsensitiveReturnsFailureWithDuplicateTermNameCode()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var result = await service.CreateTerm(MakeTerm("REVENUE"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-41000");
    }

    // ── UpdateTerm ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task UpdateTermWithExistingIdReplacesTerm()
    {
        var service = CreateService();
        var created = await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var updated = MakeTerm("Revenue", definition: "updated definition");
        var result = await service.UpdateTerm(created.Value!.Id, updated, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(created.Value!.Id);
        result.Value!.Definition.ShouldBe("updated definition");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task UpdateTermWithNonExistentIdReturnsFailureWithTermNotFoundCode()
    {
        var service = CreateService();

        var result = await service.UpdateTerm(Guid.NewGuid(), MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31000");
    }

    // ── DeleteTerm / GetTerm ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task DeleteTermWithExistingIdReturnsSuccess()
    {
        var service = CreateService();
        var created = await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var result = await service.DeleteTerm(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeleteTermWithNonExistentIdReturnsFailure()
    {
        var service = CreateService();

        var result = await service.DeleteTerm(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetTermWithExistingIdReturnsSuccess()
    {
        var service = CreateService();
        var created = await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var result = await service.GetTerm(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Revenue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetTermWithNonExistentIdReturnsFailure()
    {
        var service = CreateService();

        var result = await service.GetTerm(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31000");
    }

    // ── Annotations ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetAnnotationWithExistingDataSetReturnsSuccess()
    {
        var service = CreateService();
        await service.UpdateAnnotation("Orders", new DataSetAnnotationConfiguration(), TestContext.Current.CancellationToken);

        var result = await service.GetAnnotation("Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DataSetName.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetAnnotationWithNonExistentDataSetReturnsFailureWithAnnotationNotFoundCode()
    {
        var service = CreateService();

        var result = await service.GetAnnotation("Nonexistent", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task UpdateAnnotationWithNewDataSetAddsAndReturnsSuccess()
    {
        var service = CreateService();

        var result = await service.UpdateAnnotation(
            "Orders",
            new DataSetAnnotationConfiguration { BusinessOwner = "alice" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DataSetName.ShouldBe("Orders");
        result.Value!.BusinessOwner.ShouldBe("alice");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task UpdateAnnotationWithExistingDataSetReplacesPriorAnnotation()
    {
        var service = CreateService();
        await service.UpdateAnnotation("Orders", new DataSetAnnotationConfiguration { BusinessOwner = "alice" }, TestContext.Current.CancellationToken);

        var result = await service.UpdateAnnotation("Orders", new DataSetAnnotationConfiguration { BusinessOwner = "bob" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.BusinessOwner.ShouldBe("bob");

        var fetched = await service.GetAnnotation("Orders", TestContext.Current.CancellationToken);
        fetched.Value!.BusinessOwner.ShouldBe("bob");
    }

    // ── Search (cross-entity + relevance) ───────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task SearchMatchesTermsAndAnnotationsAndOrdersByRelevanceDescending()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue", definition: "total income for the period"), TestContext.Current.CancellationToken);
        await service.UpdateAnnotation("RevenueReport", new DataSetAnnotationConfiguration { Description = "revenue dataset" }, TestContext.Current.CancellationToken);

        var result = await service.Search("Revenue", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        // Exact-name term match (1.0) ranks above the annotation whose name only contains the query (0.7).
        result.Value![0].Type.ShouldBe("GlossaryTerm");
        result.Value![0].Relevance.ShouldBeGreaterThanOrEqualTo(result.Value![1].Relevance);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task SearchWithExactNameMatchScoresHighestRelevance()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue", definition: "unrelated text"), TestContext.Current.CancellationToken);

        var result = await service.Search("Revenue", TestContext.Current.CancellationToken);

        result.Value![0].Relevance.ShouldBe(1.0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task SearchWithPartialNameMatchScoresPartialRelevance()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Total Revenue", definition: "unrelated text"), TestContext.Current.CancellationToken);

        var result = await service.Search("Revenue", TestContext.Current.CancellationToken);

        result.Value![0].Relevance.ShouldBe(0.7);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task SearchWithDescriptionOnlyMatchAddsRelevanceBoost()
    {
        var service = CreateService();
        await service.UpdateAnnotation("Orders", new DataSetAnnotationConfiguration { Description = "contains revenue figures" }, TestContext.Current.CancellationToken);

        var result = await service.Search("revenue", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].Relevance.ShouldBe(0.3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task SearchWithAnnotationHavingNullDescriptionDoesNotThrow()
    {
        var service = CreateService();
        await service.UpdateAnnotation("RevenueOrders", new DataSetAnnotationConfiguration(), TestContext.Current.CancellationToken);

        var result = await service.Search("Revenue", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task SearchWithNoMatchesReturnsEmptyList()
    {
        var service = CreateService();
        await service.CreateTerm(MakeTerm("Revenue"), TestContext.Current.CancellationToken);

        var result = await service.Search("Nonexistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }
}
