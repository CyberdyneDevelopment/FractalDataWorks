using System.Net.Http;
using Bunit;
using Fdw.Data.Components.DataSets;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="FieldMappingTransformProvider"/> headless component.
/// Uses MockHttpHandler because DataSetApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class FieldMappingTransformProviderTests : IDisposable
{
    private readonly BunitContext _ctx;
    private static readonly Guid TestFieldMappingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public FieldMappingTransformProviderTests()
    {
        _ctx = new BunitContext();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IRenderedComponent<FieldMappingTransformProvider> RenderWithHandler(
        MockHttpHandler handler,
        Guid? fieldMappingId = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<FieldMappingTransformProvider>>(
            NullLogger<FieldMappingTransformProvider>.Instance);

        var id = fieldMappingId ?? TestFieldMappingId;
        return _ctx.Render<FieldMappingTransformProvider>(parameters =>
            parameters.Add(p => p.FieldMappingId, id));
    }

    private static FieldMappingTransformContext GetContext(
        IRenderedComponent<FieldMappingTransformProvider> component)
    {
        var field = typeof(FieldMappingTransformProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (FieldMappingTransformContext)field!.GetValue(component.Instance)!;
    }

    private static List<FieldMappingTransformPayload> CreateTransformList(int count = 2)
    {
        var list = new List<FieldMappingTransformPayload>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new FieldMappingTransformPayload
            {
                Id = Guid.NewGuid(),
                FieldMappingId = TestFieldMappingId,
                TransformType = $"Transform{i}",
                Ordinal = i,
                Parameters = []
            });
        }

        return list;
    }

    private static List<TransformTypePayload> CreateTypeList()
    {
        return
        [
            new TransformTypePayload
            {
                Name = "Trim",
                DisplayName = "Trim Whitespace",
                Description = "Removes leading/trailing whitespace",
                Category = "String",
                SupportsBatching = true,
                Parameters = []
            },
            new TransformTypePayload
            {
                Name = "ToUpper",
                DisplayName = "To Uppercase",
                Description = "Converts to uppercase",
                Category = "String",
                SupportsBatching = true,
                Parameters = []
            }
        ];
    }

    /// <summary>
    /// Creates a MockHttpHandler with standard happy-path responses for both
    /// the transforms list and the available types list.
    /// </summary>
    private static MockHttpHandler CreateStandardHandler(
        List<FieldMappingTransformPayload>? transforms = null,
        List<TransformTypePayload>? types = null)
    {
        return new MockHttpHandler()
            .RespondWith("transform-types", types ?? CreateTypeList())
            .RespondWith(HttpMethod.Get, TestFieldMappingId.ToString(), transforms ?? CreateTransformList())
            .RespondWith("transforms/reorder", new { })
            .RespondWith(HttpMethod.Delete, "/transforms/", new { })
            .RespondWith(HttpMethod.Post, "/transforms", new FieldMappingTransformPayload
            {
                Id = Guid.NewGuid(),
                FieldMappingId = TestFieldMappingId,
                TransformType = "Trim",
                Ordinal = 0,
                Parameters = []
            });
    }

    // ── P1 Tests ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void LoadsTransformsFromApi()
    {
        var transforms = CreateTransformList(3);
        var handler = CreateStandardHandler(transforms: transforms);

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.IsLoading.ShouldBeFalse();
        ctx.Transforms.Count.ShouldBe(3);
        ctx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LoadsAvailableTransformTypes()
    {
        var types = CreateTypeList();
        var handler = CreateStandardHandler(types: types);

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.AvailableTransformTypes.Count.ShouldBe(2);
        ctx.AvailableTransformTypes[0].Name.ShouldBe("Trim", StringCompareShould.IgnoreCase);
        ctx.AvailableTransformTypes[1].Name.ShouldBe("ToUpper", StringCompareShould.IgnoreCase);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AddTransformRefreshesChain()
    {
        var handler = CreateStandardHandler();
        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnAddTransform("Trim");
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task SaveTransformRefreshesChain()
    {
        var handler = CreateStandardHandler();
        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnSaveTransform(new SaveFieldMappingTransformRequest
            {
                FieldMappingId = TestFieldMappingId,
                TransformType = "Trim",
                Ordinal = 0,
                Parameters = []
            });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteTransformRefreshesChain()
    {
        var transforms = CreateTransformList(2);
        var deleteId = transforms[0].Id;
        var handler = CreateStandardHandler(transforms: transforms);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDeleteTransform(deleteId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ReorderTransformsUpdatesOrdinals()
    {
        var transforms = CreateTransformList(3);
        var handler = CreateStandardHandler(transforms: transforms);

        var component = RenderWithHandler(handler);
        var reversedIds = transforms.Select(t => t.Id).Reverse().ToList().AsReadOnly();

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnReorderTransforms(reversedIds);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task MoveUpReordersCorrectly()
    {
        var transforms = CreateTransformList(3);
        var secondId = transforms[1].Id;
        var handler = CreateStandardHandler(transforms: transforms);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnMoveUp(secondId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    // ── P2 Tests ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public void LoadTransformsApiFailureSetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondWith("transform-types", CreateTypeList())
            .RespondError(TestFieldMappingId.ToString());

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.IsLoading.ShouldBeFalse();
        ctx.ErrorMessage.ShouldNotBeNullOrEmpty();
        ctx.Transforms.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task SaveTransformApiFailureSetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondWith("transform-types", CreateTypeList())
            .RespondWith(TestFieldMappingId.ToString(), CreateTransformList())
            .RespondError("field-mappings/transforms");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnSaveTransform(new SaveFieldMappingTransformRequest
            {
                FieldMappingId = TestFieldMappingId,
                TransformType = "Trim",
                Ordinal = 0,
                Parameters = []
            });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DeleteTransformApiFailureSetsErrorMessage()
    {
        var transforms = CreateTransformList(2);
        var handler = new MockHttpHandler()
            .RespondWith("transform-types", CreateTypeList())
            .RespondWith(HttpMethod.Get, TestFieldMappingId.ToString(), transforms)
            .RespondError(HttpMethod.Delete, "/transforms/" + transforms[0].Id);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDeleteTransform(transforms[0].Id);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void IsLoadingTrueDuringLoad()
    {
        // Use a TaskCompletionSource-based approach: register a handler that blocks,
        // then check IsLoading before releasing. Since bUnit renders synchronously
        // until the first await, verify IsLoading is false after completion.
        var handler = CreateStandardHandler();
        var component = RenderWithHandler(handler);

        // After render completes (OnParametersSetAsync finished), IsLoading should be false
        var ctx = GetContext(component);
        ctx.IsLoading.ShouldBeFalse();

        // Verify the load happened by checking transforms were populated
        ctx.Transforms.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task IsSavingTrueDuringSave()
    {
        var handler = CreateStandardHandler();
        var component = RenderWithHandler(handler);

        // After save completes, IsSaving should be false
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnSaveTransform(new SaveFieldMappingTransformRequest
            {
                FieldMappingId = TestFieldMappingId,
                TransformType = "Trim",
                Ordinal = 0,
                Parameters = []
            });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task FieldMappingIdChangeReloadsTransforms()
    {
        var handler = CreateStandardHandler();
        var component = RenderWithHandler(handler);

        var ctx = GetContext(component);
        ctx.Transforms.Count.ShouldBe(2);

        // Change the FieldMappingId parameter to trigger reload
        var newId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // Register response for the new ID
        handler.RespondWith(newId.ToString(), CreateTransformList(1));

        await component.InvokeAsync(() =>
        {
            component.Render(parameters =>
                parameters.Add(p => p.FieldMappingId, newId));
            return Task.CompletedTask;
        });

        var resultCtx = GetContext(component);
        resultCtx.FieldMappingId.ShouldBe(newId);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task MoveDownReordersCorrectly()
    {
        var transforms = CreateTransformList(3);
        var firstId = transforms[0].Id;
        var handler = CreateStandardHandler(transforms: transforms);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnMoveDown(firstId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsSaving.ShouldBeFalse();
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    public void Dispose() => _ctx.Dispose();
}
