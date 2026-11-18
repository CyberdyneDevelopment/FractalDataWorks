# Fdw.Services.Data.Scenarios.Tests

The data option routes of FDW-5663 written as Gherkin scenarios (Reqnroll on xUnit v3). Scenarios are tagged with the story whose route they check (@FDW-5664, @FDW-5665, @FDW-5666) and the feature files carry @FDW-5663. They call a running host over HTTP; they name no FDW project and mock nothing.

A run writes a Cucumber Messages file to `report/cucumber_messages.ndjson` (reqnroll.json). TechCom reads that file.

## What a run needs (environment variables, none has a default; a missing one stops the scenario and names the variable)

| Variable | Meaning |
|---|---|
| FDW_SCENARIO_HOST | Base address of the host that serves the routes. |
| FDW_SCENARIO_HOST_WITHOUT_OPTIONS_TABLE | A host whose three DataOptions settings are NOT set, for the DATA-121670 scenario. |
| FDW_SCENARIO_DATASTORE, FDW_SCENARIO_PATH | The data store and path under which the containers below are declared. |
| FDW_SCENARIO_TOKEN_READ, _WRITE, _NONE | Bearer tokens of three people: datastores:read only, datastores:write, and neither. Never printed. |
| FDW_SCENARIO_VALUES_CONTAINER | A container dedicated to the column values scenarios whose column "Division" holds exactly the values listed in ColumnValues.feature's Background (the first step of each such scenario checks this and fails naming the difference). |

The option rows scenarios use two containers named "Teams" and "Players" under that data store and path, each with the columns "Division" and "Conference" (Teams) and "Name" (Players), and a data options table named by the host's three server settings. They remove every row they wrote.

Nothing assumes tst. The run is started by TechCom's runner (TC-1578) or by build-agent, after the build, against the host named by the variables.
