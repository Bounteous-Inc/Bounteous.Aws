using System;

namespace Bounteous.Aws.IoC
{
    public class LazyProvider<TProvider> : ILazyProvider<TProvider>
    {
        private readonly Func<TProvider> func;

        public LazyProvider(Func<TProvider> func)
        {
            this.func = func;
        }
        
        public TProvider Create()
        {
            return func();
        }
    }
}