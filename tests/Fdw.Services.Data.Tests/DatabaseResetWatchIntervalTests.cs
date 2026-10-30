using System;
using System.Collections.Generic;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// How often a host looks at its configuration databases for a reset is the setting ResetWatchIntervalSeconds (FDW-3207): the watcher is
/// built from it and from nothing else, and a host that has settings and no positive value does not start.
/// </summary>
public sealed class DatabaseResetWatchIntervalTests
{
    private sealed class NoSources : IDatabaseGenerationCatalog
    {
        public IReadOnlyList<IDatabaseGenerationSource> Sources() => [];
    }

    private sealed class EmptyInvalidator : ICacheInvalidator
    {
        public void InvalidateByTag(string tag)
        {
        }

        public void InvalidateByTags(IEnumerable<string> tags)
        {
        }

        public void InvalidateAll()
        {
        }
    }

    private static Mock<IEffectiveSettingsProvider> Settings(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.ResetWatchIntervalSeconds, null, null)).Returns(seconds);
        return settings;
    }

    private static IServiceCollection Host(bool withCache)
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<IDatabaseGenerationCatalog>(new NoSources())
            .AddSingleton(accessor.Object);
        return withCache ? services.AddSingleton<ICacheInvalidator>(new EmptyInvalidator()) : services;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheIntervalIsTheSettingInSeconds()
    {
        var read = DatabaseResetWatchInterval.Read(Settings(30).Object, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData(0)]
    [InlineData(-3)]
    public void AnIntervalThatIsNotPositiveFailsNamingTheSetting(int seconds)
    {
        var read = DatabaseResetWatchInterval.Read(Settings(seconds).Object, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        string.Concat(read.CurrentMessage).ShouldContain("ResetWatchIntervalSeconds");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheWatcherAHostStartsWithLooksAtTheIntervalTheSettingGives()
    {
        using var services = Host(withCache: true).AddSingleton(Settings(45).Object).BuildServiceProvider();

        DatabaseResetWatcher.Create(services).Interval.ShouldBe(TimeSpan.FromSeconds(45));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AHostWhoseSettingIsNotPositiveDoesNotGetAWatcherAndNamesTheSetting()
    {
        using var services = Host(withCache: true).AddSingleton(Settings(0).Object).BuildServiceProvider();

        var failure = Should.Throw<InvalidOperationException>(() => DatabaseResetWatcher.Create(services));

        failure.Message.ShouldContain("ResetWatchIntervalSeconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AHostThatRegistersNoSettingsWatchesNothingAndAssumesNoInterval()
    {
        using var services = Host(withCache: true).BuildServiceProvider();

        DatabaseResetWatcher.Create(services).Interval.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AHostThatKeepsNoCacheWatchesNothingAndDoesNotReadTheSetting()
    {
        var settings = Settings(0);
        using var services = Host(withCache: false).AddSingleton(settings.Object).BuildServiceProvider();

        DatabaseResetWatcher.Create(services).Interval.ShouldBeNull();
        settings.Verify(s => s.GetEffectiveValue<int>(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Never);
    }
}
