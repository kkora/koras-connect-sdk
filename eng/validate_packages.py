"""Validate packaged assets without loading or publishing user code."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET
import zipfile

root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "artifacts")
manifest = json.loads((pathlib.Path(__file__).parent / "release_manifest.json").read_text())
expected = set(manifest["packages"])
found = set()
for path in root.glob("*.nupkg"):
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        nuspec = next(n for n in names if n.endswith(".nuspec"))
        doc = ET.fromstring(archive.read(nuspec))
        metadata = next(e for e in doc if e.tag.endswith("metadata"))
        fields = {e.tag.split("}")[-1]: e.text for e in metadata}
        package = fields["id"]
        assert package in expected and package not in found, path
        found.add(package)
        assert fields["version"] == manifest["version"], path
        assert f"lib/net10.0/{package}.dll" in names, path
        assert f"lib/net10.0/{package}.xml" in names, path
        assert "README.md" in names, path
        assert not any(n.lower().endswith((".pfx", ".p12", ".key", ".cs", ".env")) for n in names), path
        print(f"Validated {path.name}: net10.0 assembly, XML documentation, README, no source/private keys")
assert found == expected, (found, expected)
