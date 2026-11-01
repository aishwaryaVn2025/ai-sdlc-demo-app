using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.approver");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver"));
    }

    [Fact]
    public void Manager_ApprovesAtLimit_50000()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "mgr-one", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("mgr-one", result.DecidedBy);
    }

    [Fact]
    public void Manager_RejectsAboveLimit_50001()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "mgr-one", "Manager"));

        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void Director_ApprovesAtLimit_500000()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 500_000m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-2", "dir-one", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("dir-one", result.DecidedBy);
    }

    [Fact]
    public void Director_FirstApproval_500001_LeavesInvoicePending_WithOneApprover()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-2", "dir-one", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Single(result.ApprovalHistory);
        Assert.Equal("dir-one", result.ApprovalHistory[0].Approver);
        Assert.Equal("Director", result.ApprovalHistory[0].Role);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Null(result.DecisionNote);

        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.ApprovalHistory);
        Assert.Equal("dir-one", invoice.ApprovalHistory[0].Approver);
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }

    [Fact]
    public void HighValueInvoice_RequiresTwoDistinctApprovers_SecondApproverMustBeDirector()
    {
        var invoice = InvoiceWith("INV-3", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var firstResult = service.Approve("INV-3", "mgr-one", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, firstResult.Status);
        Assert.Single(firstResult.ApprovalHistory);
        Assert.Equal("mgr-one", firstResult.ApprovalHistory[0].Approver);

        var second = service.Approve("INV-3", "dir-one", "Director");

        Assert.Equal(InvoiceStatus.Approved, second.Status);
        Assert.Equal(2, second.ApprovalHistory.Count);
        Assert.Equal("dir-one", second.ApprovalHistory[1].Approver);
    }

    [Fact]
    public void HighValueInvoice_FirstApprovalLeavesInvoicePendingUntilDirectorSecondApproval()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-2", "dir-one", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, firstApproval.Status);
        Assert.Single(firstApproval.ApprovalHistory);
        Assert.Equal("dir-one", firstApproval.ApprovalHistory[0].Approver);
        Assert.Null(firstApproval.DecidedBy);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-2", "mgr-one", "Manager"));

        Assert.Contains("Director", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.ApprovalHistory);
    }

    [Fact]
    public void HighValueInvoice_RequiresTwoDistinctApprovers_BeforeFinalApproval_WithDirectorSecondApprover()
    {
        var invoice = InvoiceWith("INV-6", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-6", "mgr-one", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, firstApproval.Status);
        Assert.Single(firstApproval.ApprovalHistory);
        Assert.Equal("mgr-one", firstApproval.ApprovalHistory[0].Approver);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-6", "mgr-two", "Manager"));

        Assert.Contains("Director", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.ApprovalHistory);
        Assert.Equal("mgr-one", invoice.ApprovalHistory[0].Approver);

        var finalApproval = service.Approve("INV-6", "dir-one", "Director");

        Assert.Equal(InvoiceStatus.Approved, finalApproval.Status);
        Assert.Equal(2, finalApproval.ApprovalHistory.Count);
        Assert.Equal("dir-one", finalApproval.ApprovalHistory[1].Approver);
        Assert.Equal("Director", finalApproval.ApprovalHistory[1].Role);
    }

    [Fact]
    public void HighValueInvoice_RejectsNonDirectorSecondApprover_WithoutChangingState()
    {
        var invoice = InvoiceWith("INV-4", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-4", "mgr-one", "Manager");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-4", "mgr-two", "Manager"));

        Assert.Contains("Director", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.ApprovalHistory);
        Assert.Equal("mgr-one", invoice.ApprovalHistory[0].Approver);
    }

    [Fact]
    public void HighValueInvoice_RejectsDuplicateApprover_WithoutChangingState()
    {
        var invoice = InvoiceWith("INV-5", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-5", "mgr-one", "Manager");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-5", "mgr-one", "Manager"));

        Assert.Contains("same user", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.ApprovalHistory);
        Assert.Equal("mgr-one", invoice.ApprovalHistory[0].Approver);
    }

    [Fact]
    public void Approve_RejectsBlankApproverIdentity()
    {
        var invoice = InvoiceWith("INV-7", InvoiceStatus.PendingApproval, 1_000m);
        var service = ServiceFor(invoice);

        Assert.Throws<ArgumentException>(() => service.Approve("INV-7", "   "));
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void HighValueInvoice_RejectsDuplicateApprover_UsingCaseInsensitiveComparison_WithoutChangingState()
    {
        var invoice = InvoiceWith("INV-8", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-8", "mgr-one", "Manager");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-8", "MGR-ONE", "Director"));

        Assert.Contains("same user", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Single(invoice.ApprovalHistory);
        Assert.Equal("mgr-one", invoice.ApprovalHistory[0].Approver);
        Assert.Equal("Manager", invoice.ApprovalHistory[0].Role);
    }

    [Fact]
    public void Reject_InvalidApprovalAttempt_PreservesInvoiceStateAndApprovalHistory()
    {
        var invoice = InvoiceWith("INV-9", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-9", "mgr-one", "Manager");
        var originalStatus = firstApproval.Status;
        var originalDecidedBy = firstApproval.DecidedBy;
        var originalDecidedAt = firstApproval.DecidedAt;
        var originalDecisionNote = firstApproval.DecisionNote;
        var originalApprovalRecords = firstApproval.ApprovalHistory.Select(r => new ApprovalRecord
        {
            Approver = r.Approver,
            Role = r.Role,
            ApprovedAt = r.ApprovedAt
        }).ToList();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-9", "MGR-ONE", "Director"));

        Assert.Contains("same user", ex.Message);
        Assert.Equal(originalStatus, invoice.Status);
        Assert.Equal(originalDecidedBy, invoice.DecidedBy);
        Assert.Equal(originalDecidedAt, invoice.DecidedAt);
        Assert.Equal(originalDecisionNote, invoice.DecisionNote);
        Assert.Equal(originalApprovalRecords.Count, invoice.ApprovalHistory.Count);
        for (var i = 0; i < originalApprovalRecords.Count; i++)
        {
            Assert.Equal(originalApprovalRecords[i].Approver, invoice.ApprovalHistory[i].Approver);
            Assert.Equal(originalApprovalRecords[i].Role, invoice.ApprovalHistory[i].Role);
            Assert.Equal(originalApprovalRecords[i].ApprovedAt, invoice.ApprovalHistory[i].ApprovedAt);
        }
    }

    [Fact]
    public void Reject_BlankApproverIdentity_DoesNotMutateInvoiceState()
    {
        var invoice = InvoiceWith("INV-10", InvoiceStatus.PendingApproval, 42_000m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ArgumentException>(() => service.Reject("INV-10", "   ", "Duplicate invoice"));

        Assert.Contains("Approver", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void Approve_TrimsApproverAndDirectorRoleWhitespace_At500000()
    {
        var invoice = InvoiceWith("INV-11", InvoiceStatus.PendingApproval, 500_000m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-11", "  dir-one  ", "  Director  ");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("dir-one", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Empty(result.ApprovalHistory);

        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal("dir-one", invoice.DecidedBy);
        Assert.NotNull(invoice.DecidedAt);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void Approve_AdminRole_Above50000_RejectsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-12", InvoiceStatus.PendingApproval, 50_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-12", "admin-one", "Admin"));

        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void Approve_ApproverRole_Above50000_RejectsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-13", InvoiceStatus.PendingApproval, 50_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-13", "approver-one", "Approver"));

        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void HighValueInvoice_FirstApproval_InvalidRole_Above500000_RejectsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-14", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-14", "mgr-one", "Admin"));

        Assert.Contains("Manager or Director", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.ApprovalHistory);
    }
}
