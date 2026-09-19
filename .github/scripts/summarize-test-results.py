"""Print a Markdown summary of the TRX test results under a directory.

CI appends the output to the job summary (.github/workflows/ci.yml), so the run page shows the
pass/fail counts per test project and the names and messages of failed tests without anyone
downloading the TRX files. It only reports: whether the job fails is decided by `dotnet test`'s exit
code, not by this script.

Usage: python3 summarize-test-results.py <results-directory>
"""

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def main() -> int:
    # The summary contains emoji; don't depend on the console's default encoding (e.g. cp1252).
    sys.stdout.reconfigure(encoding="utf-8")
    results_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("TestResults")
    trx_files = sorted(results_dir.glob("**/*.trx"))

    print("## Test results\n")
    if not trx_files:
        print("No test results were produced (the build may have failed before tests ran).")
        return 0

    rows = []
    failures = []
    totals = {"passed": 0, "failed": 0, "skipped": 0}
    for trx in trx_files:
        root = ET.parse(trx).getroot()
        counters = root.find("t:ResultSummary/t:Counters", NS)
        passed = int(counters.get("passed", 0))
        failed = int(counters.get("failed", 0))
        skipped = int(counters.get("notExecuted", 0))
        totals["passed"] += passed
        totals["failed"] += failed
        totals["skipped"] += skipped

        # The test project's name is the storage path's file name, e.g. EntityDetails.Api.Tests.dll.
        definition = root.find("t:TestDefinitions/t:UnitTest", NS)
        project = Path(definition.get("storage", trx.stem)).stem if definition is not None else trx.stem
        status = "❌" if failed else "✅"
        rows.append(f"| {status} {project} | {passed} | {failed} | {skipped} |")

        for result in root.iterfind("t:Results/t:UnitTestResult", NS):
            if result.get("outcome") != "Failed":
                continue
            message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
            first_line = message.strip().splitlines()[0] if message.strip() else "(no message)"
            failures.append(f"- `{result.get('testName')}`: {first_line}")

    print("| Project | Passed | Failed | Skipped |")
    print("|---|---|---|---|")
    print("\n".join(rows))
    print(f"| **Total** | **{totals['passed']}** | **{totals['failed']}** | **{totals['skipped']}** |")

    if failures:
        print("\n### Failed tests\n")
        print("\n".join(failures))

    return 0


if __name__ == "__main__":
    sys.exit(main())
