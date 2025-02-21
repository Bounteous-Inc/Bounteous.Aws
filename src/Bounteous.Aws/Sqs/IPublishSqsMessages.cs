using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bounteous.Core.Extensions;
using Serilog;

namespace Bounteous.Aws.Sqs
{
    public interface IPublishSqsMessages<T> where T : class
    {
        Task<bool> SendMessageAsync(T message);
        Task<bool> SendMessagesAsync(IEnumerable<T> messages);
    }

    public class SqsPublisher<T> : IPublishSqsMessages<T> where T : class
    {
        private readonly IAmazonSQS sqsClient;
        private string SqsQueueUrl { get; }

        public SqsPublisher(IAmazonSQS sqsClient, string sqsQueueUrl)
        {
            this.sqsClient = sqsClient;
            SqsQueueUrl = sqsQueueUrl;
        }

        public virtual async Task<bool> SendMessageAsync(T message)
        {
            var request = new SendMessageRequest(SqsQueueUrl, message.ToJson()).ApplyFifo(SqsQueueUrl);
            var response = await sqsClient.SendMessageAsync(request).ConfigureAwait(false);
            
            var successful = response.HttpStatusCode == HttpStatusCode.OK;
            if (!successful)
            {
                Log.Error("Sqs send failure: '{failureText}'", message.ToJson());
            }

            return successful;
        }

        public virtual async Task<bool> SendMessagesAsync(IEnumerable<T> messages)
        {
            var batchRequestEntries = messages.Select((m, i) => new SendMessageBatchRequestEntry(i.ToString(), m.ToJson())
                .ApplyFifo(SqsQueueUrl)).ToList();
            var request = new SendMessageBatchRequest(SqsQueueUrl, batchRequestEntries);
            var response = await sqsClient.SendMessageBatchAsync(request).ConfigureAwait(false);
            var successful = response.HttpStatusCode == HttpStatusCode.OK;
            
            if (!successful)
            {
                var failureText = response.Failed.Select(f => $"Unable to send '{batchRequestEntries[int.Parse(f.Id)].MessageBody}' because {f.Message}");
                Log.Error("Sqs batch send failure: {failureText}", string.Join(Environment.NewLine, failureText));
            }

            return successful;
        }
    }
}