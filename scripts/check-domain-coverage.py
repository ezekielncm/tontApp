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

    # Track the maximum hits for each line in each class across all reports
    # Key: (class_name, line_number), Value: max_hits
    line_hits = {}

    for f in files:
        tree = ET.parse(f)
        root = tree.getroot()
        for package in root.findall(".//package"):
            name = package.get("name", "")
            if "Domain" in name:
                for cls in package.findall(".//class"):
                    cls_name = cls.get("name", "")
                    for line in cls.findall(".//line"):
                        line_num = line.get("number", "")
                        hits = int(line.get("hits", "0"))

                        key = (cls_name, line_num)
                        if key not in line_hits or hits > line_hits[key]:
                            line_hits[key] = hits

    total_lines = len(line_hits)
    covered_lines = sum(1 for hits in line_hits.values() if hits > 0)

    if total_lines == 0:
        return 0.0
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
