import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('check_version_consistency', ROOT / 'eng/check_version_consistency.py')
check_version_consistency = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check_version_consistency)


class CheckVersionConsistencyTests(unittest.TestCase):
    def build_fixture(self, folder, manifest_version="1.2.3", sample_version="1.2.3"):
        root = Path(folder)
        (root / "Directory.Build.props").write_text(
            "<Project><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>")
        (root / "eng").mkdir()
        (root / "eng/release_manifest.json").write_text(f'{{"version": "{manifest_version}"}}')
        samples = root / "samples/Example"
        samples.mkdir(parents=True)
        (samples / "Example.csproj").write_text(
            f'<Project><ItemGroup><PackageReference Include="Example.Package" Version="{sample_version}"/></ItemGroup></Project>')
        return root

    def test_a_fully_consistent_tree_reports_no_drift(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.build_fixture(folder)
            version, mismatches = check_version_consistency.check(root)
        self.assertEqual("1.2.3", version)
        self.assertEqual([], mismatches)

    def test_a_stale_release_manifest_version_is_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.build_fixture(folder, manifest_version="1.2.2")
            _, mismatches = check_version_consistency.check(root)
        self.assertEqual(1, len(mismatches))
        self.assertIn("release_manifest.json", mismatches[0])

    def test_a_stale_sample_package_reference_version_is_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            root = self.build_fixture(folder, sample_version="1.2.2")
            _, mismatches = check_version_consistency.check(root)
        self.assertEqual(1, len(mismatches))
        self.assertIn("Example.csproj", mismatches[0])

    def test_the_real_repository_has_no_version_drift(self):
        version, mismatches = check_version_consistency.check()
        self.assertEqual([], mismatches)
        self.assertTrue(version)


if __name__ == '__main__':
    unittest.main()
