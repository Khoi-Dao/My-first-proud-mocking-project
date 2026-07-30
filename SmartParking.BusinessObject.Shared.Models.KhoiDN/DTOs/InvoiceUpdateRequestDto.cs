using System.ComponentModel.DataAnnotations;

namespace SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs
{
    public class InvoiceUpdateRequestDto
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; } = null!;

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; } = null!;
    }
}