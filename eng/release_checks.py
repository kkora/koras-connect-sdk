"""Fail-closed release eligibility; never publishes or reads credentials."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile
import xml.etree.ElementTree as ET
import sys

ROOT = Path(__file__).resolve().parent.parent

def validate(manifest, sbom, artifacts, publish=False, ref=None):
    version = manifest["version"]
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?", version): raise ValueError("Invalid release version")
    if sbom.get("spdxVersion") != "SPDX-2.3": raise ValueError("Invalid SPDX version")
    expected_archives = {f"{name}.{version}.nupkg" for name in manifest["packages"]}
    if {p.name for p in artifacts.glob("*.nupkg")} != expected_archives: raise ValueError("Unexpected or missing release archives")
    own = {p["name"]: p for p in sbom["packages"] if p["name"] in manifest["packages"]}
    if set(own) != set(manifest["packages"]): raise ValueError("SBOM does not describe every SDK package")
    for name, package in own.items():
        archive = artifacts / f"{name}.{version}.nupkg"
        if package["versionInfo"] != version or not archive.exists(): raise ValueError("Missing or mismatched package")
        digest = hashlib.sha256(archive.read_bytes()).hexdigest()
        if not any(c["algorithm"] == "SHA256" and c["checksumValue"] == digest for c in package.get("checksums", [])):
            raise ValueError("Package checksum mismatch")
    if publish:
        if not manifest.get("publicationApproved"): raise ValueError("Public publication is not approved in the reviewed manifest")
        if ref != "refs/tags/v" + version: raise ValueError("Publishing requires the matching immutable release tag")
        evidence = ROOT / "eng/publication_approval.json"
        if not evidence.exists(): raise ValueError("Owner publication approval record missing")
        approval = json.loads(evidence.read_text())
        if approval.get("version") != version or approval.get("approved") is not True or not approval.get("licenseExpression"):
            raise ValueError("Publication approval/license record incomplete")
        for name in own:
            with zipfile.ZipFile(artifacts / f"{name}.{version}.nupkg") as package:
                nuspec = next(n for n in package.namelist() if n.endswith(".nuspec"))
                metadata = ET.fromstring(package.read(nuspec))
                licenses = [e for e in metadata.iter() if e.tag.split("}")[-1] == "license"]
                if len(licenses) != 1 or licenses[0].get("type") != "expression" or licenses[0].text != approval["licenseExpression"]:
                    raise ValueError("Package license does not match the reviewed publication approval")
    return sorted(own)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--publish", action="store_true"); parser.add_argument("--ref")
    args = parser.parse_args()
    try:
        names = validate(json.loads((ROOT / "eng/release_manifest.json").read_text()), json.loads((ROOT / "artifacts/sbom.spdx.json").read_text()), ROOT / "artifacts", args.publish, args.ref)
        print("Publication eligible" if args.publish else "Release artifacts validated; no publication requested")
    except (ValueError, KeyError, OSError) as error:
        print(f"Release check failed: {error}", file=sys.stderr); sys.exit(1)
