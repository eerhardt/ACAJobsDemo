using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ACAJobsDemo.ProcessorJob;

public class QueueProcessorService : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<QueueProcessorService> _logger;
    private readonly TimeSpan _delayBetweenPolls = TimeSpan.FromSeconds(10);

    public QueueProcessorService(QueueClient queueClient, IHostApplicationLifetime hostApplicationLifetime, ILogger<QueueProcessorService> logger)
    {
        _queueClient = queueClient;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue Processor Service started");

        var emptyQueueTimes = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Receive messages from the queue (up to 32 at a time)
                QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: 32,
                    visibilityTimeout: TimeSpan.FromMinutes(1),
                    cancellationToken: stoppingToken);

                if (messages?.Length > 0)
                {
                    _logger.LogInformation("Processing {MessageCount} messages from queue", messages.Length);

                    foreach (QueueMessage message in messages)
                    {
                        await ProcessMessage(message, stoppingToken);
                    }

                    emptyQueueTimes = 0;
                }
                else if (emptyQueueTimes >= 3)
                {
                    // exit the app if no messages were processed in the last poll
                    _logger.LogInformation("No messages found in the queue. Exiting the processor.");
                    _hostApplicationLifetime.StopApplication();
                    break;
                }
                else
                {
                    emptyQueueTimes++;
                    // No messages available, wait before polling again
                    await Task.Delay(_delayBetweenPolls, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Queue processing was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing queue messages");
                
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(_delayBetweenPolls, stoppingToken);
            }
        }

        _logger.LogInformation("Queue Processor Service stopped");
    }

    private async Task ProcessMessage(QueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {MessageId} - Content: {MessageText}", 
                message.MessageId, message.MessageText);

            // Simulate some processing work
            await Task.Delay(100, cancellationToken);

            // Log the message content
            _logger.LogInformation("Successfully processed message: {MessageText}", message.MessageText);

            // Delete the message from the queue after successful processing
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            
            _logger.LogDebug("Message {MessageId} deleted from queue", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}: {MessageText}", 
                message.MessageId, message.MessageText);
            
            // The message will become visible again after the visibility timeout expires
            // You could implement retry logic or dead letter queue handling here
        }
    }
}