import xml.etree.ElementTree as ET
import glob

def calculate_domain_coverage(report_pattern: str = "./coverage/**/coverage.cobertura.xml") -> float:
    files = glob.glob(report_pattern, recursive=True)

    # We must deduplicate lines because multiple test projects generate coverage for the same Domain classes,
    # causing total_lines to be artificially inflated if we just blindly sum them up.
    # We will use a set of (filename, line_number) to track unique lines.
    domain_lines = {} # (filename, line_number) -> hits

    for f in files:
        tree = ET.parse(f)
        root = tree.getroot()
        for package in root.findall(".//package"):
            name = package.get("name", "")
            if "Domain" in name:
                for cls in package.findall(".//class"):
                    filename = cls.get("filename", "")
                    for line in cls.findall(".//line"):
                        line_number = int(line.get("number", "0"))
                        hits = int(line.get("hits", "0"))

                        key = (filename, line_number)
                        if key not in domain_lines:
                            domain_lines[key] = 0

                        domain_lines[key] += hits

    total_lines = len(domain_lines)
    covered_lines = sum(1 for hits in domain_lines.values() if hits > 0)

    if total_lines == 0:
        return 0.0
    return (covered_lines / total_lines) * 100

print(f"Coverage: {calculate_domain_coverage()}")
