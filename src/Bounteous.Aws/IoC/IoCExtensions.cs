using System;
using Amazon.SecretsManager;
using Amazon.SQS;
using Bounteous.Aws.Secrets;
using Bounteous.Aws.Sqs;
using Bounteous.Core.Utilities.ApplicationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Bounteous.Aws.IoC
{
    public static class IoCExtensions
    {
        public static IServiceCollection AddLazyProviderFor<T>(this IServiceCollection collection)
        {
            collection.AddSingleton<ILazyProvider<T>>(provider => new LazyProvider<T>(provider.GetService<T>));
            return collection;
        }

        public static IServiceCollection AddSecretProvider(this IServiceCollection collection, SecretConfigCollection secretConfigCollection)
        {
            collection.AddAWSService<IAmazonSecretsManager>();
            collection.AddSingleton<ISecretProvider>(provider => 
                new SecretProvider(secretConfigCollection, provider.GetService<IAmazonSecretsManager>()));
            return collection;
        }
        
        public static IServiceCollection AddLazySecretProvider(this IServiceCollection collection, SecretConfigCollection secretConfigCollection)
        {
            collection.AddAWSService<IAmazonSecretsManager>();
            collection.AddSingleton<ILazyProvider<ISecretProvider>>(provider =>
                new LazyProvider<ISecretProvider>(() =>
                    new SecretProvider(secretConfigCollection, provider.GetService<IAmazonSecretsManager>())));
            return collection;
        }

        public static IServiceCollection AddApplicationMonitoringWithSqsSink(this IServiceCollection collection, string sqsUrl)
        {
            return AddApplicationMonitoringWithSqsSink(collection, () => sqsUrl);
        }
        
        public static IServiceCollection AddApplicationMonitoringWithSqsSink(this IServiceCollection collection, Func<string> sqsUrlFunc)
        {
            collection.AddAWSService<IAmazonSQS>();
            collection.AddSingleton<IEventSink>(
                services => new SqsApplicationEventSink(services.GetService<IAmazonSQS>(), sqsUrlFunc()));
            collection.AddSingleton<IMonitorBuilder, MonitorBuilder>();
            return collection;
        }
    }
}