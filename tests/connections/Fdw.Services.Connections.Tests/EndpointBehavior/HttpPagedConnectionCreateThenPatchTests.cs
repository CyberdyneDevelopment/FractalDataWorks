using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Paging;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.Http.Validation;
using Fdw.Services.Settings;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// FDW-5139 (found on dev with FDW-4938): a paged HTTP connection is created, stored (which keeps no Lifetime: it has no column), then
/// edited by PATCH, as the console and the API do it. Each edit passes the endpoint rules and the write validator; before the fix every
/// edit was refused for the empty Lifetime.
/// </summary>
public sealed class HttpPagedConnectionCreateThenPatchTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void APagedHttpConnectionCanBeCreatedThenEditedThenItsStyleChanged()
    {
        var behavior = HttpBehavior();

        // Create, as POST /connections fills and binds it.
        var filled = ConnectionCreateDefaults.Fill(
            Json("""
                { "baseUrl": "https://remote.test/", "protocol": "Rest", "authenticationType": "None", "lifetime": "Scoped",
                  "pagingStyle": "OffsetLimit",
                  "authentication": { "PagingOffsetParameter": "offset", "PagingLimitParameter": "limit", "PagingPageSize": "50" } }
                """),
            behavior, Option(), new Lazy<IEffectiveSettingsProvider>(() => new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict).Object),
            Guid.Empty, "Walk", NullLogger.Instance);
        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        var created = behavior.Bind(filled.Value, NullLogger.Instance);
        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        Valid(created.Value!);

        // Stored and read back: there is no Lifetime column, so it comes back empty.
        var stored = created.Value.ShouldBeOfType<HttpConnectionConfiguration>();
        stored.Lifetime = string.Empty;

        // An edit that keeps the style.
        var edited = behavior.Merge(Json("""{ "timeoutSeconds": 40 }"""), stored, NullLogger.Instance);
        edited.IsSuccess.ShouldBeTrue(edited.CurrentMessage);
        Valid(edited.Value!);

        // The console's style change: the new style, and the old style's settings it does not read sent blank.
        var changed = behavior.Merge(
            Json("""
                { "pagingStyle": "PageNumber",
                  "authentication": { "PagingOffsetParameter": "", "PagingLimitParameter": "",
                                      "PagingPageNumberParameter": "page", "PagingPageSizeParameter": "size", "PagingFirstPageNumber": "1" } }
                """),
            edited.Value!, NullLogger.Instance);
        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        changed.Value.ShouldBeOfType<HttpConnectionConfiguration>().PagingStyle.ShouldBe("PageNumber");
        Valid(changed.Value!);
    }

    // The write validator a save runs (HttpConnectionConfigurationValidation).
    private static void Valid(IConnectionImplementationConfiguration configuration)
    {
        var validated = new HttpConnectionConfigurationValidator().Validate((HttpConnectionConfiguration)configuration);
        validated.IsValid.ShouldBeTrue(string.Join("; ", validated.Errors.Select(error => error.ErrorMessage)));
    }

    // The Http connection type's endpoint behaviour as HttpConnectionType declares it: the flat `authentication` alias, and the paging style
    // as the second owner of the authentication dictionary.
    private static ConnectionEndpointBehavior HttpBehavior()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(HttpConnectionConfiguration));
        behavior.Alias("authentication", nameof(HttpConnectionConfiguration.AdditionalProperties));
        behavior.Authentication(
            nameof(HttpConnectionConfiguration.AdditionalProperties),
            "Http",
            configuration => HttpAuthenticationTypes.ByName(((HttpConnectionConfiguration)configuration).AuthenticationType),
            configuration => HttpPagingStyles.ByName(((HttpConnectionConfiguration)configuration).PagingStyle ?? string.Empty).ExpectedProperties);
        return behavior;
    }

    private static IServiceType Option()
    {
        var option = new Mock<IServiceType>();
        option.SetupGet(o => o.Name).Returns("Http");
        return option.Object;
    }

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }
}
