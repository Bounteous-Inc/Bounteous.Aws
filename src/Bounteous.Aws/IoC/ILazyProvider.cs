namespace Bounteous.Aws.IoC
{
    public interface ILazyProvider<out TProvider> 
    {
        TProvider Create();
    }
}