using System;
using System.Linq;
using Fdw.Services.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// FDW-6221: where a cited member stands is an option of <see cref="CitedStates"/>, named exactly as the enum members it replaces, so the text a route or a
/// stored row carries for a state does not change.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class CitedStatesTests
{

    [Fact]
    public void TheCollectionHoldsTheFourStatesAndNoOther()
    {
        CitedStates.All().Select(state => state.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()
            .ShouldBe(["Absent", "Current", "Retired", "Unchecked"]);
    }
}
