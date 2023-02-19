
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using SqsPublisher;

var sqsClient = new AmazonSQSClient();

var customer = new CustomerCreated
{
    Id = Guid.NewGuid(),
    FullName = "John Doe",
    Email = "john.doe@example.com",
    GitHubUsername = "johndoe",
    DateOfBirth = new DateTime(1980, 1, 1)
};

var queueUrlResponse = await sqsClient.GetQueueUrlAsync("customers");
string qUrl = queueUrlResponse.QueueUrl;

var sendMessageRequest = new SendMessageRequest
{
    QueueUrl = qUrl,
    MessageBody = JsonSerializer.Serialize(customer),
    MessageAttributes = new Dictionary<string, MessageAttributeValue>
    {
        { 
            "MessageType", new MessageAttributeValue
            {
                DataType = "String", 
                StringValue = nameof(CustomerCreated)
            }
        }
    },
    DelaySeconds = 1
};

var response = await sqsClient.SendMessageAsync(sendMessageRequest);

Console.WriteLine();

