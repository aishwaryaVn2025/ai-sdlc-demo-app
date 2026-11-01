# AI in SDLC Assignment — Role-Based Invoice Approvals

## 1. Project Overview

This assignment extends the existing .NET 8 invoice approval application with role-based approval rules and a two-approver workflow for high-value invoices.

The implementation was developed using an AI-assisted, specification-driven software development process.

**Repository branch:** `feature/role-based-approval-v2`

## 2. Business Requirements

The implemented approval rules are:

- Manager can approve invoices up to 50,000.
- Director can approve invoices up to 500,000.
- Invoices above 500,000 require two different approvers.
- The second approver must be a Director.
- The same user cannot approve the same invoice twice.
- Invalid approval attempts return an error without changing invoice state.

## 3. AI-Assisted Development Workflow

### Agent Instructions

Created `AGENTS.md` to document business rules, architecture guidelines, coding expectations, and validation requirements.

### Specification-Driven Development

Used Spec Kit to prepare:

- Feature specification
- Implementation plan
- Task breakdown

The specifications are maintained under the `specs/` directory.

### Agent Roles

Created custom agent instructions for:

- Implementation
- Testing
- Review

These instructions are stored in `.github/agents/`.

AI-assisted development was performed through Copilot sessions. A separate Copilot CLI session was used for the independent code review.

The custom Review agent was not directly selected from the agent picker.

### AI Interaction Logging

AI interaction records are maintained in `ai-logs/AI_INTERACTIONS.md`.

A repository hook configuration and Node.js logging script were also prepared. The script passed manual verification, and Copilot detected the hook configuration, but automatic execution was not confirmed.

The hook work remains incomplete and is not claimed as a verified working integration.

## 4. Implementation

The feature was implemented in separate Git commits.

**Phase 1 — Single-Approver Rules**

Implemented Manager and Director approval limits, with validation and tests for permitted and rejected approvals.

**Phase 2 — High-Value Approval Workflow**

Implemented the requirement for two distinct approvers when the invoice amount exceeds 500,000.

The first approval leaves the invoice pending, and the second approval must be performed by a Director.

Duplicate approvers are rejected using a case-insensitive identity comparison.

Invalid approval attempts preserve the existing invoice state.

## 5. Testing Evidence

### Unit Testing

**Framework:** xUnit

**Final results:**

- Passed: 26
- Failed: 0
- Skipped: 0

The tests cover approval limits, high-value approval workflows, duplicate approvers, invalid approval attempts, state preservation, and boundary conditions.

### Playwright End-to-End Testing

A Playwright UI test verifies that an over-limit Manager approval attempt is rejected and the invoice remains pending.

The recorded Playwright test run passed.

### Stryker.NET Mutation Testing

**Version:** 4.13.0

| Metric | Before | After |
|---|---|---|
| Unit tests | 19 | 24 |
| Mutation score | 63.55% | 71.03% |
| Killed mutants | 68 | 76 |
| Uncovered mutants | 16 | 7 |

The mutation score improved by **7.48 percentage points**.

Two additional boundary tests were subsequently added, bringing the final unit test count to 26. Mutation testing was not rerun after these two tests.

Detailed evidence is recorded in `mutation-results/MUTATION_RESULTS.md`.

## 6. Independent AI Code Review

A separate Copilot CLI review session reviewed the implementation against the specification using `REVIEW_DIFF.txt`.

The AI reviewer recommended approval and did not identify required production-code fixes.

A manual verification afterward identified two missing boundary tests:

- Manager approval at 49,999
- Director approval at 499,999

Both tests were added and passed.

The remaining uncovered mutation-testing cases were deferred for future test hardening.

Detailed review results are recorded in `review-results/INDEPENDENT_REVIEW.md`.

## 11. GitHub Pull Request

Pull Request: https://github.com/aishwaryaVn2025/ai-sdlc-demo-app/pull/1

The feature branch contains the implementation, tests,
mutation results, and independent AI review documentation.

The pull request was created within the personal GitHub fork
because the original repository restricts pull request creation
to collaborators.