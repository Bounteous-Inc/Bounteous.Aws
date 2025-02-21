using Amazon.Extensions.NETCore.Setup;

namespace Bounteous.Aws
{
    public interface IApplicationConfigBase : Bounteous.Core.IApplicationConfigBase
    {
        AWSOptions AwsOptions { get; set; }
    }
}