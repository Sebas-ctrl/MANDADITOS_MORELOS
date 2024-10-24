using Amazon.S3;
using MANDADITOS_MORELOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly MorelosContext _context;
        public PaymentsController(MorelosContext context)
        {
            _context = context;
        }

        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentCreateRequest request)
        {
            DateTime fecha = DateTime.Now;
            if (request.Method.ToString().Equals("Cash"))
            {
                decimal monto = request.Amount / 100.0m;
                monto = Math.Round(monto * 2, MidpointRounding.AwayFromZero) / 2;

                var pago = new PagosModel
                {
                    Monto = monto,
                    Fecha = fecha
                };

                _context.Pagos.Add(pago);
                _context.SaveChanges();

                return Ok(new { pagoID = pago.ID });
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "manual"
            };

            var service = new PaymentIntentService();
            try
            {
                var paymentIntent = service.Create(options);

                Console.WriteLine("Payment: " + paymentIntent);

                var pago = new PagosModel
                {
                    PaymentID = paymentIntent.Id,
                    Estatus = "created",
                    Monto = request.Amount / 100.0m,
                    Fecha = fecha
                };

                _context.Pagos.Add(pago);
                _context.SaveChanges();

                return Ok(new { clientSecret = paymentIntent.ClientSecret, pagoID = pago.ID });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.StripeError.Message });
            }
        }

        [HttpPost("capture-payment-intent")]
        public IActionResult CapturePaymentIntent([FromBody] CapturePaymentRequest request)
        {
            var service = new PaymentIntentService();
            try
            {
                var paymentIntent = service.Capture(request.PaymentIntentId);
                return Ok(new { status = paymentIntent.Status });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.StripeError.Message });
            }
        }

        public class CapturePaymentRequest
        {
            public string PaymentIntentId { get; set; }
        }

    }

    public class PaymentIntentCreateRequest
    {
        public MethodEnum Method { get; set; }
        public long Amount { get; set; }
        public string? Currency { get; set; }

        public enum MethodEnum
        {
            Card = 1,
            Cash = 2
        }
    }
}
