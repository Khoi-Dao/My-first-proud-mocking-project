using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN.DTOs.Transaction
{
    public class TransactionResponseDto
    {
        [JsonPropertyName("transactionCode")]
        public string TransactionCode { get; set; } = string.Empty;

        [JsonPropertyName("invoiceKhoiDnId")] // 2. Khóa cứng ánh xạ JSON
        public int InvoiceKhoiDnId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = string.Empty;

        [JsonPropertyName("transactionTime")]
        public DateTime TransactionTime { get; set; }

        [JsonPropertyName("referenceCode")]
        public string ReferenceCode { get; set; } = string.Empty;
    }
}
