using Microsoft.Extensions.Configuration;

namespace Bounteous.Aws
{
    public class ApplicationConfigurationBuilder<T> : Bounteous.Core.ApplicationConfigurationBuilder<T> where T : IApplicationConfigBase, new()
    {
        protected override T Build(IConfiguration config, T appConfig)
        {
            appConfig.AwsOptions = config.GetAWSOptions();
            return appConfig;
        }
    }
}