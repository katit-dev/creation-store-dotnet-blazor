using System;
using System.Collections.Generic;

namespace CreationStore.API.Models;

public partial class PaymentTransaction
{
    public int PaymentTransactionId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public string TransactionStatus { get; set; } = null!;

    public string VnpTxnRef { get; set; } = null!;

    public string? VnpTransactionNo { get; set; }

    public string? VnpResponseCode { get; set; }

    public string? VnpTransactionStatus { get; set; }

    public string? VnpBankCode { get; set; }

    public string? VnpPayDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? RawResponse { get; set; }

    public virtual Order Order { get; set; } = null!;
}
