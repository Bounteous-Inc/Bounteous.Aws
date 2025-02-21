using System;
using Amazon.SecretsManager;
using Bounteous.Aws.Secrets;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bounteous.Aws.Test.Secrets
{
    public class SecretProviderTest : IDisposable
    {
        private readonly MockRepository mocks;

        private readonly Mock<IAmazonSecretsManager> manager;

        public SecretProviderTest()
        {
            mocks = new MockRepository(MockBehavior.Loose);
            manager = mocks.Create<IAmazonSecretsManager>();
        }
        
        [Fact]
        public void ShouldGetSecret()
        {
            const string configName = "test";
            var collection = new SecretConfigCollection
            {
                Items = new []{ new SecretConfig {Name = configName, Region = "us-east-2", SecretId = "a secret"}}
            };

            var systemUnderTest = new SecretProvider(collection, manager.Object);
            var actual = systemUnderTest.GetAwsSecret(configName);
            actual.Should().NotBeNull();
            actual.Should().BeAssignableTo<CachedSecret>();
        }
        
        [Fact]
        public void ShouldNotFindConfigInCollection()
        {
            var collection = new SecretConfigCollection
            {
                Items = new []{ new SecretConfig {Name = "test", Region = "us-east-2", SecretId = "a secret"}}
            };

            var systemUnderTest = new SecretProvider(collection, manager.Object);
            Action act = () => systemUnderTest.GetAwsSecret("fail");
            act.Should().Throw<SecretException>();
        }

        public void Dispose()
        {
            mocks.VerifyAll();
        }
    }
}