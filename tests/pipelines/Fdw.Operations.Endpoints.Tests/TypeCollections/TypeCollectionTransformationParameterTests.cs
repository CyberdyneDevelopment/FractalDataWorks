using System.Linq;
using Fdw.Data.Transformations;
using Fdw.Operations.Endpoints.TypeCollections;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// FDW-1988: the type-collection value summary of a field transformation lists the parameters it states, so the designer can ask for them. Each
/// is text (a step passes every parameter as text, which may name a variable), says whether it is required, and carries the transformation's help.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionTransformationParameterTests
{
    [Fact]
    public void FormatDateTimeListsItsRequiredFormat()
    {
        var format = TypeCollectionResolver.ToSummary(new FormatDateTimeFieldTransformer()).Parameters.ShouldHaveSingleItem();

        format.Name.ShouldBe("format");
        format.Kind.ShouldBe("Text");
        format.IsRequired.ShouldBeTrue();
        format.AcceptsParameterText.ShouldBeTrue();
        format.HelpText.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AddDurationListsItsAmountAndUnitBothRequired()
    {
        var parameters = TypeCollectionResolver.ToSummary(new AddDurationFieldTransformer()).Parameters;

        parameters.Select(parameter => parameter.Name).ShouldBe(["amount", "unit"], ignoreOrder: true);
        parameters.ShouldAllBe(parameter => parameter.IsRequired);
    }

    [Fact]
    public void ConcatenateListsItsTwoOptionalParameters()
    {
        var parameters = TypeCollectionResolver.ToSummary(new ConcatenateFieldTransformer()).Parameters;

        parameters.Select(parameter => parameter.Name).ShouldBe(["prefix", "suffix"], ignoreOrder: true);
        parameters.ShouldAllBe(parameter => !parameter.IsRequired);
    }

    [Fact]
    public void ATransformationThatStatesNoParameterListsNone()
    {
        TypeCollectionResolver.ToSummary(new ToUpperFieldTransformer()).Parameters.ShouldBeEmpty();
    }
}
