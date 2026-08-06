using System.Net;
using System.Net.Http.Json;
using Fdw.Web.Calculations.Clients.ApiClients;
using Fdw.Web.Calculations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Calculations.Clients.Tests;

public sealed class CalculationApiClientTests
{
    private static CalculationApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new CalculationApiClient(httpClient, Mock.Of<ILogger<CalculationApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationTypesSendsCorrectRequest()
    {
        var expected = new CalculationTypesResponse
        {
            Types = [new CalculationTypePayload { Name = "Sum", DisplayName = "Sum", Description = "Sum calc", CalculationSource = "Default" }]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCalculationTypes(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculations/types");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Types.Count.ShouldBe(1);
        result.Value.Types[0].Name.ShouldBe("Sum");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationTypesReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetCalculationTypes(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteCalculationSendsCorrectRequest()
    {
        var expected = new ExecuteCalculationResponse
        {
            CalculationType = "Sum",
            Result = 15m,
            InputCount = 3
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new ExecuteCalculationRequest { CalculationType = "Sum", Values = [5m, 5m, 5m] };

        var result = await sut.ExecuteCalculation(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculations/execute");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Result.ShouldBe(15m);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteCalculationReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new ExecuteCalculationRequest { CalculationType = "Invalid" };

        var result = await sut.ExecuteCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new ExecuteCalculationRequest { CalculationType = "Sum" };

        var result = await sut.ExecuteCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationsSendsCorrectRequest()
    {
        var expected = new List<CalculationSummaryPayload>
        {
            new() { Name = "Revenue", TargetDataSet = "Sales", IsEnabled = true }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCalculations(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculation-entities");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Revenue");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetCalculations(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var expected = new CalculationDetailPayload { Id = id, Name = "Revenue", Formula = "SUM(Amount)" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCalculation(id, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/calculation-entities/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Revenue");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetCalculation(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateCalculationSendsCorrectRequest()
    {
        var expected = new CalculationDetailPayload { Name = "NewCalc", Formula = "AVG(Price)" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new CreateCalculationDefinitionRequest
        {
            Name = "NewCalc",
            TargetDataSet = "Products",
            ResultFieldName = "AvgPrice",
            Formula = "AVG(Price)"
        };

        var result = await sut.CreateCalculation(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculation-entities");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("NewCalc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateCalculationReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new CreateCalculationDefinitionRequest { Name = "Bad" };

        var result = await sut.CreateCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new CreateCalculationDefinitionRequest { Name = "Err" };

        var result = await sut.CreateCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateCalculationSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var expected = new CalculationDetailPayload { Id = id, Name = "Updated" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new UpdateCalculationDefinitionRequest
        {
            Name = "Updated",
            ResultFieldName = "Total",
            Formula = "SUM(Amount)"
        };

        var result = await sut.UpdateCalculation(id, request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/calculation-entities/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Updated");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateCalculationReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);
        var request = new UpdateCalculationDefinitionRequest { Name = "Missing" };

        var result = await sut.UpdateCalculation(Guid.NewGuid(), request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new UpdateCalculationDefinitionRequest { Name = "Err" };

        var result = await sut.UpdateCalculation(Guid.NewGuid(), request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteCalculationSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteCalculation(id, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/calculation-entities/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteCalculationReturnsFailureOnNotFound()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteCalculation(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteCalculation(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ValidateFormulaSendsCorrectRequest()
    {
        var expected = new PreviewFormulaResponse { IsValid = true, InferredResultType = "decimal" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new ValidateFormulaPayload { Formula = "SUM(Amount)", TargetDataSet = "Sales" };

        var result = await sut.ValidateFormula(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculation-entities/validate-formula");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ValidateFormulaReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new ValidateFormulaPayload { Formula = "INVALID" };

        var result = await sut.ValidateFormula(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ValidateFormulaReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new ValidateFormulaPayload { Formula = "ERR" };

        var result = await sut.ValidateFormula(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task PreviewCalculationSendsCorrectRequest()
    {
        var expected = new PreviewCalculationResponse
        {
            CalculationType = "Sum",
            Result = 42m,
            SampleData = [10m, 20m, 12m]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new PreviewCalculationRequest { CalculationType = "Sum", SampleSize = 5 };

        var result = await sut.PreviewCalculation(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/calculations/preview");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Result.ShouldBe(42m);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task PreviewCalculationReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);
        var request = new PreviewCalculationRequest { CalculationType = "Bad" };

        var result = await sut.PreviewCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task PreviewCalculationReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new PreviewCalculationRequest { CalculationType = "Err" };

        var result = await sut.PreviewCalculation(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataSetFieldsSendsCorrectRequest()
    {
        var expected = new List<DataSetFieldPayload>
        {
            new() { Name = "Amount", DataType = "decimal", IsKey = false }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDataSetFields("Sales", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datasets/Sales/fields");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Amount");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataSetFieldsEncodesSpecialCharacters()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<DataSetFieldPayload>())
        });
        var sut = CreateClient(handler);

        await sut.GetDataSetFields("My Data Set", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datasets/My%20Data%20Set/fields");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataSetFieldsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetDataSetFields("Sales", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
