# Baselines

A baseline is the project manifest (`changeset.py manifest`) of the tree a release was made from: every project's
git tree id and the hashes of the repo-wide build files. The next allocated release compares the current tree with
it to find the packages that changed.

`release-FDW-1.5.5.json` is the manifest of the 1.5.5 release tree (git tag `release-FDW-1.5.5`), the last release
under the repo-wide version. The 1.5.5 patch republished every package at 1.5.5, so it is what the
published packages were built from. It is the baseline of the FIRST allocated release only, passed as `baseline_file`
(or the variable `NUGET_BASELINE_FILE`) to `publish-nuget.yml`. That release publishes `Fdw.Bundle`, which carries its
own manifest at `fdw/manifest.json`, and every later release reads its baseline from the latest stable bundle, so no
further file is added here.

The manifest also states `assemblyVersionFloors` ({assembly: x.y.z.w}): for each assembly, the higher of the version it carries and the
highest version any dll of that release asks for it at. A git ref holds no dlls, so they are measured from the published packages:
`python3 public/tools/versioning/assemblyid.py identity --package X.nupkg --out DIR/X.txt` for each package of the release, then
`python3 public/tools/versioning/bindcheck.py floors --dir DIR`. A plan stops without them (BASELINE_FLOORS_MISSING). Each later release
records its own in the project manifest its bundle carries. For 1.5.5 every assembly is 1.5.5.0, because each lockstep release stamped its
own number. `packagesWithoutRuntimeAssembly` lists the packages that carry none (source generators and tools; `bindcheck.py bare --dir DIR`):
nothing binds to them at run time, so they have no floor, and any other package without one stops the release.

Made with: `python3 public/tools/versioning/changeset.py manifest --repo . --ref release-FDW-1.5.5`, plus the floors above.
