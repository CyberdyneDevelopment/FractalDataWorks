using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data.Tests;

/// <summary>Tests for attaching implementation providers to their domain providers (FDW-3966): a failed attachment stops the run and is returned, never dropped.</summary>
public sealed class DataDomainRegistrationsTests
{
    private static IGenericResult Failed() => GenericResult.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AFailedAttachmentIsReturnedAndTheStepsAfterItAreNotRun()
    {
        var ran = new List<string>();
        var steps = new List<Func<IGenericResult>>
        {
            () => { ran.Add("first"); return GenericResult.Success(); },
            () => { ran.Add("second"); return Failed(); },
            () => { ran.Add("third"); return GenericResult.Success(); },
        };

        var result = DataDomainRegistrations.FirstFailure(steps);

        result.IsFailure.ShouldBeTrue();
        ran.ShouldBe(["first", "second"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void EveryAttachmentRunsAndTheRunSucceedsWhenNoneFails()
    {
        var ran = 0;
        var steps = new List<Func<IGenericResult>> { () => { ran++; return GenericResult.Success(); }, () => { ran++; return GenericResult.Success(); } };

        DataDomainRegistrations.FirstFailure(steps).IsSuccess.ShouldBeTrue();
        ran.ShouldBe(2);
    }
}
