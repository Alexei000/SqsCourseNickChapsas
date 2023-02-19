using System.Security.Principal;
using Amazon.SQS;
using Amazon.SQS.Model;

var sqsClient = new AmazonSQSClient();

var cts = new CancellationTokenSource();

var queueUrlResponse = await sqsClient.GetQueueUrlAsync("customers");
string qUrl = queueUrlResponse.QueueUrl;

var receiveMessageRequest = new ReceiveMessageRequest
{
    QueueUrl = qUrl,
    AttributeNames = new List<string> { "All" },
    MessageAttributeNames = new List<string> { "All" }
};

while (!cts.IsCancellationRequested)
{
    var response = await sqsClient.ReceiveMessageAsync(receiveMessageRequest, cts.Token);

    foreach (var message in response.Messages)
    {
        Console.WriteLine($"Message Id: {message.MessageId}");
        Console.WriteLine($"Message type: {message.MessageAttributes["MessageType"].StringValue}");
        Console.WriteLine($"Message body: {message.Body}");

        await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle, cts.Token);
    }

    await Task.Delay(3000);
}