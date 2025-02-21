using System.Threading.Tasks;

namespace Bounteous.Aws.Secrets
{
    public interface ISecret
    {
        Task<string> GetSecretAsync();
    }
}