# Tasks: Role-Based Invoice Approvals

**Input**: Design documents from `specs/001-role-based-invoice-approvals.md` and `specs/001-role-based-invoice-approvals-plan.md`

**Prerequisites**: `specs/001-role-based-invoice-approvals-plan.md` and `specs/001-role-based-invoice-approvals.md`

## Phase 1: Setup and Validation Baseline

**Purpose**: Establish the approval policy design and testing baseline before implementation begins.

- [ ] T001 [P] Review the approved spec and implementation plan to confirm boundary values and policy wording in `specs/001-role-based-invoice-approvals.md`
- [ ] T002 [P] Confirm the repository structure and service boundaries for `backend/src/InvoiceApp.Domain` and `backend/src/InvoiceApp.Api`
- [ ] T003 [P] Define the expected domain model responsibilities for invoice approval, approval records, and rejection reasons
- [ ] T004 [P] Capture the exact boundary test matrix: Manager at 50,000, Manager above 50,000, Director at 500,000, Director above 500,000, and high-value >500,000 cases
- [ ] T005 [P] Prepare the initial xUnit test plan for Phase 1 approval rules and the Phase 2 dual-approval workflow
- [ ] T006 [P] Document the rejection contract and state-preservation expectations for all invalid approval attempts

## Phase 2: Phase 1 — Single-Approver Rules (Manager and Director)

**Goal**: Enforce the single-approver approval limits and prove them through domain-level xUnit tests before moving to dual approval.

### User Story 1 - Manager approval limit (Priority: P1)

**Independent Test**: Manager approval succeeds at 50,000 and fails above 50,000 with a reason and no state mutation.

- [ ] T101 [P] [US1] Add failing xUnit tests for Manager approval at 49,999 and 50,000 in `backend/tests/InvoiceApp.Tests/Domain/`
- [ ] T102 [P] [US1] Add failing xUnit tests for Manager rejection above 50,000 in `backend/tests/InvoiceApp.Tests/Domain/`
- [ ] T103 [US1] Implement domain validation for Manager approval limit in `backend/src/InvoiceApp.Domain/`
- [ ] T104 [US1] Ensure rejection responses are explicit and include a clear human-readable reason
- [ ] T105 [US1] Verify invalid Manager approvals do not change invoice state, approval status, or approval history
- [ ] T106 [US1] Run targeted xUnit tests for Manager approval scenarios and fix failing assertions

### User Story 2 - Director approval limit (Priority: P1)

**Independent Test**: Director approval succeeds up to 500,000 and fails above that in single-approver flow.

- [ ] T201 [P] [US2] Add failing xUnit tests for Director approval at 499,999 and 500,000 in `backend/tests/InvoiceApp.Tests/Domain/`
- [ ] T202 [P] [US2] Add failing xUnit tests for Director rejection above 500,000 in a single approval flow in `backend/tests/InvoiceApp.Tests/Domain/`
- [ ] T203 [US2] Implement domain validation for Director approval limit in `backend/src/InvoiceApp.Domain/`
- [ ] T204 [US2] Enforce clear rejection reasons when a Director attempts to approve an invoice above 500,000 without a second approver
- [ ] T205 [US2] Verify no state mutation occurs for rejected Director approvals
- [ ] T206 [US2] Run targeted xUnit tests for Director approval scenarios and fix failing assertions

### Phase 1 Completion Gate

- [ ] T207 [P] Confirm all Phase 1 boundary values are covered in the xUnit suite
- [ ] T208 [P] Confirm all rejected attempts return clear reasons without mutating invoice state
- [ ] T209 [P] Run `dotnet test backend/InvoiceApp.sln` for Phase 1 validation
- [ ] T210 [P] Execute AI review of Phase 1 implementation and record findings in `ai-logs/AI_INTERACTIONS.md`

## Phase 3: Phase 2 — High-Value Dual Approval Workflow

**Goal**: Implement the workflow for amounts above 500,000 with distinct approvers, Director second approver enforcement, duplicate rejection, and state-preservation guarantees.

### User Story 3 - High-value dual approval (Priority: P1)

**Independent Test**: An invoice above 500,000 requires two distinct approvers; the second approver must be a Director, and duplicate approvals are rejected.

- [ ] T301 [P] [US3] Add failing xUnit tests for invoice amounts above 500,000 requiring dual approval in `backend/tests/InvoiceApp.Tests/Domain/`
- [ ] T302 [P] [US3] Add failing xUnit tests for valid Manager-plus-Director approval sequences
- [ ] T303 [P] [US3] Add failing xUnit tests for invalid second approver not being a Director
- [ ] T304 [P] [US3] Add failing xUnit tests for same-user duplicate approvals on the same invoice
- [ ] T305 [US3] Implement domain workflow logic for high-value invoices in `backend/src/InvoiceApp.Domain/`
- [ ] T306 [US3] Enforce distinct approver identity checks and second-approver Director validation
- [ ] T307 [US3] Reject duplicate approval attempts with a clear reason and no state mutation
- [ ] T308 [US3] Ensure rejected high-value attempts do not append approval records or advance workflow state
- [ ] T309 [US3] Run targeted xUnit tests for dual-approval scenarios and fix failing assertions

### User Story 4 - State integrity and invalid workflow handling (Priority: P1)

**Independent Test**: Rejected attempts never modify invoice state even after earlier valid approvals or invalid approval sequencing.

- [ ] T401 [P] [US4] Add failing tests for state preservation after a failed approval following a valid first approval
- [ ] T402 [P] [US4] Add failing tests for invalid state transitions on draft, submitted, approved, and rejected invoice flows
- [ ] T403 [US4] Implement or adjust domain checks so rejected attempts are atomic and side-effect free
- [ ] T404 [US4] Validate exact data remains unchanged before and after rejected requests
- [ ] T405 [US4] Run targeted xUnit tests and confirm state preservation in all invalid flows

### Phase 2 Completion Gate

- [ ] T406 [P] Confirm all high-value dual approval rules are captured in test scenarios
- [ ] T407 [P] Confirm second approver must be Director and no duplicate approver is allowed
- [ ] T408 [P] Confirm all rejected attempts leave the invoice state unchanged
- [ ] T409 [P] Run `dotnet test backend/InvoiceApp.sln` after Phase 2 implementation
- [ ] T410 [P] Execute AI review of Phase 2 implementation and record findings in `ai-logs/AI_INTERACTIONS.md`

## Phase 4: API and Workflow Validation

**Purpose**: Ensure the API layer surfaces the same approval decisions and clear reasons as the domain layer without allowing bypasses.

- [ ] T501 [P] Add API-level tests for Manager approval threshold validation in `backend/tests/InvoiceApp.Tests/Api/`
- [ ] T502 [P] Add API-level tests for Director approval threshold validation in `backend/tests/InvoiceApp.Tests/Api/`
- [ ] T503 [P] Add API-level tests for high-value dual approval validation in `backend/tests/InvoiceApp.Tests/Api/`
- [ ] T504 [P] Add API-level tests ensuring rejected approvals return reason strings without state mutation
- [ ] T505 [P] Verify API error payloads remain clear, readable, and consistent with all rejection scenarios
- [ ] T506 [P] Run API-focused test suite and confirm policy validations are not bypassed at the HTTP layer

## Phase 5: UI Automation (Playwright)

**Purpose**: Verify the user-visible approval flows in the browser, including rejection feedback and state preservation.

- [ ] T601 [P] Add Playwright UI test for Manager approval at 50,000 in `e2e/`
- [ ] T602 [P] Add Playwright UI test for Manager rejection above 50,000 with reason shown to user
- [ ] T603 [P] Add Playwright UI test for Director approval at 500,000
- [ ] T604 [P] Add Playwright UI test for single-approver rejection above 500,000
- [ ] T605 [P] Add Playwright UI test for dual approval above 500,000 with distinct approvers
- [ ] T606 [P] Add Playwright UI test for invalid second approver role (non-Director) on high-value invoice
- [ ] T607 [P] Add Playwright UI test for duplicate approver rejection on the same invoice
- [ ] T608 [P] Add Playwright UI test confirming state remains unchanged after a rejected approval attempt
- [ ] T609 [P] Run Playwright test suite and fix browser-side assertion failures

## Phase 6: Mutation Testing and Quality Gates

**Purpose**: Verify the approval rules are resilient to mutation and that quality gates pass before completion.

- [ ] T701 [P] Add Stryker.NET configuration for the .NET solution and target relevant test projects
- [ ] T702 [P] Run initial mutation testing before code hardening to establish a baseline score
- [ ] T703 [P] Review mutation results and identify approval-rule mutants that survive
- [ ] T704 [P] Strengthen xUnit coverage for threshold comparisons, role validation, and duplicate detection
- [ ] T705 [P] Run mutation testing again after coverage improvements to measure the final score
- [ ] T706 [P] Record before-and-after Stryker.NET mutation scores in the review log and/or QA evidence
- [ ] T707 [P] Confirm mutation score is acceptable for the invoice approval feature and document any remaining exceptions

## Phase 7: Independent AI Review and Compliance Hooks

**Purpose**: Ensure each implementation phase is reviewed independently and that process evidence is retained.

- [ ] T801 [P] Run an independent AI review after Phase 1 implementation using `AGENTS.md` and the feature spec as review criteria
- [ ] T802 [P] Run an independent AI review after Phase 2 implementation using `AGENTS.md` and the feature spec as review criteria
- [ ] T803 [P] Record all AI review findings as Fixed, Dismissed, or Deferred with rationale
- [ ] T804 [P] Ensure the review checks spec compliance, state preservation, duplicate prevention, and role validation boundaries
- [ ] T805 [P] Validate that each review is independent of the original implementation agent
- [ ] T806 [P] Update `ai-logs/AI_INTERACTIONS.md` with prompts, decisions, code-generation steps, and review outcomes

## Phase 8: Repo Hooks and Final Validation

**Purpose**: Add automation and evidence-gathering hooks for repeated validation.

- [ ] T901 [P] Create or update any required git hooks or pre-commit validation steps for invoice approval rules
- [ ] T902 [P] Add task-level validation reminders for running xUnit tests before commit or merge
- [ ] T903 [P] Add task-level validation reminders for Playwright verification of high-value approval scenarios
- [ ] T904 [P] Add task-level validation reminders for Stryker.NET runs before final approval
- [ ] T905 [P] Ensure AI interaction logs are appended after every implementation and review checkpoint
- [ ] T906 [P] Perform final regression check across unit, API, UI, and mutation-test evidence

## Dependencies and Execution Order

- Phase 1 single-approver rules must be implemented and validated before Phase 2 dual-approver logic begins.
- API and UI tasks depend on domain validation being in place.
- Playwright tasks depend on UI and API behavior being complete enough to exercise the approval flows.
- Mutation testing depends on the final xUnit coverage being in place.
- AI reviews and interaction logging occur after each major implementation phase and before final sign-off.

## Completion Criteria

The feature is ready for implementation sign-off only when all of the following are true:

- Manager and Director approval limits are enforced at the exact thresholds specified in the spec.
- High-value invoices above 500,000 require two distinct approvers, with the second being a Director.
- Duplicate approver attempts are rejected and do not mutate invoice state.
- All rejected attempts return a clear business reason without modifying the invoice.
- xUnit test coverage validates the required manager/director and dual-approval scenarios.
- Playwright UI tests validate the user-visible approval flows.
- Stryker.NET before-and-after mutation scores are recorded.
- Independent AI reviews are recorded as Fixed, Dismissed, or Deferred with reasoning.
- `ai-logs/AI_INTERACTIONS.md` reflects the actual implementation and review history.
