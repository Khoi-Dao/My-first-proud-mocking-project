using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs.Transaction
{
    public class TransactionCreateRequestDto
    {
        [Required(ErrorMessage = "Mã hóa đơn là bắt buộc.")]
        public int InvoiceKhoiDnid { get; set; }

        [Required(ErrorMessage = "Số tiền thanh toán là bắt buộc.")]
        [Range(1000, 100000000, ErrorMessage = "Số tiền thanh toán phải từ 1,000 VND đến 100,000,000 VND.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
        [RegularExpression("^(CASH|VNMB|MOMO|BANK)$", ErrorMessage = "Phương thức thanh toán chỉ chấp nhận: CASH, VNMB, MOMO, BANK.")]
        public string PaymentMethod { get; set; } = null!;

        [Required(ErrorMessage = "Mã tham chiếu (Reference Code) không được để trống.")]
        public string ReferenceCode { get; set; } = null!;
    }
}
