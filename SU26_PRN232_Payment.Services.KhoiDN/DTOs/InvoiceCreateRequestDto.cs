using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN.DTOs
{
    public class InvoiceCreateRequestDto
    {
        [Required(ErrorMessage = "Mã phiên đỗ xe là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã phiên đỗ xe (ParkingSessionId) phải lớn hơn 0.")]
        public int ParkingSessionId { get; set; }

        [Required(ErrorMessage = "Mã người dùng không được để trống.")]
        public int UserId { get; set; }

        // Description có thể null, nhưng nếu có nhập thì tối đa 500 ký tự
        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        // BaseFee và PenaltyFee sẽ được tính toán dựa trên logic kinh doanh, nên không cần người dùng nhập vào

        //dùng khi nhân viên muốn hủy hóa đơn 
        public class InvoiceCancelRequestDto
        {
            [Required(ErrorMessage = "Cancelling is mandatory")]
            public int InvoiceId { get; set; }
            [MaxLength(200)]
            public string CancellationReason { get; set; }
        }
    }
}
