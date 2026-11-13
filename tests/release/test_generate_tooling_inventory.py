import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('generate_tooling_inventory', ROOT / 'eng/generate_tooling_inventory.py')
generate_tooling_inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generate_tooling_inventory)


class GenerateToolingInventoryTests(unittest.TestCase):
    def test_collects_every_package_reference_from_the_given_test_projects_only(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "Fake.Tests.csproj"
            path.write_text(
                '<Project><ItemGroup>'
                '<PackageReference Include="xunit" Version="2.9.3"/>'
                '<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/>'
                '</ItemGroup></Project>')
            absolute = str(path)
            tools = generate_tooling_inventory.collect([absolute])
        self.assertEqual(
            [{"project": absolute, "package": "xunit", "version": "2.9.3"},
             {"project": absolute, "package": "Microsoft.NET.Test.Sdk", "version": "17.14.1"}],
            tools)

    def test_real_test_project_inventory_never_includes_a_shipping_runtime_package(self):
        tools = generate_tooling_inventory.collect()
        shipping_packages = {"Koras.Connect.Abstractions", "Koras.Connect.Core", "Koras.Connect.Authentication",
                              "Koras.Connect.Export", "Koras.Connect.Adp", "Koras.Connect.Sap.SuccessFactors",
                              "Koras.Connect.Qualtrics", "Koras.Connect"}
        self.assertFalse(shipping_packages & {t["package"] for t in tools})
        self.assertTrue(tools)  # the real test project does reference some tooling


if __name__ == '__main__':
    unittest.main()
