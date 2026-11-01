using System;
using System.Linq;
using System.Reflection;
using Fdw.MessageLogging;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// An "already exists" refusal is a conflict with what is stored, so it answers 409. The status comes from the number
/// of the message (its leading digit is the category), so a duplicate logged in the NotFound band (3xxxx) answered
/// 404 and sent callers hunting for a missing path (FDW-1050).
/// </summary>
public class AlreadyExistsStatusTests
{
    [Fact]
    public void A_duplicate_container_answers_409_not_404()
    {
        var result = GenericResult.Failure(
            DataStoreEndpointLog.ContainerAlreadyExists(NullLogger.Instance, "all_day", "earthquakes", "UsgsFeed"));

        var (status, _) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(409);
    }

    [Fact]
    public void Every_already_exists_log_in_this_package_is_in_the_conflict_band()
    {
        var misbanded = typeof(DataStoreEndpointLog).Assembly.GetTypes()
            .Where(type => type.IsAbstract && type.IsSealed && type.Name.EndsWith("Log", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name.Contains("AlreadyExists", StringComparison.Ordinal))
                .Select(method => (Name: $"{type.Name}.{method.Name}", Attribute: method.GetCustomAttribute<MessageLoggingAttribute>())))
            .Where(entry => entry.Attribute is null || entry.Attribute.EventId is < 40000 or >= 50000)
            .Select(entry => entry.Name)
            .ToList();

        misbanded.ShouldBeEmpty();
    }
}
