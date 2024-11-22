using System;

namespace AElfChain.Common.Contracts
{
    public enum NameProvider
    {
        Genesis,
        Election,
        Profit,
        Vote,
        Treasury,
        Token,
        TokenHolder,
        TokenConverter,
        Consensus,
        Parliament,
        CrossChain,
        Association,
        Configuration,
        Referendum,
        Economic
    }

    public static class NameProviderExtension
    {
        public static NameProvider ConvertNameProvider(this string name)
        {
            return (NameProvider) Enum.Parse(typeof(NameProvider), name, true);
        }
    }
}