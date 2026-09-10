using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Provides centralized registry and resolution for logical DataSet <em>configurations</em>.
/// Returns <see cref="DomainConfiguration"/> records — not runtime services.
/// Use <c>IDataSetProvider</c> (in <c>Fdw.Services.Data.Abstractions</c>) when you need the live <see cref="Fdw.Data.Abstractions.IDataSet"/> runtime.
/// </summary>
/// <remarks>
/// Merges three configuration sources in priority order:
/// <list type="number">
/// <item><description>IOptionsMonitor (ctrl/system DataSets from configurationSchema.json)</description></item>
/// <item><description>ConfigurationDb (user-defined DataSets in the cfg schema)</description></item>
/// <item><description>DataSetTypes TypeCollection (code-defined static DataSets)</description></item>
/// </list>
/// </remarks>
public interface IDataSetConfigurationProvider
{

}
