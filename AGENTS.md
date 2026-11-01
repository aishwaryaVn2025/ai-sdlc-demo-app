# AI Agent Instructions

## Project
.NET 8 invoice approval application using Domain, API and test projects.

## Business Rules
- Manager: may approve invoices up to 50,000.
- Director: may approve invoices up to 500,000.
- Invoices above 500,000 require two distinct approvers.
- The second approver must be a Director.
- The same person cannot approve the same invoice twice.
- Rejected attempts must return a clear reason.
- Rejected attempts must not modify any invoice state.

## Architecture
- Business logic belongs in InvoiceApp.Domain.
- HTTP concerns belong in InvoiceApp.Api.
- Preserve existing behavior unless the specification requires a change.
- Keep changes small and testable.

## Testing
Run: dotnet test backend/InvoiceApp.sln

Required tests:
- Manager approval below and at 50,000
- Manager rejection above 50,000
- Director approval below and at 500,000
- Director rejection above 500,000 in single-approval flow
- Two-approver workflow above 500,000
- Second approver must be Director
- Duplicate approver rejected
- Rejected attempt leaves state unchanged

Also create a Playwright UI test and perform Stryker.NET mutation testing.

## AI Development Process
- Read the specification before changing code.
- Implement single-approver rules first.
- Implement the two-approver workflow in a separate commit.
- Use specialist agents for implementation, testing, and review.
- Log actual AI interactions, decisions and generated outputs.
- Do not invent execution results or review evidence.

## Review Expectations
- Identify functional bugs and missing edge cases.
- Verify boundary values and state preservation.
- Review generated tests for meaningful assertions.
- Report each finding as Fixed, Dismissed, or Deferred, with a reason.
- Run tests before recommending changes for merging.