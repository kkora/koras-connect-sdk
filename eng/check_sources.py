"""Repository hygiene checks; operates on tracked candidate source, excluding outputs."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
issues = []
for path in root.rglob("*"):
    if not path.is_file() or any(p in {"bin", "obj", "artifacts", ".git", "TestResults"} for p in path.relative_to(root).parts):
        continue
    if path.suffix.lower() in {".pfx", ".p12", ".key"}:
        issues.append(str(path.relative_to(root)))
    if path.suffix in {".cs", ".csproj", ".yml", ".md", ".props", ".json"}:
        text = path.read_text(encoding="utf-8")
        if re.search(r"-----BEGIN (?:RSA |EC |ENCRYPTED )?PRIVATE KEY-----", text):
            issues.append(str(path.relative_to(root)))
        if path.suffix == ".csproj" and re.search(r"ProjectReference[^>]+(?:cli|cloud|AdpIntegration)", text, re.I):
            issues.append(str(path.relative_to(root)))
assert not issues, issues
print("Source hygiene passed: no private key files/PEM material or forbidden package-boundary references")
