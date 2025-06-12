using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using HexMaster.AspireDemo.WebApi.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HexMaster.AspireDemo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublisherController(ServiceBusClient serviceBusClient, ILogger<PublisherController> logger) : ControllerBase
    {
        private static readonly ActivitySource ActivitySource = new(nameof(PublisherController));

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var activity = ActivitySource.StartActivity("PublishMessage", ActivityKind.Producer);

            var sender = serviceBusClient.CreateSender("message");
            var message = new PublishedMessage(Guid.NewGuid(), DateTimeOffset.UtcNow);

            // Add the current activity to tags so we can track it
            activity?.AddTag("message.id", message.Id);
            activity?.AddTag("message.type", "PublishedMessage");

            var serviceBusMessage = new ServiceBusMessage(message.SerializeToJson())
            {
                ContentType = "application/json"
            };

            // Add current activity ID to the message for distributed tracing
            if (activity != null)
            {
                serviceBusMessage.ApplicationProperties.Add("Diagnostic-Id", activity.Id);
            }

            logger.LogInformation(
                "Publishing message {MessageId} with trace {TraceId}",
                message.Id,
                activity?.TraceId);

            await sender.SendMessageAsync(serviceBusMessage, HttpContext.RequestAborted);
            return Ok(message);
        }
    }
}
