using Amazon.SQS;
using Bounteous.Aws.Sqs;

namespace Bounteous.Aws.Test.Sqs.Doubles
{
    public interface IPersonPublisher : IPublishSqsMessages<PersonMessage> 
    {
    }
    
    public class PersonPublisher : SqsPublisher<PersonMessage>, IPersonPublisher
    {
        public PersonPublisher(IAmazonSQS sqsClient, string sqsUrl) : base(sqsClient, sqsUrl)
        {
        }
    }
}