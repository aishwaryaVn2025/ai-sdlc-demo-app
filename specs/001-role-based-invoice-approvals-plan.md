# Implementation Plan: Role-Based Invoice Approvals

**Branch**: `001-role-based-invoice-approvals` | **Date**: 2026-11-01 | **Spec**: `specs/001-role-based-invoice-approvals.md`

**Input**: Feature specification from `specs/001-role-based-invoice-approvals.md`

## Summary

Implement the approval-policy feature in the existing .NET 8 application by separating the work into two controlled phases. Phase 1 will enforce the single-approver limits for Manager and Director roles with unit and integration-style xUnit tests. Phase 2 will add the high-value multi-approver workflow for invoices above 500,000, ensuring two distinct approvers, Director as the final approver, duplicate rejection, and no mutation of invoice state on rejected attempts. The plan also includes UI automation and mutation testing, plus independent AI review and interaction logging as required by the repository’s SDLC expectations.

## Technical Context

**Language/Version**: .NET 8

**Primary Dependencies**: ASP.NET Core, xUnit, Playwright, Stryker.NET, existing in-memory app architecture

**Storage**: In-memory application state for the demo app; no database change required for this feature

**Testing**: xUnit for domain validation; Playwright for end-to-end UI flows; Stryker.NET for mutation testing

**Target Platform**: Local .NET 8 web application with API and browser UI

**Project Type**: Web application / backend service with simple UI

**Performance Goals**: No material performance requirement beyond normal in-memory request handling; the focus is correctness and policy enforcement

**Constraints**:
- Keep business rules in `InvoiceApp.Domain`
- Keep API concerns in `InvoiceApp.Api`
- Preserve existing user workflow and invoice state transitions unless the spec changes them
- Rejected attempts must return a reason without mutating state
- Feature must be implemented in phases to preserve clarity and reviewability

**Scale/Scope**: Small single-service invoice approval demo application; feature scope limited to approval policy validation and coverage

## Constitution Check

Gates for this feature are consistent with the repository conventions:
- Business logic belongs in the domain layer.
- HTTP and UI concerns stay outside the business rules.
- Tests are required at the domain and full workflow levels.
- Review and logging requirements are part of the SDLC process.

No constitutional violation is anticipated. The change remains within the stated architecture and scope.

## Project Structure

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── Entities/
│   │   ├── Services/
│   │   ├── Enums/
│   │   └── Validation/
│   └── InvoiceApp.Api/
│       ├── Controllers/
│       ├── Models/
│       ├── wwwroot/
│       └── Services/
└── tests/
    └── InvoiceApp.Tests/
        ├── Domain/
        ├── Api/
        ├── UI/
        └── Mutation/
```

**Structure Decision**: Use the current repository layout with domain validation logic in `InvoiceApp.Domain`, approval API behavior in `InvoiceApp.Api`, and test coverage in `backend/tests/InvoiceApp.Tests` split by domain, API, and UI concerns.

## Phase Breakdown

### Phase 0: Validation and Design Review

**Objective**: Confirm the spec is precise and traceable before implementation.

**Tasks**:
- Review the feature spec in `specs/001-role-based-invoice-approvals.md` against the current application flow and state machine.
- Confirm the exact approval thresholds and policy wording: Manager up to 50,000 inclusive; Director up to 500,000 inclusive; high-value invoices above 500,000 require two approvals; second approver must be Director.
- Identify the likely domain object responsible for approval validation and state changes (for example the invoice domain model or service).
- Document the expected rejection payload semantics: clear reason, no state mutation, no partial updates.
- Prepare the test matrix and the boundary conditions before writing code.

**Exit Criteria**:
- Team agrees on the approval policy and boundary values.
- Domain responsibilities are assigned clearly.
- Rejection contract is explicitly defined for API consumers and UI behavior.

### Phase 1: Single-Approver Policy Enforcement

**Objective**: Implement and verify the baseline approval rules before adding the high-value workflow.

**Planned Domain Work**:
- Add or update approval validation rules in `InvoiceApp.Domain` so that:
  - Manager approval is allowed for amounts up to 50,000 inclusive.
  - Director approval is allowed for amounts up to 500,000 inclusive.
  - Amounts above these limits are rejected with a reason.
- Ensure policy evaluation is centralized in domain logic rather than duplicated in the API layer.
- Keep invalid attempts side-effect free: no invoice status change, no approval record appended, and no workflow state mutation.

**Planned API Work**:
- Expose approval validation through the existing invoice approval endpoint(s) in `InvoiceApp.Api`.
- Map validation failures to clear API-level messages without altering invoice state.
- Ensure the HTTP layer does not bypass the domain validation rules.

**Planned Test Work (xUnit)**:
- Manager approval below 50,000 succeeds.
- Manager approval at 50,000 succeeds.
- Manager approval above 50,000 fails with a clear reason.
- Director approval below 500,000 succeeds.
- Director approval at 500,000 succeeds.
- Director approval above 500,000 fails in single-approver flow.
- Rejected attempt leaves invoice state unchanged.

**Explicit Boundaries to Verify**:
- 49,999 and 50,000 are valid for Manager.
- 50,001 is invalid for Manager.
- 499,999 and 500,000 are valid for Director.
- 500,001 is invalid in a single-approver flow.
- Reject reasons must be human-readable and specific to policy.

**Exit Criteria**:
- Phase 1 tests pass with no exceptions in policy evaluation.
- All single-role approval rules work in isolation.
- No invalid approval modifies invoice state.

### Phase 2: High-Value Dual Approval Workflow

**Objective**: Extend the domain and API to enforce the two-approval policy for amounts above 500,000.

**Planned Domain Work**:
- Add workflow logic for invoices above 500,000 requiring two approvals.
- Validate the first and second approver identities are distinct.
- Enforce that the second approver is a Director.
- Reject duplicate approver attempts even when the same user reuses the same role.
- Ensure that a rejected attempt does not append records or change workflow status.
- Clearly differentiate approval order: first approval, second approval, and final approval completion.

**Planned API Work**:
- Add or update the API contract to support staged approval submissions for high-value invoices.
- Return explicit reasons for:
  - second approver missing
  - second approver not a Director
  - same approver used twice
  - duplicate request on an already approved invoice
  - invalid state transition
- Keep rejected attempts atomic: no partial state change on validation failure.

**Planned Test Work (xUnit)**:
- Invoice at 500,001 requires two distinct approvers.
- Manager + Director approval sequence succeeds when rules are valid.
- Manager + Manager fails because the second approver is not a Director.
- Director + Director with same user fails because the same user cannot approve the same invoice twice.
- Director attempt as sole approver for >500,000 fails with a clear reason.
- High-value invoice rejected if the second approver is not a Director.
- Duplicate approval on same invoice fails with no state mutation.
- Rejected attempt does not change status, approver history, or approval count.

**Explicit Boundaries to Verify**:
- 500,000 remains valid for a single Director approval.
- 500,001 requires dual approval.
- The second approver cannot be a Manager.
- The same user cannot approve the same invoice twice.
- A rejected attempt after one valid approval does not alter previously accepted state.

**Exit Criteria**:
- High-value dual approval is enforceable and testable.
- Rejection reasons are consistent and specific.
- State remains unchanged for all failed approval attempts.

## Testing Strategy

### Unit / Domain Testing
- Put approval rules at the domain level to validate policy decisions independent from HTTP and UI concerns.
- Use xUnit tests for threshold validation, rejection messaging, and state preservation.
- Use parameterized tests where possible to cover the boundary values and common approval permutations.

### API Testing
- Validate HTTP responses for invalid approval attempts.
- Assert response payloads include clear reason strings and unchanged invoice details after rejection.
- Verify that rejected requests return the same persisted invoice values as before the request.

### UI Testing (Playwright)
- Add UI tests covering:
  - manager approval at 50,000
  - manager rejection above 50,000
  - director approval at 500,000
  - single-approver rejection above 500,000
  - two-step approval flow above 500,000
  - second approver being a Director
  - duplicate approver flow fails cleanly
  - invalid rejections keep the invoice visible in its prior state
- Keep UI scenarios aligned to the same policy messages used by the domain and API layers.

### Mutation Testing (Stryker.NET)
- Add Stryker.NET mutation analysis to the .NET solution after the approval rules are implemented.
- Focus mutation targets on:
  - threshold comparisons
  - role checks
  - distinct approver validation
  - second approver Director enforcement
  - duplicate-user checks
  - no-state-change guard conditions
- Ensure the test suite is strong enough to kill mutants that would allow policy bypasses.

## AI Review and Verification Plan

### Independent AI Code Review
- Run a separate AI review after each implementation phase.
- The review should explicitly verify:
  - compliance with the feature spec
  - correct boundary handling
  - duplicate-approver prevention
  - consistent rejection reason messaging
  - no side effects on rejected requests
  - architecture compliance (domain vs API boundaries)
- Record findings as Fixed, Dismissed, or Deferred with explicit rationale.

### Interaction Logging
- Log all AI-assisted reasoning, prompts, code-generation steps, and review findings in `ai-logs/AI_INTERACTIONS.md`.
- Capture:
  - feature decision rationale
  - validation/test additions
  - review findings and disposition
  - any follow-up corrections or clarifications
- Ensure logs reflect actual execution and evidence rather than inferred output.

## Operational Execution Plan

### Phase 1 Execution Order
1. Review the domain model and existing invoice approval flow.
2. Add approval validation rules for Manager and Director thresholds.
3. Write xUnit tests for the required boundary values and rejection scenarios.
4. Confirm rejected attempts return an explicit reason and preserve state.
5. Run `dotnet test backend/InvoiceApp.sln`.
6. Perform AI review of the Phase 1 implementation and logging.

### Phase 2 Execution Order
1. Add high-value dual-approval workflow logic in the domain.
2. Enforce distinct approver and Director-second requirements.
3. Add duplicate approver rejection tests and no-state-mutation assertions.
4. Extend API validations for staged approval failures and final approval success.
5. Create Playwright UI test coverage for the end-to-end approval journeys.
6. Run xUnit + Playwright tests.
7. Run Stryker.NET mutation testing.
8. Conduct independent AI code review and log the outcomes.

## Risks and Mitigations

**Risk**: Validation logic is placed in the API layer, weakening the domain boundary.
- Mitigation: Keep all policy decisions in `InvoiceApp.Domain` and keep API controllers thin.

**Risk**: Rejection paths accidentally mutate invoice state.
- Mitigation: Treat rejection as atomic validation failure; assert state equality before and after attempts.

**Risk**: Duplicates and role sequencing are not covered by tests.
- Mitigation: Add dedicated xUnit and Playwright tests for exact failure cases and boundary conditions.

**Risk**: Mutation testing finds weak approval validation coverage.
- Mitigation: Add targeted tests to kill mutants around threshold checks and second-approver requirements.

## Success Criteria

- Manager and Director approval rules are enforced correctly at the threshold boundaries.
- Invoices above 500,000 require two distinct approvers, and the second approver must be a Director.
- Duplicate approval attempts are rejected with a clear reason and no state change.
- Rejected attempts remain side-effect free.
- xUnit coverage confirms all required scenarios.
- Playwright UI tests verify the user-visible policy flows.
- Stryker.NET mutation testing shows the approval logic is resilient to common bypass mutations.
- AI review findings are documented and dispositioned as Fixed, Dismissed, or Deferred.
- Interaction logs are kept current in `ai-logs/AI_INTERACTIONS.md`.
