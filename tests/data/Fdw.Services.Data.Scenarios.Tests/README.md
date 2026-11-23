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

## The fixture (`Fixture/`)

The scenarios need containers and an options table on the host's data store. `Fixture/` creates them. All four scripts are idempotent, take their names as
`sqlcmd -v` variables with no default, and were **written but not run** by their author; the field declarations copy the values the databases-seed set uses
for every field (DatumTypeId, ClassificationId, FrameworkDataType from the data type), and the declaration inserts follow the route owner's proof scripts. The first
run on a scratch ConfigurationDb is the test of them.

| Script | Where it runs | What it does |
|---|---|---|
| `setup-data.sql` | the DATA database the data store points at | creates the schema (if missing) and the tables `Teams` (Division, Conference), `Players` (Name), `ScenarioValues` (Division: East x3, West x2, North x4, South x1, Eastern Shore x5) and `ScenarioDataOption` (the options table: Id with no default, then the seven option row columns); grants a principal rights on them |
| `setup-configuration.sql` | ConfigurationDb | declares those four tables as containers of an existing data store and path with their fields and a Surrogate key on Id each (without a key every paged read refuses the container: databases-seed seed 38; the script stops if one has none) (Id of the options table is not system-provided) and adds the three DataOptions server settings that name `ScenarioDataOption` |
| `teardown-data.sql`, `teardown-configuration.sql` | the same two | remove them; the schema and the three settings are left (see the comments) |

```
sqlcmd -S <server> -d <data database>  -b -I -v Schema=<schema> DatabaseUser=<principal> -i Fixture/setup-data.sql
sqlcmd -S <server> -d <ConfigurationDb> -b -I -v DataStore=<data store> Path=<path>       -i Fixture/setup-configuration.sql
```

It assumes: the data store and a path named like the schema already exist; the host's connection signs in as the principal given; the host reads
settings after a restart if it caches them. Then set `FDW_SCENARIO_DATASTORE`, `FDW_SCENARIO_PATH` and `FDW_SCENARIO_VALUES_CONTAINER=ScenarioValues`.

**The dev window (Release-Supervisor, 2026-11-18).** The scenarios run on dev only, after the 1.6.0 dev deploy has passed and dev answers, in a window build-agent announces and never during a database reset. Every SQL script refuses any database but `devDataDb` or `devConfigurationDb` (it THROWs and `sqlcmd -b` stops). The setup scripts print the three DataOptions settings as they were (`OLD <name>=<value>`, `(none)` if absent) and change them to the fixture's; keep those lines, the teardown takes them as `-v OldDataStore=.. OldPath=.. OldContainer=..` and puts the settings back (or deletes the rows the setup added). **Restart the host after the setup and after the teardown**: settings and container declarations are cached. Only build-agent runs the setup, on a deploy-kind queue entry naming the repo and the window.

**The three test people (`Fixture/people.sh`).** `people.sh create` makes the roles `scenario-read-role` (datastores:read) and `scenario-write-role` (datastores:read and datastores:write: the scenarios list rows as the writer) and the users `scenario-read`, `scenario-write` and `scenario-none` (no role) through the admin API, signs each in and writes a token to a mode-600 file in a directory outside any git work tree; `people.sh token` signs them in again (an access token expires: run it just before the run); `people.sh remove` deletes the users, the roles and the files. It needs four variables (none has a default): `FDW_SCENARIO_HOST`, `FDW_SCENARIO_CONFIRM` (the same host typed again), `FDW_ADMIN_TOKEN_FILE` (an administrator's token in a file the host owner supplies) and `FDW_SCENARIO_PEOPLE_DIR`. No token or password is printed, passed as an argument or kept anywhere but those files. Not run: the first run is its test.

**What the fixture does not create:** the administrator's token for `people.sh` (the host owner supplies it) and the variables of a run: `FDW_SCENARIO_TOKEN_READ`, `_WRITE` and `_NONE` are set from the three token files without printing them. A token is never written into the repo, a message, an item or a commit. Nor can it create the second host the DATA-121670 scenario needs: that scenario is alone in `Features/DataOptionsTableNotNamed.feature`, tagged `options-table-not-named`, with no Background (a Background that lists or clears rows would itself be refused when the table is not named). Decision (Release-Supervisor, 2026-11-18): the project is NOT run twice. With a host named and no second host that scenario FAILS naming the missing variable `FDW_SCENARIO_HOST_WITHOUT_OPTIONS_TABLE` (it does not skip: a host was named, so a missing variable is a fault), and the report shows that reason. FDW-6792 asks for the second host; when it exists, set that variable to its address. The tag `options-table-not-named` also lets a run exclude or select the scenario; that Reqnroll turns the tag into a trait `dotnet test --filter` can select is not verified.
