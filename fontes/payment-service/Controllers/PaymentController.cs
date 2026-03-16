using Locacao.PaymentService.Models;
using Locacao.PaymentService.Models.Commands;
using Locacao.PaymentService.Infrastructure.Repositories;
using Locacao.PaymentService.Infrastructure.Adapters;
using Microsoft.AspNetCore.Mvc;

namespace Locacao.PaymentService.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentGatewayAdapter _paymentGateway;

        public PaymentController(IPaymentRepository paymentRepository, IPaymentGatewayAdapter paymentGateway)
        {
            _paymentRepository = paymentRepository;
            _paymentGateway = paymentGateway;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment is null)
                return NotFound(new { Message = $"Pagamento com id '{id}' não encontrado." });

            return Ok(payment);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAsync([FromBody] ProcessPaymentCommand command)
        {
            var payment = command.ToModel();

            var approved = await _paymentGateway.ProcessAsync(payment);

            payment.Status = approved ? PaymentStatus.Confirmado : PaymentStatus.Falhou;
            payment.ProcessedAt = DateTimeOffset.UtcNow;

            await _paymentRepository.CreateAsync(payment);

            if (!approved)
                return UnprocessableEntity(new { Message = "Pagamento recusado pelo gateway.", payment });

            return Created($"/v1/payment/{payment.Id}", payment);
        }

        [HttpPost("{id:guid}/reembolso")]
        public async Task<IActionResult> RefundAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment is null)
                return NotFound(new { Message = $"Pagamento com id '{id}' não encontrado." });

            if (payment.Status != PaymentStatus.Confirmado)
                return BadRequest(new { Message = "Apenas pagamentos confirmados podem ser reembolsados." });

            var refunded = await _paymentGateway.RefundAsync(payment);
            if (!refunded)
                return UnprocessableEntity(new { Message = "Erro ao processar reembolso no gateway." });

            payment.Status = PaymentStatus.Reembolsado;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            return Ok(payment);
        }
    }
}
