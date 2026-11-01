# Feature Specification: Role-Based Invoice Approvals

**Feature Branch**: `001-role-based-invoice-approvals`

**Created**: 2026-11-01

**Status**: Draft

**Input**: User description: "Create a specification for role-based invoice approvals in the existing .NET 8 application. A Manager can approve invoices up to 50,000 and a Director up to 500,000. Invoices above 500,000 require two different approvers, with the second being a Director. The same user cannot approve the same invoice twice. Rejected attempts must return a clear reason without modifying invoice state. Include acceptance criteria, boundary cases, and test scenarios. Do not implement code yet."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manager approval within limit (Priority: P1)

A manager needs to approve routine invoices within the maximum amount they are authorized to authorize, while receiving immediate feedback if the request is outside policy.

**Why this priority**: This is the baseline approval path and the minimum viable policy control for everyday invoice processing.

**Independent Test**: A manager attempts to approve a 49,999 invoice and a 50,000 invoice; both flows should succeed, and the system should preserve invoice status and approver records correctly.

**Acceptance Scenarios**:

1. **Given** an invoice amount of 49,999 and a user with the Manager role, **When** the user approves the invoice, **Then** the approval is accepted and the invoice state reflects a valid approval.
2. **Given** an invoice amount of 50,000 and a user with the Manager role, **When** the user approves the invoice, **Then** the approval is accepted because the amount is at the manager limit.
3. **Given** an invoice amount of 50,001 and a user with the Manager role, **When** the user approves the invoice, **Then** the approval is rejected with a clear reason stating the role limit is exceeded and the invoice state remains unchanged.

---

### User Story 2 - Director approval within limit and rejection above limit (Priority: P1)

A director needs to approve larger invoices within their permitted ceiling, but cannot single-handedly approve amounts beyond the defined director threshold.

**Why this priority**: Directors are the senior validation role and must be able to process medium-value invoices without allowing a rule bypass.

**Independent Test**: A director approves 499,999 and 500,000 successfully, and a director attempting 500,001 in a single-approval flow is rejected with a reason.

**Acceptance Scenarios**:

1. **Given** an invoice amount of 499,999 and a user with the Director role, **When** the user approves the invoice, **Then** the approval is accepted.
2. **Given** an invoice amount of 500,000 and a user with the Director role, **When** the user approves the invoice, **Then** the approval is accepted because the amount is at the director limit.
3. **Given** an invoice amount of 500,001 and a user with the Director role in a single-approval workflow, **When** the user approves the invoice, **Then** the approval is rejected with a clear reason indicating the invoice requires a second approver and the invoice state remains unchanged.

---

### User Story 3 - High-value invoice requires dual approval with a director second signature (Priority: P1)

Any invoice above 500,000 must be reviewed by two distinct approvers, with the final approval by a director to satisfy governance requirements.

**Why this priority**: This is the highest-risk approval path and the primary control for large financial exposure.

**Independent Test**: A manager or director can act as the first approver, but the second approver must be a different user and must be a Director; duplicates or invalid sequencing are rejected.

**Acceptance Scenarios**:

1. **Given** an invoice amount of 500,001 and two distinct approvers, **When** the first approver is a Manager and the second approver is a Director, **Then** the invoice is approved only when both approvals are valid and different users.
2. **Given** an invoice amount of 500,001 and the second approver is not a Director, **When** the approval is submitted, **Then** the attempt is rejected with a clear reason and no invoice state changes occur.
3. **Given** an invoice amount of 500,001 and the same user attempts to approve it twice, **When** the second approval is submitted, **Then** the system rejects the duplicate with a clear reason and keeps the invoice state unchanged.

---

### Edge Cases

- What happens when the invoice amount is exactly 50,000, 500,000, or 500,001?
- What happens when the same person tries to approve the same invoice multiple times in a single approval flow or in the multi-approver flow?
- What happens when a Manager attempts to approve a 500,001 invoice without a Director second approver?
- What happens when a non-Director user attempts to be the second approver for a >500,000 invoice?
- What happens when a rejected attempt occurs after one valid approval has already been recorded?
- What happens when approval is attempted on a draft, submitted, approved, or rejected invoice in an invalid workflow state?
- What happens when the request contains ambiguous or missing approver role information?
- What happens when the system receives decimal or currency formatting edge values around the policy thresholds?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST enforce role-based approval limits: a Manager may approve invoices up to and including 50,000, and a Director may approve invoices up to and including 500,000.
- **FR-002**: The system MUST reject any approval attempt by a Manager for an amount above 50,000 with a clear reason that identifies the limit and policy violation.
- **FR-003**: The system MUST reject any single-approver attempt by a Director for an amount above 500,000 with a clear reason that the invoice requires additional approval.
- **FR-004**: The system MUST require two distinct approvers for invoices above 500,000.
- **FR-005**: The system MUST require the second approver for high-value invoices to be a Director.
- **FR-006**: The system MUST reject duplicate approval attempts when the same user attempts to approve the same invoice more than once.
- **FR-007**: The system MUST reject an approval attempt if the same approver has already approved the invoice in the same approval flow.
- **FR-008**: The system MUST return a clear rejection reason for any invalid approval attempt and MUST NOT modify invoice state, approver history, or approval status when the attempt is rejected.
- **FR-009**: The system MUST preserve all existing invoice state until a valid approval passes the required rules.
- **FR-010**: The system MUST allow the approval flow to proceed in stages for high-value invoices, but only when each approval is by a different user and the final approver satisfies the Director requirement.
- **FR-011**: The system MUST clearly distinguish between the first approval and second approval for invoices above 500,000, so validation order can be enforced.
- **FR-012**: The system MUST treat the boundary values 50,000 and 500,000 as valid approvals for the corresponding roles, and 50,001 and 500,001 as invalid for the single-approver rules.
- **FR-013**: The system MUST support explicit validation messaging for approval policy failures, including duplicate approver, unauthorized role, amount threshold exceeded, and missing second approver scenarios.

### Key Entities *(include if feature involves data)*

- **Invoice**: The invoice being reviewed; it has an amount, workflow state, and a set of approval records.
- **Approver**: A person or user identity associated with a role, such as Manager or Director.
- **ApprovalAttempt**: A single attempt to approve an invoice, including the requesting approver, amount context, role, and outcome.
- **ApprovalRecord**: The persisted record of a valid approval event on an invoice, including approver identity, role, and approval order.
- **ApprovalWorkflow**: The policy-driven decision process that determines whether an invoice is valid for single approval or dual approval.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All policy rules are enforced at the exact thresholds: Manager approvals at 50,000 succeed and 50,001 fail; Director approvals at 500,000 succeed and 500,001 fail in single-approval flow.
- **SC-002**: Invoices above 500,000 require exactly two distinct approvers, and the second approver is always a Director.
- **SC-003**: Duplicate approvals are rejected with a clear reason and no state changes occur.
- **SC-004**: Every rejected approval attempt returns a meaningful policy error message and leaves the invoice in the same status and approval state as before the attempted action.
- **SC-005**: The acceptance test suite covers the required approval paths and boundary conditions for manager, director, and dual-approval scenarios.

## Assumptions

- The application already has user identity and roles available to the domain layer; this specification assumes those values are available during approval validation.
- Approval rules are enforced in the domain/business logic rather than by presentation-layer checks alone.
- Invoice amounts are treated as numeric currency values and compared as exact thresholds without hidden rounding beyond the app’s existing domain conventions.
- The system is expected to preserve state on rejected attempts and not mutate invoice approval history unless a valid approval passes policy checks.
- This feature is limited to role-based approval validation; authentication, authorization, and UI layout are out of scope unless required by the existing application architecture.
