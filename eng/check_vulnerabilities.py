"""Fail closed on any known-vulnerable dependency; `dotnet list package --vulnerable` always exits 0 on its own
regardless of findings, so without this wrapper a known CVE in a transitive dependency would never fail CI.
Severity policy: any reported advisory blocks, at any severity - deliberately conservative until an owner
triages findings and records a documented exception; this script never auto-approves/suppresses anything.
"""
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def find_vulnerabilities(node, path=""):
    """Recursively finds every non-empty "vulnerabilities" array in a `dotnet list package --format json` report."""
    found = []
    if isinstance(node, dict):
        for key, value in node.items():
            if key == "vulnerabilities" and isinstance(value, list) and value:
                found.append((path, value))
            else:
                found.extend(find_vulnerabilities(value, f"{path}.{key}"))
    elif isinstance(node, list):
        for index, item in enumerate(node):
            found.extend(find_vulnerabilities(item, f"{path}[{index}]"))
    return found


def run(solution="Koras.Connect.slnx"):
    result = subprocess.run(
        ["dotnet", "list", solution, "package", "--vulnerable", "--include-transitive", "--format", "json"],
        capture_output=True, text=True, cwd=ROOT, check=True)
    return find_vulnerabilities(json.loads(result.stdout))


if __name__ == "__main__":
    findings = run()
    if findings:
        for path, vulnerabilities in findings:
            print(f"Vulnerable dependency at {path}: {vulnerabilities}", file=sys.stderr)
        print(f"{len(findings)} known-vulnerable dependency finding(s); failing per severity policy "
              "(any known advisory blocks until an owner records a triaged exception).", file=sys.stderr)
        sys.exit(1)
    print("No known-vulnerable dependencies found.")
