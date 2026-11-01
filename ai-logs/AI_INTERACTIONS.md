# AI Interaction Log

## Project
AI in SDLC — Role-Based Invoice Approval

## AI Tools
- GitHub Copilot
- GitHub Spec Kit
- .NET 8 SDK
- xUnit

## Interaction 001 — Project Setup

**Date:** 2026-11-01

**Task:** Configure the AI-assisted development environment.

**Activities:**
- Cloned the reference repository.
- Created the `feature/role-based-approval-v2` branch.
- Installed and configured GitHub Spec Kit.
- Created AGENTS.md to define development rules.
- Created Implementation, Testing, and Review agent definitions.
- Created the AI interaction logging template.

**Validation:** Original unit tests passed (7/7).

**Human Decision:** Accepted the initial project configuration.

**Related commits:** `cec63c7`, `06338c4`, `d2b7ce5`, `5175d23`.

---

## Interaction 002 — Feature Specification

**Date:** 2026-11-01

**AI Tool:** GitHub Copilot (MAI-Code-1.1-Flash)

**Task:** Generate the feature specification using Spec Kit.

**Prompt summary:** Requested a specification covering Manager approval limits of 50,000, Director limits of 500,000, dual approval above 500,000, duplicate approver prevention, rejection messages, and state preservation.

**AI Response:** Generated `specs/001-role-based-invoice-approvals.md`, including functional requirements, acceptance scenarios, boundary conditions, and test scenarios.

**Human Review:** Reviewed the generated specification against the assignment requirements.

**Commit:** `ea1c90d`.

---

## Interaction 003 — Technical Planning

**Date:** 2026-11-01

**AI Tool:** GitHub Copilot using Spec Kit

**Task:** Generate a technical implementation plan.

**Prompt summary:** Requested a phased implementation plan following the existing .NET 8 Domain and API architecture.

**AI Response:** Generated `specs/001-role-based-invoice-approvals-plan.md`.

**Plan Included:**
- Phase 1: Single-approver role limits.
- Phase 2: High-value dual approval.
- xUnit validation.
- Playwright end-to-end testing.
- Stryker.NET mutation testing.
- AI code review and logging.

**Human Decision:** Accepted the plan.

**Commit:** `b041e31`.

---

## Interaction 004 — Task Breakdown

**Date:** 2026-11-01

**AI Tool:** GitHub Copilot using Spec Kit

**Task:** Generate implementation tasks.

**Prompt summary:** Requested separate development tasks for both approval phases, unit testing, Playwright, mutation testing, AI reviews, and logging.

**AI Response:** Generated `specs/001-role-based-invoice-approvals-tasks.md`.

**Validation:** Copilot reported the task breakdown was created without modifying application code.

**Commit:** Not yet verified.

---

## Interaction 005 — Phase 1 Implementation

**Date:** 2026-11-01

**AI Tool:** GitHub Copilot

**Task:** Implement Manager and Director single-approver limits.

**Prompt summary:** Requested Phase 1 only, including role limits, boundary tests, clear rejection reasons, and no invoice mutation after rejection.

**Files Changed:**
- `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`

**AI Response:** Implemented approval validation and tests. Copilot was unable to execute the tests because its environment could not locate the .NET SDK.

**Human Validation:**
- Executed the tests manually using .NET SDK 8.0.427.
- Initial result: 10 tests passed, 1 failed.
- Identified a mismatch in the Director rejection message.
- Requested an AI-generated correction.
- Reran unit tests successfully: 11 passed, 0 failed, 0 skipped.
- `git diff --check` completed without errors.

**Human Decision:** Accepted Phase 1 after correcting the failing test and verifying the results.

**Commit:** `45259c2`.

---

## Remaining Activities

- Implement and test Phase 2 dual approval.
- Execute and document specialist agent reviews.
- Configure and demonstrate hooks.
- Execute Playwright end-to-end tests.
- Capture genuine Stryker.NET before-and-after mutation scores.
- Review the pull request and classify findings.
- Complete ASSIGNMENT.md and record the demonstration video.

## Evidence Guidelines

Record the full prompts and actual agent responses where available. Preserve test outputs, review findings, and commit references.

Do not invent AI interactions, agent execution, review findings, or test results.