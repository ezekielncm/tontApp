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

    if not files:
        # Fallback to scanning individual files if the merged report isn't found
        files = glob.glob("./coverage/**/coverage.cobertura.xml", recursive=True)

    max_coverage = 0.0

    for f in files:
        tree = ET.parse(f)
        root = tree.getroot()
        for package in root.findall(".//package"):
            name = package.get("name", "")
            if name == "Domain":
                rate_str = package.get("line-rate", "0")
                try:
                    rate = float(rate_str)
                    max_coverage = max(max_coverage, rate * 100)
                except ValueError:
                    pass

    return max_coverage


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
