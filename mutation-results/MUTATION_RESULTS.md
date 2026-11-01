# Stryker.NET Mutation Testing Results

## Configuration

Date: 2026-11-01
Stryker.NET Version: 4.13.0
Target: InvoiceApp.Domain / ApprovalService.cs

Command:
dotnet stryker --project InvoiceApp.Domain.csproj --mutate "**/ApprovalService.cs" --reporter Progress --reporter Html

## Before Test Improvements

Unit Tests: 19
Mutants Created: 161
Mutants Tested: 91
Killed: 68
Survived: 23
No Coverage: 16
Timeouts: 0
Mutation Score: 63.55%

## After Test Improvements

Unit Tests: 24
Mutants Created: 161
Mutants Tested: 100
Killed: 76
Survived: 24
No Coverage: 7
Timeouts: 0
Mutation Score: 71.03%

## Improvement

Mutation score increased by 7.48 percentage points.

Five additional xUnit tests were added to cover:
- Blank approver identity during rejection
- Whitespace normalization for approver and Director role
- Admin approval above the permitted limit
- Approver role approval above the permitted limit
- Invalid first approver role on high-value invoices

Production approval logic remained unchanged between runs.

## Conclusion

The additional tests improved mutation detection and reduced the number of uncovered mutants from 16 to 7.

The remaining surviving and uncovered mutants require further investigation.
