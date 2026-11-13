"""Fails if any of the independent places a release version is written drift out of sync: Directory.Build.props
(the actual build-time Version every package gets), eng/release_manifest.json (what release_checks.py validates
against), and every sample consumer's hardcoded PackageReference Version (which dotnet pack/build never touches,
so nothing else would ever catch a stale sample after a version bump).
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def build_props_version(root=ROOT):
    match = re.search(r"<Version>([^<]+)</Version>", (root / "Directory.Build.props").read_text(encoding="utf-8"))
    if not match: raise ValueError("Directory.Build.props has no <Version> element")
    return match.group(1)


def manifest_version(root=ROOT):
    return json.loads((root / "eng/release_manifest.json").read_text(encoding="utf-8"))["version"]


def sample_versions(root=ROOT):
    found = {}
    for csproj in sorted((root / "samples").glob("*/*.csproj")):
        for match in re.finditer(r'<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"', csproj.read_text(encoding="utf-8")):
            found[f"{csproj.relative_to(root)}::{match.group(1)}"] = match.group(2)
    return found


def check(root=ROOT):
    expected = build_props_version(root)
    mismatches = []
    actual_manifest = manifest_version(root)
    if actual_manifest != expected:
        mismatches.append(f"eng/release_manifest.json version {actual_manifest!r} != Directory.Build.props {expected!r}")
    for key, version in sample_versions(root).items():
        if version != expected:
            mismatches.append(f"{key} references {version!r} != Directory.Build.props {expected!r}")
    return expected, mismatches


if __name__ == "__main__":
    version, mismatches = check()
    if mismatches:
        for mismatch in mismatches:
            print(f"Version drift: {mismatch}", file=sys.stderr)
        sys.exit(1)
    print(f"Version {version} agrees across Directory.Build.props, the release manifest and every sample.")
