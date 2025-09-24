using Azure.Storage.Queues;

namespace Frontend.Services;

public class QueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(QueueClient queueClient, ILogger<QueueService> logger)
    {
        _queueClient = queueClient;
        _logger = logger;
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            await _queueClient.SendMessageAsync(message);
            _logger.LogInformation("Message sent to queue: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to queue: {Message}", message);
            throw;
        }
    }
}