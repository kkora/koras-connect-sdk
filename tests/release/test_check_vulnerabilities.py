import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('check_vulnerabilities', ROOT / 'eng/check_vulnerabilities.py')
check_vulnerabilities = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check_vulnerabilities)


class CheckVulnerabilitiesTests(unittest.TestCase):
    def test_a_clean_report_with_no_vulnerabilities_key_anywhere_finds_nothing(self):
        clean_report = {"version": 1, "projects": [{"path": "Example.csproj"}]}
        self.assertEqual([], check_vulnerabilities.find_vulnerabilities(clean_report))

    def test_an_empty_vulnerabilities_array_is_not_a_finding(self):
        # dotnet list package emits "vulnerabilities": [] for packages it checked but found nothing for; this must
        # not be treated as a finding, or every clean run would fail closed.
        report = {"projects": [{"frameworks": [{"topLevelPackages": [
            {"id": "Clean.Package", "vulnerabilities": []}
        ]}]}]}
        self.assertEqual([], check_vulnerabilities.find_vulnerabilities(report))

    def test_a_populated_vulnerabilities_array_at_any_nesting_depth_is_found(self):
        report = {"projects": [{"frameworks": [{"transitivePackages": [
            {"id": "Vulnerable.Package", "resolvedVersion": "1.0.0",
             "vulnerabilities": [{"severity": "High", "advisoryurl": "https://example.test/advisory/1"}]}
        ]}]}]}
        findings = check_vulnerabilities.find_vulnerabilities(report)
        self.assertEqual(1, len(findings))
        path, vulnerabilities = findings[0]
        self.assertEqual([{"severity": "High", "advisoryurl": "https://example.test/advisory/1"}], vulnerabilities)

    def test_multiple_findings_across_separate_projects_are_all_reported_not_just_the_first(self):
        report = {"projects": [
            {"frameworks": [{"topLevelPackages": [{"id": "A", "vulnerabilities": [{"severity": "Low"}]}]}]},
            {"frameworks": [{"topLevelPackages": [{"id": "B", "vulnerabilities": [{"severity": "Critical"}]}]}]},
        ]}
        self.assertEqual(2, len(check_vulnerabilities.find_vulnerabilities(report)))


if __name__ == '__main__':
    unittest.main()
