using Microsoft.AspNetCore.Mvc;
using InventoryService.Models;
using InventoryService.Services;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly KafkaProducerService _kafkaProducer;
        private readonly WebhookService _webhookService;

        public InventoryController(
            ILogger<InventoryController> logger,
            KafkaProducerService kafkaProducer,
            WebhookService webhookService)
        {
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _webhookService = webhookService;
        }

        [HttpPost("processInventory")]
        public async Task<IActionResult> ProcessInventory([FromQuery] string errorFlag = null)
        {
            var job = new InventoryJob();

            // Set error flag if provided
            if (!string.IsNullOrEmpty(errorFlag))
            {
                job.Status = "Error";

                switch (errorFlag.ToUpper())
                {
                    case "VALIDATION":
                        job.ErrorCode = ErrorCodes.ValidationError;
                        job.ErrorMessage = "Invalid inventory data provided";
                        break;
                    case "NOTFOUND":
                        job.ErrorCode = ErrorCodes.NotFoundError;
                        job.ErrorMessage = "Inventory item not found";
                        break;
                    case "AUTH":
                        job.ErrorCode = ErrorCodes.AuthorizationError;
                        job.ErrorMessage = "Not authorized to process inventory";
                        break;
                    case "DB":
                        job.ErrorCode = ErrorCodes.DatabaseError;
                        job.ErrorMessage = "Error connecting to inventory database";
                        break;
                    case "PROCESSING":
                        job.ErrorCode = ErrorCodes.ProcessingError;
                        job.ErrorMessage = "Error during inventory processing";
                        break;
                    default:
                        job.Status = "Pending"; // Reset to pending if unknown error flag
                        break;
                }
            }

            _logger.LogInformation($"Created inventory job with ID: {job.JobId}, Error Flag: {errorFlag}");

            // Publish job to Kafka
            await _kafkaProducer.ProduceAsync(job);

            // Return 202 Accepted with job ID
            return Accepted(new { jobId = job.JobId });
        }

        [HttpPost("registerWebhook")]
        public IActionResult RegisterWebhook([FromBody] WebhookRegistrationRequest request)
        {
            if (string.IsNullOrEmpty(request.CallbackUrl))
            {
                return BadRequest("Callback URL is required");
            }

            var registration = _webhookService.RegisterWebhook(request.CallbackUrl);

            return Ok(new { webhookId = registration.Id });
        }
    }

    public class WebhookRegistrationRequest
    {
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
