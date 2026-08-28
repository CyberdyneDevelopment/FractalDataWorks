using System;
using System.Collections.Generic;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Unit coverage for <see cref="RecordRowValidator"/> — fix #1: a declared non-nullable field that is
/// ABSENT from a decoded row (or present as null) must fail loud, never silently default via the
/// generated PocoMapper's GetOrdinal→IndexOutOfRangeException catch.
/// </summary>
public sealed class RecordRowValidatorTests
{
    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
            dict[key] = value;
        return dict;
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "RequiredField")]
    public void ValidateFailsWhenADeclaredNonNullableFieldIsAbsentFromARow()
    {
        var container = ContainerStub.Build("EnvironmentVariableSecretManager", ("Prefix", false));
        var rows = new List<IReadOnlyDictionary<string, object?>> { Row(("SecretManagerId", Guid.NewGuid())) };

        var result = RecordRowValidator.Validate(rows, container, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "RequiredField")]
    public void ValidateFailsWhenADeclaredNonNullableFieldIsExplicitlyNullInARow()
    {
        var container = ContainerStub.Build("EnvironmentVariableSecretManager", ("Prefix", false));
        var rows = new List<IReadOnlyDictionary<string, object?>> { Row(("Prefix", null)) };

        var result = RecordRowValidator.Validate(rows, container, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateSucceedsWhenEveryDeclaredNonNullableFieldIsPresentAndNonNull()
    {
        var container = ContainerStub.Build("SecretManager", ("Name", false), ("Description", true));
        var rows = new List<IReadOnlyDictionary<string, object?>> { Row(("Name", "EnvSecrets")) };

        var result = RecordRowValidator.Validate(rows, container, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateSucceedsWhenANullableFieldIsAbsentFromARow()
    {
        var container = ContainerStub.Build("SecretManager", ("Name", false), ("Description", true));
        var rows = new List<IReadOnlyDictionary<string, object?>> { Row(("Name", "EnvSecrets")) };

        var result = RecordRowValidator.Validate(rows, container, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "RequiredField")]
    public void ValidateFailsOnTheOffendingRowEvenWhenAnEarlierRowIsValid()
    {
        var container = ContainerStub.Build("SecretManager", ("Name", false));
        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row(("Name", "EnvSecrets")),
            Row(("SomeOtherColumn", "x")),
        };

        var result = RecordRowValidator.Validate(rows, container, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateSucceedsForAnEmptyRowSet()
    {
        var container = ContainerStub.Build("SecretManager", ("Name", false));

        var result = RecordRowValidator.Validate([], container, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }
}
