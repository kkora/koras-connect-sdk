# Release candidate and publication runbook

Current version: 0.1.0-preview.2. Public publishing is disabled. This runbook and the workflow are implemented; no public release has occurred.

## Reproduce validation

```sh
dotnet restore Koras.Connect.slnx
dotnet build Koras.Connect.slnx -c Release --no-restore
dotnet test Koras.Connect.slnx -c Release --no-build
dotnet pack Koras.Connect.slnx -c Release --no-build -o artifacts
python eng/validate_packages.py
python eng/generate_sbom.py
python eng/release_checks.py
python -m unittest discover -s tests/release -v
dotnet restore samples/PackageConsumer/PackageConsumer.csproj --configfile samples/PackageConsumer/NuGet.Config
dotnet run --project samples/PackageConsumer/PackageConsumer.csproj -c Release --no-restore
```

Use a clean artifacts directory for a version change; do not mix old packages with current candidates. Four nupkg archives plus sbom.spdx.json are retained as CI artifacts. Validate both OS jobs and the exact PR head. The core has no external runtime dependency; SAP and Qualtrics depend on the core only; ADP also uses Microsoft.Extensions.Http.

## Before a future public release

Resolve G0/G3 acceptance and tenant verification, confirm vendor terms, package ownership/reserved names and license/distribution rights, record owner authorization for a specific version, select an approved SPDX package license, and configure the protected nuget-release environment/API key. No sensitive credential belongs in the approval record. Version changes must update Directory.Build.props, release_manifest.json and the independent consumer references together.

The publication validator requires a matching vVERSION tag, manifest publicationApproved=true, reviewed publication_approval.json and package checksums. The approval record must identify version, approved=true and licenseExpression. The workflow defaults to validate-only; publishing needs an explicit dispatch input. The workflow does not configure account permissions or create an approval record for the owner.

Publishing can partially succeed across four packages if a remote service fails mid-run. NuGet versions cannot be overwritten. Review which exact packages/version arrived before retrying; never silently replace an existing version or mark all packages published from a partial result. No automatic rollback, unlisting, release tagging or GitHub release creation occurs.
