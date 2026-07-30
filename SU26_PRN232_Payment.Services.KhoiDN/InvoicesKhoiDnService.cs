using Microsoft.Extensions.Logging;
using SU26_PRN232_Payment.Repositories.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN.DTOs;
using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public class InvoicesKhoiDnService : IInvoicesKhoiDnService
    {
        private readonly IInvoicesKhoiDnRepository _invoicesKhoiDnRepository;
        private readonly ILogger<InvoicesKhoiDnService> _logger;

        public InvoicesKhoiDnService(
            IInvoicesKhoiDnRepository repository,
            ILogger<InvoicesKhoiDnService> logger)
        {
            _invoicesKhoiDnRepository = repository;
            _logger = logger;
        }

        // =========================================================================
        // 1. HÀM CRUD CƠ BẢN (GIỮ NGUYÊN 100% ĐỂ DEMO THẦY GIÁO)
        // =========================================================================
        public async Task<InvoiceResponseDto> CreateInvoiceAsync(InvoiceCreateRequestDto request)
        {
            _logger.LogInformation("[CRUD DEMO] Bắt đầu tạo hóa đơn cho phiên đỗ xe ID: {SessionId}", request.ParkingSessionId);

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            if (!isValid)
            {
                var firstError = validationResults.First().ErrorMessage ?? "Dữ liệu không hợp lệ.";
                _logger.LogWarning("Validation Failed: {Error}", firstError);
                throw new ArgumentException(firstError);
            }

            var newInvoice = new InvoicesKhoiDn
            {
                ParkingSessionId = request.ParkingSessionId,
                UserId = request.UserId,
                Description = request.Description,
                InvoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                Status = "UNPAID",
                CreatedAt = DateTime.UtcNow,
                BaseFee = 10000,
                PenaltyFee = 0,
                TotalAmount = 10000
            };

            await _invoicesKhoiDnRepository.CreateAsync(newInvoice);
            await _invoicesKhoiDnRepository.SaveAsync();

            _logger.LogInformation("Tạo thành công hóa đơn CRUD. Mã: {InvoiceCode}", newInvoice.InvoiceCode);

            return new InvoiceResponseDto
            {
                // ⚠️ GIỮ NGUYÊN MAPPING ID CHUẨN ĐỂ TRÁNH LỖI ID = 0
                InvoiceKhoiDNId = newInvoice.InvoiceKhoiDnid,

                InvoiceCode = newInvoice.InvoiceCode,
                ParkingSessionId = newInvoice.ParkingSessionId,
                TotalAmount = newInvoice.TotalAmount,
                Status = newInvoice.Status,
                CreatedAt = newInvoice.CreatedAt.GetValueOrDefault(),
                Description = newInvoice.Description
            };
        }

        // =========================================================================
        // 2. HÀM NGHIỆP VỤ THỰC CHIẾN (DÙNG CHO NÚT CHECK-IN VÀO BÃI XE)
        // =========================================================================
        public async Task<InvoiceResponseDto> CreateBusinessInvoiceAsync(InvoiceCreateRequestDto request)
        {
            _logger.LogInformation("[CHECK-IN] Ghi nhận xe vào bãi cho phiên ID: {SessionId}", request.ParkingSessionId);

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                throw new ArgumentException(validationResults.First().ErrorMessage ?? "Dữ liệu không hợp lệ.");
            }

            // A. Lấy giờ hệ thống và quy đổi sang giờ Việt Nam (UTC+7) để phân loại khung giờ
            var nowUtc = DateTime.UtcNow;
            var vietnamTime = nowUtc.AddHours(7);
            int currentHour = vietnamTime.Hour;

            // B. Áp dụng nghiệp vụ ngày và đêm
            string autoDescription;
            decimal autoBaseFee;

            // Từ 06:00 sáng đến 21:59 (9h59 tối) -> Gửi trong ngày
            if (currentHour >= 6 && currentHour < 22)
            {
                autoDescription = "xe gửi trong ngày";
                autoBaseFee = 5000m;
            }
            // Từ 22:00 tối đến 05:59 sáng -> Gửi qua đêm
            else
            {
                autoDescription = "xe gửi qua đêm";
                autoBaseFee = 40000m;
            }

            // Nối thêm ghi chú nếu người dùng có nhập thêm
            string finalDescription = string.IsNullOrWhiteSpace(request.Description)
                ? autoDescription
                : $"{autoDescription} - {request.Description.Trim()}";

            // C. Tạo Entity hóa đơn nghiệp vụ
            var newInvoice = new InvoicesKhoiDn
            {
                ParkingSessionId = request.ParkingSessionId,
                UserId = request.UserId > 0 ? request.UserId : 1, // Mặc định UserId = 1 theo nghiệp vụ bãi xe
                Description = finalDescription,
                InvoiceCode = $"INV-{vietnamTime:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                Status = "UNPAID",
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                BaseFee = autoBaseFee,
                PenaltyFee = 0m,
                TotalAmount = autoBaseFee
            };

            await _invoicesKhoiDnRepository.CreateAsync(newInvoice);
            await _invoicesKhoiDnRepository.SaveAsync();

            _logger.LogInformation("[CHECK-IN THÀNH CÔNG] Mã: {Code} | Khung: {Desc} | Phí cơ bản: {Fee:N0} VND",
                newInvoice.InvoiceCode, autoDescription, autoBaseFee);

            return new InvoiceResponseDto
            {
                InvoiceKhoiDNId = newInvoice.InvoiceKhoiDnid,
                InvoiceCode = newInvoice.InvoiceCode,
                ParkingSessionId = newInvoice.ParkingSessionId,
                TotalAmount = newInvoice.TotalAmount,
                Status = newInvoice.Status,
                CreatedAt = newInvoice.CreatedAt.GetValueOrDefault(),
                Description = newInvoice.Description
            };
        }

        // =========================================================================
        // CÁC HÀM KHÁC GIỮ NGUYÊN 100%
        // =========================================================================
        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _invoicesKhoiDnRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            _invoicesKhoiDnRepository.Remove(invoice);
            await _invoicesKhoiDnRepository.SaveAsync();
            return true;
        }

        public async Task<List<InvoiceResponseDto>> GetAllInvoicesAsync()
        {
            var invoices = await _invoicesKhoiDnRepository.GetAllAsync();
            return invoices.Select(invoice => new InvoiceResponseDto
            {
                InvoiceKhoiDNId = invoice.InvoiceKhoiDnid,
                InvoiceCode = invoice.InvoiceCode,
                ParkingSessionId = invoice.ParkingSessionId,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt.GetValueOrDefault(),
                Description = invoice.Description
            }).ToList();
        }

        public async Task<InvoiceDetailResponseDto> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _invoicesKhoiDnRepository.GetInvoiceWithTransactionsAsync(id);

            if (invoice == null)
            {
                return null!;
            }

            return new InvoiceDetailResponseDto
            {
                InvoiceKhoiDNId = invoice.InvoiceKhoiDnid,
                InvoiceCode = invoice.InvoiceCode,
                ParkingSessionId = invoice.ParkingSessionId,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt.GetValueOrDefault(),
                Description = invoice.Description,
                BaseFee = invoice.BaseFee,
                PenaltyFee = invoice.PenaltyFee.GetValueOrDefault(),

                Transactions = invoice.TransactionsKhoiDns?.Select(t => new TransactionBasicDto
                {
                    TransactionCode = t.TransactionCode,
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    PaymentStatus = t.PaymentStatus,
                    TransactionTime = t.TransactionTime.GetValueOrDefault()
                }).ToList() ?? new List<TransactionBasicDto>()
            };
        }

        public async Task<bool> UpdateInvoiceAsync(int id, string status, string description)
        {
            var invoice = await _invoicesKhoiDnRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            invoice.Status = status;
            invoice.Description = description;

            _invoicesKhoiDnRepository.Update(invoice);
            await _invoicesKhoiDnRepository.SaveAsync();
            return true;
        }

        public async Task<List<InvoiceResponseDto>> SearchInvoicesAsync(string keyword)
        {
            _logger.LogInformation("Tầng Service: Đang tìm kiếm hóa đơn với từ khóa: {Keyword}", keyword);

            var invoices = await _invoicesKhoiDnRepository.SearchInvoicesAsync(keyword);

            return invoices.Select(invoice => new InvoiceResponseDto
            {
                InvoiceKhoiDNId = invoice.InvoiceKhoiDnid,
                InvoiceCode = invoice.InvoiceCode,
                ParkingSessionId = invoice.ParkingSessionId,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt.GetValueOrDefault(),
                Description = invoice.Description
            }).ToList();
        }

        public async Task<List<InvoiceResponseDto>> SearchAdvancedAsync(int? parkingSessionId, string? status, string? description)
        {
            var invoices = await _invoicesKhoiDnRepository.SearchAdvancedAsync(parkingSessionId, status, description);

            return invoices.Select(invoice => new InvoiceResponseDto
            {
                InvoiceKhoiDNId = invoice.InvoiceKhoiDnid,
                InvoiceCode = invoice.InvoiceCode,
                ParkingSessionId = invoice.ParkingSessionId,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt.GetValueOrDefault(),
                Description = invoice.Description
            }).ToList();
        }
    }
}