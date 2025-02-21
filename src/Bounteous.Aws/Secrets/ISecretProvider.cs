namespace Bounteous.Aws.Secrets
{
    public interface ISecretProvider
    {
        ISecret GetAwsSecret(string name);
    }
}