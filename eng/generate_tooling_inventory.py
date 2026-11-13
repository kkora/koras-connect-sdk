"""Inventories build/test-only tooling (xunit, Microsoft.NET.Test.Sdk, etc.) separately from the runtime SBOM in
generate_sbom.py, which deliberately only covers packed shipping packages. Never claims these tools ship to a
consumer; this is a development-time dependency list, not a vulnerability/license certification.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TEST_PROJECTS = ["tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj"]


def collect(test_projects=TEST_PROJECTS):
    tools = []
    for project in test_projects:
        path = Path(project)
        text = (path if path.is_absolute() else ROOT / path).read_text(encoding="utf-8")
        for match in re.finditer(r'<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"', text):
            tools.append({"project": project, "package": match.group(1), "version": match.group(2)})
    return tools


if __name__ == "__main__":
    inventory = {
        "scope": "build/test tooling only - never part of a shipped package; see eng/generate_sbom.py for the runtime SBOM",
        "tools": collect(),
    }
    path = ROOT / "artifacts/tooling-inventory.json"
    path.write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote tooling inventory: {len(inventory['tools'])} build/test package reference(s)")
