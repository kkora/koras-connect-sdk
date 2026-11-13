import hashlib
import importlib.util
from pathlib import Path
import tempfile
import json
import zipfile
from unittest.mock import patch
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('release_checks', ROOT / 'eng/release_checks.py')
checks = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checks)

class ReleaseChecksTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.path = Path(self.folder.name)
        self.archive = self.path / 'Example.1.2.3.nupkg'
        self.archive.write_bytes(b'synthetic-package')
        self.manifest = {'version': '1.2.3', 'packages': ['Example'], 'publicationApproved': False}
        self.sbom = {'spdxVersion': 'SPDX-2.3', 'packages': [{'name': 'Example', 'versionInfo': '1.2.3', 'checksums': [{'algorithm': 'SHA256', 'checksumValue': hashlib.sha256(self.archive.read_bytes()).hexdigest()}]}]}
    def tearDown(self):
        self.folder.cleanup()
    def test_validation_requires_no_publication_credentials(self):
        self.assertEqual(['Example'], checks.validate(self.manifest, self.sbom, self.path))
    def test_invalid_semver_is_rejected(self):
        for bad_version in ('1.2', '1.2.3.4', 'v1.2.3', '1.2.3-', 'latest'):
            self.manifest['version'] = bad_version
            with self.assertRaisesRegex(ValueError, 'Invalid release version'):
                checks.validate(self.manifest, self.sbom, self.path)
    def test_publish_fails_closed_without_owner_approval(self):
        with self.assertRaisesRegex(ValueError, 'not approved'):
            checks.validate(self.manifest, self.sbom, self.path, True, 'refs/tags/v1.2.3')
    def test_publish_rejects_branch_or_wrong_version(self):
        self.manifest['publicationApproved'] = True
        for ref in ('refs/heads/main', 'refs/tags/v1.2.4', None):
            with self.assertRaisesRegex(ValueError, 'release tag'):
                checks.validate(self.manifest, self.sbom, self.path, True, ref)
    def test_publish_requires_approval_record(self):
        self.manifest['publicationApproved'] = True
        with self.assertRaisesRegex(ValueError, 'approval record missing'):
            checks.validate(self.manifest, self.sbom, self.path, True, 'refs/tags/v1.2.3')
    def test_tampered_packages_fail(self):
        self.archive.write_bytes(b'tampered')
        with self.assertRaisesRegex(ValueError, 'checksum'):
            checks.validate(self.manifest, self.sbom, self.path)
    def test_missing_packages_fail(self):
        self.archive.unlink()
        with self.assertRaisesRegex(ValueError, 'missing'):
            checks.validate(self.manifest, self.sbom, self.path)
    def test_incomplete_sbom_fails(self):
        self.sbom['packages'] = []
        with self.assertRaisesRegex(ValueError, 'every SDK package'):
            checks.validate(self.manifest, self.sbom, self.path)
    def test_package_version_mismatch_fails(self):
        self.sbom['packages'][0]['versionInfo'] = '1.2.4'
        with self.assertRaisesRegex(ValueError, 'mismatched'):
            checks.validate(self.manifest, self.sbom, self.path)

    def publish_setup(self, license_expression='MIT'):
        with zipfile.ZipFile(self.archive, 'w') as archive:
            archive.writestr('Example.nuspec', f'<package><metadata><license type="expression">{license_expression}</license></metadata></package>')
        self.sbom['packages'][0]['checksums'][0]['checksumValue'] = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        self.manifest['publicationApproved'] = True
        (self.path / 'eng').mkdir()
        (self.path / 'eng/publication_approval.json').write_text(json.dumps({'version': '1.2.3', 'approved': True, 'licenseExpression': 'MIT'}))
    def test_reviewed_synthetic_release_can_pass_without_publishing(self):
        self.publish_setup()
        with patch.object(checks, 'ROOT', self.path):
            self.assertEqual(['Example'], checks.validate(self.manifest, self.sbom, self.path, True, 'refs/tags/v1.2.3'))
    def test_license_mismatch_fails(self):
        self.publish_setup('Apache-2.0')
        with patch.object(checks, 'ROOT', self.path):
            with self.assertRaisesRegex(ValueError, 'license does not match'):
                checks.validate(self.manifest, self.sbom, self.path, True, 'refs/tags/v1.2.3')
    def test_extra_archive_cannot_be_published(self):
        (self.path / 'Unexpected.1.2.3.nupkg').write_bytes(b'unreviewed')
        with self.assertRaisesRegex(ValueError, 'Unexpected'):
            checks.validate(self.manifest, self.sbom, self.path)

    def test_publish_rejects_a_manifest_order_that_precedes_a_dependency(self):
        # "Other" depends on "Example", but the manifest lists "Other" first - this must be rejected at publish
        # time so a consumer can never try to restore "Other" mid-release and find "Example" not yet live.
        other_archive = self.path / 'Other.1.2.3.nupkg'
        with zipfile.ZipFile(other_archive, 'w') as archive:
            archive.writestr('Other.nuspec', '<package><metadata><license type="expression">MIT</license>'
                              '<dependencies><group><dependency id="Example" version="1.2.3" /></group></dependencies>'
                              '</metadata></package>')
        with zipfile.ZipFile(self.archive, 'w') as archive:
            archive.writestr('Example.nuspec', '<package><metadata><license type="expression">MIT</license></metadata></package>')
        self.sbom['packages'][0]['checksums'][0]['checksumValue'] = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        self.sbom['packages'].append({'name': 'Other', 'versionInfo': '1.2.3',
            'checksums': [{'algorithm': 'SHA256', 'checksumValue': hashlib.sha256(other_archive.read_bytes()).hexdigest()}]})
        self.manifest['packages'] = ['Other', 'Example']  # wrong order: dependent listed before its dependency
        self.manifest['publicationApproved'] = True
        (self.path / 'eng').mkdir()
        (self.path / 'eng/publication_approval.json').write_text(json.dumps({'version': '1.2.3', 'approved': True, 'licenseExpression': 'MIT'}))
        with patch.object(checks, 'ROOT', self.path):
            with self.assertRaisesRegex(ValueError, 'dependency order'):
                checks.validate(self.manifest, self.sbom, self.path, True, 'refs/tags/v1.2.3')

if __name__ == '__main__':
    unittest.main()
