using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Fdw.Web.RestEndpoints.ApiReference;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>Registers the page this project's policy tests declare, once per test process.</summary>
/// <remarks>
/// The registration generator emits the cross-assembly RegisterMember calls for every assembly this one references, and never for
/// the assembly it runs in, so <see cref="PolicyTestPage"/> is not in <see cref="ApiReferencePages"/> unless this assembly puts it
/// there. The collection freezes on its first read and a member registered after that throws; a module initializer runs when
/// this assembly loads, before any test can read the collection, whatever order the tests run in.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class PolicyTestPageRegistration
{
    [ModuleInitializer]
    internal static void Register() => ApiReferencePages.RegisterMember(new PolicyTestPage());
}
