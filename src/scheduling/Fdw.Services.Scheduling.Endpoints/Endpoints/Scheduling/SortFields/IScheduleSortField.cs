using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>A column a schedule list can be sorted by.</summary>
public interface IScheduleSortField : ITypeOption<int, ScheduleSortFieldBase>
{
    /// <summary>Gets the lower-case word a request's <c>SortBy</c> names the column by.</summary>
    string Key { get; }

    /// <summary>Orders the schedules by this column.</summary>
    /// <param name="schedules">The schedules to order.</param>
    /// <param name="descending">Whether the largest value comes first.</param>
    /// <returns>The schedules in order.</returns>
    IEnumerable<IScheduleImplementationConfiguration> Order(IEnumerable<IScheduleImplementationConfiguration> schedules, bool descending);
}
