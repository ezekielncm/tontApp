import glob
import sys
import xml.etree.ElementTree as ET

def calculate_domain_coverage(report_pattern: str = "tests/**/coverage.cobertura.xml") -> float:
    files = glob.glob(report_pattern, recursive=True)

    domain_lines = {}

    for f in files:
        try:
            tree = ET.parse(f)
            root = tree.getroot()
            for package in root.findall(".//package"):
                name = package.get("name", "")
                if "Domain" in name:
                    for cls in package.findall(".//class"):
                        class_name = cls.get("name", "")
                        for line in cls.findall(".//line"):
                            line_num = int(line.get("number", "0"))
                            hits = int(line.get("hits", "0"))
                            key = (class_name, line_num)
                            if key not in domain_lines:
                                domain_lines[key] = 0
                            domain_lines[key] += hits
        except Exception as e:
            print(f"Error parsing {f}: {e}")

    total_lines = len(domain_lines)
    covered_lines = sum(1 for hits in domain_lines.values() if hits > 0)

    print(f"Total: {total_lines}, Covered: {covered_lines}")

    if total_lines == 0:
        return 0.0
    return (covered_lines / total_lines) * 100

print(calculate_domain_coverage())
