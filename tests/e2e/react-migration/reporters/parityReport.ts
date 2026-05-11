import type { FullResult, Reporter, TestCase, TestResult } from "@playwright/test/reporter";

type SpecSummary = {
  name: string;
  passed: number;
  failed: number;
  skipped: number;
};

function getSpecName(file: string) {
  return file.split("/react-migration/").pop() ?? file;
}

class ParityReport implements Reporter {
  private readonly summaries = new Map<string, SpecSummary>();

  onTestEnd(test: TestCase, result: TestResult) {
    if (!test.location.file.includes("/react-migration/")) {
      return;
    }

    const key = getSpecName(test.location.file);
    const summary = this.summaries.get(key) ?? {
      name: key,
      passed: 0,
      failed: 0,
      skipped: 0,
    };

    if (result.status === "passed") {
      summary.passed += 1;
    }
    else if (result.status === "skipped") {
      summary.skipped += 1;
    }
    else {
      summary.failed += 1;
    }

    this.summaries.set(key, summary);
  }

  onEnd(result: FullResult) {
    if (this.summaries.size === 0) {
      return;
    }

    const rows = [...this.summaries.values()]
      .sort((left, right) => left.name.localeCompare(right.name))
      .map(summary => {
        const status = summary.failed > 0 ? "FAIL" : summary.passed > 0 ? "PASS" : "SKIP";
        return `| ${summary.name} | ${status} | ${summary.passed} | ${summary.failed} | ${summary.skipped} |`;
      });

    // eslint-disable-next-line no-console
    console.log([
      "",
      "## React Migration Parity Report",
      "| Spec | Status | Passed | Failed | Skipped |",
      "|------|--------|--------|--------|---------|",
      ...rows,
      "",
      `Overall result: ${result.status}`,
      "",
    ].join("\n"));
  }
}

export default ParityReport;
