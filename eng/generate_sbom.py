"""Generate an SPDX 2.3 inventory for packed SDKs and their resolved runtime dependencies.
Not a certification or completeness claim for external vendor services or test tooling.
"""
import datetime
import hashlib
import json
from pathlib import Path
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = json.loads((ROOT / "eng/release_manifest.json").read_text())

def build():
    packages = {}
    relationships = []
    def identifier(name, version):
        return "SPDXRef-" + hashlib.sha256((name.lower() + "/" + version).encode()).hexdigest()[:24]
    def add(name, version, license_id="NOASSERTION", archive=None):
        key = identifier(name, version)
        if key not in packages:
            item = {"SPDXID": key, "name": name, "versionInfo": version, "downloadLocation": "NOASSERTION",
                    "filesAnalyzed": False, "licenseConcluded": "NOASSERTION", "licenseDeclared": license_id,
                    "copyrightText": "NOASSERTION", "externalRefs": [{"referenceCategory": "PACKAGE-MANAGER", "referenceType": "purl",
                    "referenceLocator": f"pkg:nuget/{name}@{version}"}]}
            if archive and archive.exists():
                item["checksums"] = [{"algorithm": "SHA256", "checksumValue": hashlib.sha256(archive.read_bytes()).hexdigest()}]
            packages[key] = item
        return key
    def relationship(source, kind, target):
        item = {"spdxElementId": source, "relationshipType": kind, "relatedSpdxElement": target}
        if item not in relationships: relationships.append(item)
    for name in MANIFEST["packages"]:
        project = ROOT / "src" / name
        archive = ROOT / "artifacts" / f"{name}.{MANIFEST['version']}.nupkg"
        if not archive.exists(): raise ValueError("Pack all release packages before generating the SBOM")
        own = add(name, MANIFEST["version"], archive=archive)
        relationship("SPDXRef-DOCUMENT", "DESCRIBES", own)
        assets = json.loads((project / "obj/project.assets.json").read_text())
        folders = [Path(p) for p in assets["packageFolders"]]
        resolved = {}
        for key, library in assets["libraries"].items():
            if library["type"] != "package": continue
            dependency, version = key.rsplit("/", 1)
            package_dir = next((p / library["path"] for p in folders if (p / library["path"]).exists()), None)
            license_id = "NOASSERTION"
            dep_archive = None
            if package_dir:
                nuspec = package_dir / (dependency.lower() + ".nuspec")
                if nuspec.exists():
                    for node in ET.parse(nuspec).getroot().iter():
                        if node.tag.split("}")[-1] == "license" and node.get("type") == "expression": license_id = node.text or license_id
                dep_archive = package_dir / (dependency.lower() + "." + version + ".nupkg")
            resolved[dependency.lower()] = add(dependency, version, license_id, dep_archive)
        # Include project-to-project and transitive resolved runtime dependencies, excluding test projects.
        for key, library in assets["libraries"].items():
            dependency, version = key.rsplit("/", 1)
            if library["type"] == "project" and dependency in MANIFEST["packages"]:
                relationship(own, "DEPENDS_ON", add(dependency, MANIFEST["version"]))
        for dep in resolved.values(): relationship(own, "DEPENDS_ON", dep)
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    return {"spdxVersion": "SPDX-2.3", "dataLicense": "CC0-1.0", "SPDXID": "SPDXRef-DOCUMENT",
            "name": "Koras Connect SDK runtime inventory", "documentNamespace": "https://spdx.org/spdxdocs/koras-connect-" + str(uuid.uuid4()),
            "creationInfo": {"created": stamp, "creators": ["Tool: Koras.Connect.Sbom-1"]},
            "packages": list(packages.values()), "relationships": relationships}

if __name__ == "__main__":
    sbom = build()
    path = ROOT / "artifacts/sbom.spdx.json"
    path.write_text(json.dumps(sbom, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote runtime SBOM: {len(sbom['packages'])} packages")
