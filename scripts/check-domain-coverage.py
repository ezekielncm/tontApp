#!/usr/bin/env python3
"""
Check domain code coverage from Cobertura XML reports.
Exits with code 1 if coverage is below the threshold.

Usage: python3 scripts/check-domain-coverage.py [threshold]
  threshold: minimum coverage percentage (default: 70)
"""

import glob
import sys
import xml.etree.ElementTree as ET


def calculate_domain_coverage(report_pattern: str = "./coverage/**/coverage.cobertura.xml") -> float:
    files = glob.glob(report_pattern, recursive=True)

    # We want to aggregate lines correctly by taking the maximum hits across all files
    # since coverlet might produce multiple xml files (one per test project) and a line might be hit
    # in one project but not another.

    lines_dict = {} # key: (class_name, line_number), value: max_hits

    for f in files:
        try:
            tree = ET.parse(f)
            root = tree.getroot()
            for package in root.findall(".//package"):
                name = package.get("name", "")
                if "Domain" in name:
                    for cls in package.findall(".//class"):
                        cls_name = cls.get("name")
                        for line in cls.findall(".//line"):
                            line_num = line.get("number")
                            hits = int(line.get("hits", "0"))

                            key = (cls_name, line_num)
                            if key not in lines_dict:
                                lines_dict[key] = hits
                            else:
                                lines_dict[key] = max(lines_dict[key], hits)
        except Exception as e:
            print(f"Error parsing {f}: {e}")

    total_lines = len(lines_dict)
    if total_lines == 0:
        return 0.0

    covered_lines = sum(1 for hits in lines_dict.values() if hits > 0)

    return (covered_lines / total_lines) * 100


def main() -> None:
    threshold = float(sys.argv[1]) if len(sys.argv) > 1 else 70.0
    coverage = calculate_domain_coverage()
    print(f"Domain coverage: {coverage:.1f}%")

    if coverage < threshold:
        print(f"FAIL: Domain coverage {coverage:.1f}% is below {threshold:.0f}% threshold")
        sys.exit(1)
    else:
        print(f"PASS: Domain coverage {coverage:.1f}% meets {threshold:.0f}% threshold")


if __name__ == "__main__":
    main()
