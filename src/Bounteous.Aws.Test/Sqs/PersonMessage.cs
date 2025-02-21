using System;

namespace Bounteous.Aws.Test.Sqs
{
    public class PersonMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Done { get; set; }
    }
}