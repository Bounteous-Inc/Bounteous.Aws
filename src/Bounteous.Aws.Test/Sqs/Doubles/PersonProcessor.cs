using Bounteous.Aws.Sqs;

namespace Bounteous.Aws.Test.Sqs.Doubles
{
    public class PersonProcessor : SqsMessageProcessor<PersonMessage>
    {
        public PersonProcessor(IConsumeSqsMessages<PersonMessage> consumer, IPublishSqsMessages<PersonMessage> publisher) 
                : base(consumer, publisher)
        {
        }
    }
}