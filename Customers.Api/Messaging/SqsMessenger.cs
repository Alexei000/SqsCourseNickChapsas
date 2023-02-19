using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace Customers.Api.Messaging;

public class SqsMessenger : ISqsMessenger
{
    private readonly IAmazonSQS _sqs;
    private readonly IOptions<QueueSettings> _queueSettings;

    private string? _queueUrl;

    public SqsMessenger(IAmazonSQS sqs, IOptions<QueueSettings> queueSettings)
    {
        _sqs = sqs;
        _queueSettings = queueSettings;
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken token)
    {
        if (_queueUrl is not null)
            return _queueUrl;

        var queueUrlResponse = await _sqs.GetQueueUrlAsync(_queueSettings.Value.Name, token);
        _queueUrl = queueUrlResponse.QueueUrl;
        return _queueUrl;
    }

    public async Task<SendMessageResponse> SendMessageAsync<T>(T message, CancellationToken token)
    {
        string queueUrl = await GetQueueUrlAsync(token);

        var sendMessageObject = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            {
                {
                    "MessageType", new MessageAttributeValue 
                    { 
                        DataType = "String", 
                        StringValue = typeof(T).Name
                    }
                }
            }
        };

        var response = await _sqs.SendMessageAsync(sendMessageObject, token);
        return response;
    }


}