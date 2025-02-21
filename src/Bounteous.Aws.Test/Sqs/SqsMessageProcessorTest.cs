using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Bounteous.Aws.Sqs;
using Bounteous.Aws.Test.Sqs.Doubles;
using Bounteous.Core.Extensions;
using Moq;
using Xunit;

namespace Bounteous.Aws.Test.Sqs
{
    public class SqsMessageProcessorTest : IDisposable
    {
        private readonly Mock<IConsumeSqsMessages<PersonMessage>> consumer;
        private readonly Mock<IPublishSqsMessages<PersonMessage>> publisher;
        private readonly MockRepository mocks;

        public SqsMessageProcessorTest()
        {
            mocks = new MockRepository(MockBehavior.Strict);
            consumer = mocks.Create<IConsumeSqsMessages<PersonMessage>>();
            publisher = mocks.Create<IPublishSqsMessages<PersonMessage>>();
        }

        [Fact]
        public void CanProcessMessage()
        {
            var processor = new PersonProcessor(consumer.Object, publisher.Object);
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            var message = new SQSEvent.SQSMessage {Body = elvis.ToJson()};
            consumer.Setup(x => x.Process(new[] {message}))
                    .Returns(Task.CompletedTask);
        
            processor.Process(new[] {message});
        }

        [Fact]
        public void CanSendMessage()
        {
            var processor = new PersonProcessor(consumer.Object, publisher.Object);
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            publisher.Setup(x => x.SendMessageAsync(elvis))
                     .ReturnsAsync(true);
        
            processor.SendMessageAsync(elvis);
        }

        [Fact]
        public void CanSendMessages()
        {
            var processor = new PersonProcessor(consumer.Object, publisher.Object);
            var elvis = new PersonMessage {Id = Guid.NewGuid(), Name = "Elvis"};
            publisher.Setup(x => x.SendMessagesAsync(new[] {elvis}))
                .ReturnsAsync(true);
        
            processor.SendMessagesAsync(new[] {elvis});
        }

        public void Dispose()
        {
            mocks.VerifyAll();
        }
    }
}