using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-1682: a glossary term that states what it stands for is an acronym, and it states whether it is approved with it; a name is
/// unique among the terms ignoring case.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class AcronymFactsTests
{
    private static IGlossaryTermImplementationConfiguration Term(string name, Guid? id = null, string? standsFor = null)
    {
        var term = new Mock<IGlossaryTermImplementationConfiguration>();
        term.SetupGet(t => t.Id).Returns(id is { } given ? given : Guid.NewGuid());
        term.SetupGet(t => t.Name).Returns(name);
        term.SetupGet(t => t.Definition).Returns(string.Empty);
        term.SetupGet(t => t.StandsFor).Returns(standsFor);
        return term.Object;
    }

    private static Mock<IGlossaryTermConfigurationProvider> Provider(params IGlossaryTermImplementationConfiguration[] terms)
    {
        var provider = new Mock<IGlossaryTermConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IGlossaryTermImplementationConfiguration>>.Success(terms));
        return provider;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnAcronymThatStatesBothFactsIsValidAndATermThatStatesNeitherIsNotAnAcronym()
    {
        AcronymFacts.Refusal("MTD", "Month to date", true, NullLogger.Instance).ShouldBeNull();
        AcronymFacts.Refusal("Churn", null, null, NullLogger.Instance).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnAcronymThatDoesNotSayWhetherItIsApprovedIsRefused()
    {
        AcronymFacts.Refusal("MTD", "Month to date", null, NullLogger.Instance).ShouldNotBeNull().Code.ShouldNotBeNull().ShouldContain("22732");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ATermThatIsApprovedWithoutStatingWhatItStandsForIsRefused()
    {
        AcronymFacts.Refusal("Churn", null, true, NullLogger.Instance).ShouldNotBeNull().Code.ShouldNotBeNull().ShouldContain("22733");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnEmptyAndATooLongStandsForAreRefused()
    {
        AcronymFacts.Refusal("MTD", "  ", true, NullLogger.Instance).ShouldNotBeNull().Code.ShouldNotBeNull().ShouldContain("22730");
        AcronymFacts.Refusal("MTD", new string('x', AcronymFacts.MaxStandsForLength + 1), true, NullLogger.Instance).ShouldNotBeNull().Code.ShouldNotBeNull().ShouldContain("22731");
        AcronymFacts.Refusal("MTD", new string('x', AcronymFacts.MaxStandsForLength), true, NullLogger.Instance).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ANameIsTakenIgnoringCaseButNotByTheTermBeingSaved()
    {
        var mtd = Term("MTD");
        var terms = new[] { mtd, Term("YTD") };

        AcronymFacts.NameTaken(terms, "mtd", null).ShouldBeTrue();
        AcronymFacts.NameTaken(terms, "mtd", mtd.Id).ShouldBeFalse();
        AcronymFacts.NameTaken(terms, "QTD", null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task CheckRefusesATakenNameWith409AndAllowsANewOne()
    {
        var provider = Provider(Term("MTD"));

        var taken = await AcronymFacts.Check(provider.Object, "mtd", "Month to date", true, null, NullLogger.Instance, TestContext.Current.CancellationToken);
        var fresh = await AcronymFacts.Check(provider.Object, "QTD", "Quarter to date", true, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        taken.IsFailure.ShouldBeTrue();
        taken.CurrentMessage.ShouldNotBeNull();
        fresh.IsSuccess.ShouldBeTrue(fresh.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CheckDoesNotReadTheTermsWhenTheFactsAreAlreadyRefused()
    {
        var provider = Provider();

        var refused = await AcronymFacts.Check(provider.Object, "MTD", "Month to date", null, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        provider.Verify(p => p.Get(It.IsAny<CancellationToken>()), Times.Never);
    }
}
