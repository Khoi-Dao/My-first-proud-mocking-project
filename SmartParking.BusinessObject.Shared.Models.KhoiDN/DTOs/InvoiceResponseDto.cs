using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization; // Thêm namespace này để dùng JsonPropertyName

namespace SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs
{
    // Dùng cho API Get All (Danh sách gọn nhẹ)
    public class InvoiceResponseDto
    {
        // ⚠️ KHÓA CỨNG ÁNH XẠ JSON: Đảm bảo Blazor và Swagger luôn đọc chuẩn số ID, không bao giờ bị lệch thành số 0
        [JsonPropertyName("invoiceKhoiDNId")]
        public int InvoiceKhoiDNId { get; set; }

        public string InvoiceCode { get; set; } = string.Empty;
        public int? ParkingSessionId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // Dùng cho API Get By Id / Get By Code (Bao gồm cả lịch sử thanh toán)
    public class InvoiceDetailResponseDto : InvoiceResponseDto
    {
        // Hiển thị chi tiết tiền để Front-end vẽ hóa đơn (Receipt)
        public decimal BaseFee { get; set; }
        public decimal PenaltyFee { get; set; }

        // Danh sách các lần quẹt thẻ/chuyển khoản của hóa đơn này
        public List<TransactionBasicDto> Transactions { get; set; } = new List<TransactionBasicDto>();
    }

    // DTO phụ trợ để hiển thị giao dịch bên trong Hóa đơn (tránh vòng lặp vô hạn)
    public class TransactionBasicDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime TransactionTime { get; set; }
    }
}