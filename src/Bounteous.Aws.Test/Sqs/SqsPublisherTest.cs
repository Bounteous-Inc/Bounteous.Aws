using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bounteous.Aws.Test.Sqs.Doubles;
using Bounteous.Core.Extensions;
using Bounteous.Core.Validations;
using Moq;
using Xunit;

namespace Bounteous.Aws.Test.Sqs
{
    public class SqsPublisherTest : IDisposable
    {
        private readonly MockRepository mocks;
        private readonly Mock<IAmazonSQS> sqsClient;

        public SqsPublisherTest()
        {
            mocks = new MockRepository(MockBehavior.Strict);
            sqsClient = mocks.Create<IAmazonSQS>();
        }

        [Fact]
        public async Task CanSendMessage()
        {
            var publisher = new PersonPublisher(sqsClient.Object, "sqs.com");
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            var response = new SendMessageResponse();
            sqsClient.Setup(x => x.SendMessageAsync(It.Is<SendMessageRequest>(m => Matches(m, elvis)), 
                It.IsNotNull<CancellationToken>()))
                     .ReturnsAsync(response);

            await publisher.SendMessageAsync(elvis);
        }

        [Fact]
        public async Task CanSendMessageFifo()
        {
            var publisher = new PersonPublisher(sqsClient.Object, "sqs.com.fifo");
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            var response = new SendMessageResponse();
            sqsClient.Setup(x => x.SendMessageAsync(It.Is<SendMessageRequest>(m => Matches(m, elvis)), 
                It.IsNotNull<CancellationToken>()))
                     .ReturnsAsync(response);

            await publisher.SendMessageAsync(elvis);
        }

        [Fact]
        public async Task CanSendMessages()
        {
            var publisher = new PersonPublisher(sqsClient.Object, "sqs.com");
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            var response = new SendMessageBatchResponse();
            sqsClient.Setup(x => x.SendMessageBatchAsync(It.IsNotNull<SendMessageBatchRequest>(), It.IsNotNull<CancellationToken>()))
                .ReturnsAsync(response);

            await publisher.SendMessagesAsync(new[] {elvis});
        }

        [Fact]
        public async Task CanSendMessagesFifo()
        {
            var publisher = new PersonPublisher(sqsClient.Object, "sqs.com.fifo");
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            var response = new SendMessageBatchResponse();
            sqsClient.Setup(x => x.SendMessageBatchAsync(It.IsNotNull<SendMessageBatchRequest>(), It.IsNotNull<CancellationToken>()))
                .ReturnsAsync(response);

            await publisher.SendMessagesAsync(new[] {elvis});
        }
        
        private static bool Matches(SendMessageRequest actual, PersonMessage expected)
        {
            Validate.Begin()
                .IsNotNull(actual, "actual").Check()
                .IsNotNull(expected, "expected").Check()
                .IsNotNull(actual.MessageBody, "messageBody")
                .Check();

            var person = actual.MessageBody.FromJson<PersonMessage>();
            
            return Validate.Begin()
                .IsNotNull(person, "person").Check()
                .IsEqual(person.Id, expected.Id, "id")
                .IsEqual(person.Name, expected.Name, "Name")
                .Check()
                .IsValid();
        }

        public void Dispose()
        {
            mocks.VerifyAll();
        }
    }
}