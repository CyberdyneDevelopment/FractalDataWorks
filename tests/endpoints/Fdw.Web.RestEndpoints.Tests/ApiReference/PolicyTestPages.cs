using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>The callers, configuration and requests the page-policy tests share.</summary>
internal static class PolicyTestPages
{
    public const string DeclaredPolicy = "test:open";

    public const string ConfiguredPolicy = "test:configured";

    public const string Key = "ApiReference:Pages:PolicyTestPage:RequiredPolicies";

    public static IApiReferencePage Page => ApiReferencePages.All().Single(page => page.Name == "PolicyTestPage");

    public static IConfiguration Configuration(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();

    public static ApiReferenceCaller SignedIn(params string[] permissions) =>
        ApiReferenceCaller.From(
            new ClaimsPrincipal(new ClaimsIdentity(permissions.Select(permission => new Claim(ClaimDefinitions.perm.Name, permission)), "test")),
            new Mock<ISystemRoleConfiguration>().Object);

    public static ApiReferenceCaller Anonymous() =>
        ApiReferenceCaller.From(new ClaimsPrincipal(new ClaimsIdentity()), new Mock<ISystemRoleConfiguration>().Object);

    public static HttpContext Request(IConfiguration configuration) =>
        new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(configuration).BuildServiceProvider(),
        };
}
