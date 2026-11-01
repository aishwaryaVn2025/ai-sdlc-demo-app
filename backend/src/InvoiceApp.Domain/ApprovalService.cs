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
    private const decimal HighValueApprovalThreshold = 500_000m;

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
        var normalizedApprover = NormalizeApprover(approver);
        if (string.IsNullOrWhiteSpace(normalizedApprover))
        {
            throw new ArgumentException("Approver is required.", nameof(approver));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        var normalizedRole = NormalizeRole(role);

        if (invoice.Amount > HighValueApprovalThreshold)
        {
            ApplyHighValueApproval(invoice, normalizedApprover, normalizedRole);
            _repository.Update(invoice);
            return invoice;
        }

        ValidateSingleApproverLimit(invoice.Amount, normalizedRole);

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = normalizedApprover;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = null;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var normalizedApprover = NormalizeApprover(approver);
        if (string.IsNullOrWhiteSpace(normalizedApprover))
        {
            throw new ArgumentException("Approver is required.", nameof(approver));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = normalizedApprover;
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

    private static void ApplyHighValueApproval(Invoice invoice, string approver, string role)
    {
        if (invoice.ApprovalHistory.Count == 0)
        {
            if (role != "Manager" && role != "Director")
            {
                throw new InvalidOperationException(
                    $"Invoice amount {FormatCurrency(invoice.Amount)} requires two distinct approvers; the first approver must be a Manager or Director.");
            }

            invoice.ApprovalHistory.Add(new ApprovalRecord
            {
                Approver = approver,
                Role = role,
                ApprovedAt = DateTime.UtcNow
            });

            invoice.Status = InvoiceStatus.PendingApproval;
            invoice.DecidedBy = null;
            invoice.DecidedAt = null;
            invoice.DecisionNote = null;
            return;
        }

        if (invoice.ApprovalHistory.Count > 1)
        {
            throw new InvalidOperationException("Invoice is already fully approved.");
        }

        if (invoice.ApprovalHistory.Any(r => string.Equals(r.Approver, approver, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Duplicate approver detected: the same user cannot approve the same invoice twice.");
        }

        if (role != "Director")
        {
            throw new InvalidOperationException(
                $"Invoice amount {FormatCurrency(invoice.Amount)} requires the second approver to be a Director.");
        }

        invoice.ApprovalHistory.Add(new ApprovalRecord
        {
            Approver = approver,
            Role = role,
            ApprovedAt = DateTime.UtcNow
        });

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = null;
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

    private static string NormalizeApprover(string? approver) =>
        string.IsNullOrWhiteSpace(approver) ? string.Empty : approver.Trim();

    private static string FormatCurrency(decimal value) =>
        value.ToString("C", CultureInfo.InvariantCulture);
}
