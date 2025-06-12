using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using HexMaster.AspireDemo.WebApi.Messages;
using Microsoft.Extensions.Logging;

namespace HexMaster.AspireDemo.WebApi.Services;

public class MessageSubscriberService : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<MessageSubscriberService> _logger;
    private ServiceBusProcessor? _processor;
    private readonly ActivitySource _activitySource;

    public MessageSubscriberService(
        ServiceBusClient serviceBusClient,
        ILogger<MessageSubscriberService> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _activitySource = new ActivitySource(nameof(MessageSubscriberService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _serviceBusClient.CreateProcessor("message");
        
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        _logger.LogInformation("Message subscriber service started");
        
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the service is stopping
        }
        finally
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(CancellationToken.None);
            }
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        // Extract diagnostic context from message
        var messageBody = args.Message.Body.ToString();
        
        // Create activity to track processing
        using var activity = _activitySource.StartActivity("ProcessMessage", ActivityKind.Consumer);
        
        // Extract and link to parent trace if available
        if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var diagnosticId) && 
            diagnosticId is string traceparent)
        {
            activity?.SetParentId(traceparent);
        }

        try
        {
            var message = JsonSerializer.Deserialize<PublishedMessage>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message != null)
            {
                // Add message details to activity
                activity?.AddTag("message.id", message.Id);
                activity?.AddTag("message.sent", message.SentOn.ToString("o"));
                
                _logger.LogInformation(
                    "Received message {MessageId} sent at {SentOn} with trace {TraceId}",
                    message.Id,
                    message.SentOn,
                    activity?.TraceId);
            }
            else
            {
                _logger.LogWarning("Received empty or invalid message");
            }

            // Complete the message to remove it from the queue
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error processing message");
            
            // Abandon the message to make it available for reprocessing
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing Service Bus message: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}