# Baselines

A baseline is the record of the last release (FDW-5709): the project manifest (`changeset.py manifest`) of the tree the release was made
from, plus what the plan needs to know about the packages. The next release compares the current tree with it to find the packages that
changed, and compares the versions held on nuget.org with it to tell its own earlier push from the last release's.

`release-FDW-<N>.json` is the record of release N. It states:

- `projects` and `globals`: every project's git tree id and the hashes of the repo-wide build files (the content of the tag `release-FDW-<N>`);
- `assemblyVersionFloors` ({assembly: x.y.z.w}): for each assembly, the higher of the version it carries and the highest version any dll of
  that release asks for it at; a package that goes to 2.0.0 never raises the floor of a 1.x one. A plan stops without them
  (BASELINE_FLOORS_MISSING);
- `packagesWithoutRuntimeAssembly`: the packages that carry none (source generators and tools); nothing binds to them at run time, so they
  have no floor, and any other package without one stops the release;
- `release`: N, which must equal the file name;
- `packages` ({package id: exact version}): every package of the release at the version it had in it, tools included. The packages the
  release did not rebuild are in it at their older versions.

Who writes it: the run. The job's last phase (`release-allocated.sh record`, `release.py record`) writes `release-work/baseline.json` from the
plan and from what `bindings` measured, prints it and uploads it as the artifact `release-record-<version>`. After the green run and the tag
the release owner commits it here as `release-FDW-<N>.json` (data only, in no package's closure). A candidate (`N-rc.k`) writes a record
too, but it is never committed: the baseline is always a stable release.

Who reads it: the plan. `baseline_file` (or the variable `NUGET_BASELINE_FILE`) names the record to plan from; empty takes the record with
the highest number in this folder and prints which. The plan refuses, naming why: a missing file or folder, a record not named
`release-FDW-<N>.json`, one without `release`, `packages` or the manifest, one whose release differs from its name, and one that is not the
latest (a newer record here, or a newer release tag where the checkout has tags; the GitHub mirror has none).

`release-FDW-1.5.5.json` is the manifest of the 1.5.5 release tree (git tag `release-FDW-1.5.5`), the last release under the repo-wide
version, made for the first allocated release; it has no `release` or `packages`, so a plan refuses it as a baseline now, and it stays as
history. `release-FDW-1.5.7.json` was made once, read-only, from the published 1.5.7 release at the end of the era in which a release also
published a package that carried this data; it is the baseline of the release after 1.5.7.

Made by hand (never needed again): `python3 public/tools/versioning/changeset.py manifest --repo . --ref release-FDW-<N>`, plus the floors
measured from the published packages: `python3 public/tools/versioning/assemblyid.py identity --package X.nupkg --out DIR/X.txt` for each,
then `python3 public/tools/versioning/bindcheck.py floors --dir DIR` and `bare --dir DIR`.
