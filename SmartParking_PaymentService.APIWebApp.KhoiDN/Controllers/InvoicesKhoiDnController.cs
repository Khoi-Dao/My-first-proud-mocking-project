using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SU26_PRN232_Payment.Services.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN.DTOs;
using Microsoft.AspNetCore.OData.Query;
using MassTransit;
using SmartParking.BusinessObject.Shared.Models.KhoiDN.Events;

namespace SmartParking.Invoices.Microservice.KhoiDN.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoicesKhoiDnController : ControllerBase
    {
        private readonly IInvoicesKhoiDnService _invoiceService;
        private readonly ILogger<InvoicesKhoiDnController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public InvoicesKhoiDnController(
            IInvoicesKhoiDnService invoiceService,
            ILogger<InvoicesKhoiDnController> logger,
            IPublishEndpoint publishEndpoint)
        {
            _invoiceService = invoiceService;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// API khởi tạo hóa đơn mới khi xe chuẩn bị ra khỏi bãi (Tích hợp phát Event lên RabbitMQ)
        /// </summary>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreateInvoice([FromBody] InvoiceCreateRequestDto request)
        {
            _logger.LogInformation("Nhận HTTP POST Request tạo hóa đơn mới.");

            var createdInvoice = await _invoiceService.CreateInvoiceAsync(request);

            if (createdInvoice != null)
            {
                var eventMessage = new InvoiceCreatedEvent
                {
                    // FIX LỖI CS1061: Dùng ParkingSessionId làm ID liên kết vì DTO không có InvoiceKhoiDnid
                    InvoiceId = createdInvoice.ParkingSessionId ?? 0,
                    InvoiceCode = createdInvoice.InvoiceCode ?? string.Empty,
                    ParkingSessionId = createdInvoice.ParkingSessionId ?? 0,
                    // FIX LỖI CS0019: Bỏ ?? 0 vì request.UserId đã là kiểu int bắt buộc
                    UserId = request.UserId,
                    TotalAmount = createdInvoice.TotalAmount,
                    CreatedAt = createdInvoice.CreatedAt,
                    Description = createdInvoice.Description ?? string.Empty
                };

                await _publishEndpoint.Publish(eventMessage);

                // FIX LỖI CS1061: Ghi log theo InvoiceCode thay vì InvoiceKhoiDnid
                _logger.LogInformation("Đã publish sự kiện InvoiceCreatedEvent cho hóa đơn mã: {Code} lên RabbitMQ.", createdInvoice.InvoiceCode);
            }
            else
            {
                _logger.LogWarning("Tạo hóa đơn thất bại, không có sự kiện nào được publish.");
            }

            return Ok(1);
        }

        /// <summary>
        /// API lấy thông tin chi tiết hóa đơn kèm theo lịch sử giao dịch
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InvoiceDetailResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceDetailResponseDto>> GetInvoiceById([FromRoute] int id)
        {
            _logger.LogInformation("Nhận HTTP GET Request lấy chi tiết hóa đơn ID: {Id}", id);

            var result = await _invoiceService.GetInvoiceByIdAsync(id);

            if (result == null)
            {
                _logger.LogWarning("API trả về 404 Not Found cho Hóa đơn ID: {Id}", id);
                return NotFound(new { message = $"Không tìm thấy hóa đơn với ID {id}." });
            }

            return Ok(result);
        }

        /// <summary>
        /// API Lấy toàn bộ danh sách (Read All) - Đã qua Service
        /// </summary>
        [HttpGet]
        [EnableQuery]
        public async Task<ActionResult<List<InvoiceResponseDto>>> GetAllInvoices()
        {
            _logger.LogInformation("API: Gọi lấy danh sách hóa đơn (qua OData).");
            var result = await _invoiceService.GetAllInvoicesAsync();

            return Ok(result.AsQueryable());
        }

        /// <summary>
        /// API Cập nhật hóa đơn (Update) - Tích hợp Publish Event khi Check-out thu tiền
        /// </summary>
        [Authorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(int))]
        public async Task<ActionResult<int>> UpdateInvoice(int id, [FromBody] InvoiceUpdateRequestDto request)
        {
            _logger.LogInformation("API: Gọi cập nhật hóa đơn ID: {Id}", id);

            // 1. Lấy thông tin hóa đơn hiện tại từ DB để biết số tiền cần thanh toán
            var existingInvoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (existingInvoice == null)
            {
                return NotFound(0);
            }

            // 2. Cập nhật trạng thái mới
            var isUpdated = await _invoiceService.UpdateInvoiceAsync(id, request.Status, request.Description);
            if (!isUpdated)
            {
                return NotFound(0);
            }

            // 3. THUẦN EVENT-DRIVEN: Nếu trạng thái được cập nhật thành "PAID", bắn tin nhắn lên RabbitMQ
            if (request.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            {
                var eventMessage = new InvoiceCreatedEvent
                {
                    InvoiceId = existingInvoice.InvoiceKhoiDNId,
                    InvoiceCode = existingInvoice.InvoiceCode ?? string.Empty,
                    ParkingSessionId = existingInvoice.ParkingSessionId ?? 0,
                    UserId = request.UserId,
                    TotalAmount = existingInvoice.TotalAmount,
                    CreatedAt = DateTime.UtcNow,
                    Description = request.Description
                };

                await _publishEndpoint.Publish(eventMessage);
                _logger.LogInformation("Đã publish sự kiện thanh toán cho hóa đơn {Code} lên RabbitMQ.", existingInvoice.InvoiceCode);
            }

            return Ok(1);
        }

        /// <summary>
        /// API Xóa hóa đơn (Delete) - Trả về True/False
        /// </summary>
        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(bool))]
        public async Task<ActionResult<bool>> DeleteInvoice(int id)
        {
            _logger.LogInformation("API: Gọi xóa hóa đơn ID: {Id}", id);
            var isDeleted = await _invoiceService.DeleteInvoiceAsync(id);

            if (!isDeleted)
            {
                return NotFound(false);
            }

            return Ok(true);
        }

        /// <summary>
        /// API Tìm kiếm hóa đơn (Quét qua Mã hóa đơn, Trạng thái, và Mô tả)
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InvoiceResponseDto>))]
        public async Task<ActionResult<List<InvoiceResponseDto>>> SearchInvoices([FromQuery] string keyword)
        {
            _logger.LogInformation("API: Nhận yêu cầu tìm kiếm hóa đơn với từ khóa: {Keyword}", keyword);

            var result = await _invoiceService.SearchInvoicesAsync(keyword ?? "");

            return Ok(result);
        }

        /// <summary>
        /// API Tìm kiếm nâng cao (Lọc linh hoạt theo ParkingSessionId, Status, Description)
        /// </summary>
        [HttpGet("advancedsearch")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InvoiceResponseDto>))]
        public async Task<ActionResult<List<InvoiceResponseDto>>> SearchInvoicesAdvanced(
            [FromQuery] int? parkingSessionId,
            [FromQuery] string? status,
            [FromQuery] string? description)
        {
            _logger.LogInformation("Nhận Request Tìm kiếm nâng cao.");

            var result = await _invoiceService.SearchAdvancedAsync(parkingSessionId, status, description);

            return Ok(result);
        }
    }
}