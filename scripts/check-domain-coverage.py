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


def calculate_domain_coverage(report_pattern: str = "./coverage/report/Cobertura.xml") -> float:
    files = glob.glob(report_pattern, recursive=True)
    total_lines = 0
    covered_lines = 0

    if not files:
        print(f"Warning: No coverage file found matching {report_pattern}")
        return 0.0

    # Parse only the first (merged) report
    f = files[0]
    try:
        tree = ET.parse(f)
        root = tree.getroot()
        for package in root.findall(".//package"):
            name = package.get("name", "")
            if "Domain" in name:
                for cls in package.findall(".//class"):
                    for line in cls.findall(".//line"):
                        total_lines += 1
                        if int(line.get("hits", "0")) > 0:
                            covered_lines += 1
    except Exception as e:
        print(f"Error parsing coverage report: {e}")
        return 0.0

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
