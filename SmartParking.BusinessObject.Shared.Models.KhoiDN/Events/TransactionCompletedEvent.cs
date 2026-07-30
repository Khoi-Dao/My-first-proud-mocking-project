using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartParking.BusinessObject.Shared.Models.KhoiDN.Events
{
    // Sự kiện bắn lên RabbitMQ khi Microservice 2 tạo giao dịch thành công
    public class TransactionCompletedEvent
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
