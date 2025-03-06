using System.Text.Json.Serialization;

namespace Bounteous.Aws.Test.Model
{
    public class Foo
    {
        public Foo() { }
        
        public Foo(string first, string last, int age = 10)
        {
            FirstName = first;
            LastName = last;
        }

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        [JsonPropertyName("Age")]
        public int Age { get; set; }
    }
}