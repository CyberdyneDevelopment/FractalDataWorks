using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status. A filter operator the
/// protocol cannot express is in the caller's query, so it answers 400, not 500.
/// </summary>
public sealed class ResultCodeStatusTests
{
    [Fact]
    [Trait("Priority", "P1")]
    public void AFilterOperatorTheProtocolCannotExpressAnswersBadRequest()
        => ResultCategories.ById(HttpResultCodes.ByName("UnsupportedFilterOperator").Id / 10000).HttpStatus.ShouldBe(400);
}
