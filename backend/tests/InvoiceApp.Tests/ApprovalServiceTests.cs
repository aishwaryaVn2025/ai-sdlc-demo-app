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
    public void Director_RejectsAboveLimit_500001_InSingleApprovalFlow()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-2", "dir-one", "Director"));

        Assert.Contains("500,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
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
}
