namespace InvoiceApp.Domain;

using System.Globalization;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time, decided as a whole by a single approver.
/// </summary>
public class ApprovalService
{
    private const decimal ManagerApprovalLimit = 50_000m;
    private const decimal DirectorApprovalLimit = 500_000m;

    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, string? role = null)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            throw new ArgumentException("Approver is required.", nameof(approver));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        ValidateSingleApproverLimit(invoice.Amount, role);

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private static void ValidateSingleApproverLimit(decimal amount, string? role)
    {
        var normalizedRole = NormalizeRole(role);

        if (normalizedRole == "Manager")
        {
            if (amount > ManagerApprovalLimit)
            {
                throw new InvalidOperationException(
                    $"Manager approval limit exceeded: invoice amount {FormatCurrency(amount)} exceeds the maximum approved amount of {FormatCurrency(ManagerApprovalLimit)}.");
            }

            return;
        }

        if (normalizedRole == "Director")
        {
            if (amount > DirectorApprovalLimit)
            {
                throw new InvalidOperationException(
                    $"Director single-approval limit exceeded: invoice amount {FormatCurrency(amount)} exceeds the maximum approved amount of {FormatCurrency(DirectorApprovalLimit)}. A second approver is required.");
            }

            return;
        }

        if (normalizedRole == "Approver" || normalizedRole == "Admin")
        {
            if (amount > ManagerApprovalLimit)
            {
                throw new InvalidOperationException(
                    $"Approval limit exceeded: invoice amount {FormatCurrency(amount)} exceeds the maximum approved amount of {FormatCurrency(ManagerApprovalLimit)} for this role.");
            }

            return;
        }

        if (amount > ManagerApprovalLimit)
        {
            throw new InvalidOperationException(
                $"Approval limit exceeded: invoice amount {FormatCurrency(amount)} exceeds the maximum approved amount of {FormatCurrency(ManagerApprovalLimit)}.");
        }
    }

    private static string NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim();

    private static string FormatCurrency(decimal value) =>
        value.ToString("C", CultureInfo.InvariantCulture);
}
