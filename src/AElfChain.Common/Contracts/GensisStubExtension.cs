using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;

namespace AElfChain.Common.Contracts
{
    public static class GensisStubExtension
    {
        public static BasicContractZeroImplContainer.BasicContractZeroImplStub GetGenesisImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            return genesis.GetTestStub<BasicContractZeroImplContainer.BasicContractZeroImplStub>(caller);
        }
        
        public static AEDPoSContractContainer.AEDPoSContractStub GetConsensusStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.ToBase58());

            return contract.GetTestStub<AEDPoSContractContainer.AEDPoSContractStub>(caller);
        }

        public static AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.ToBase58());

            return contract.GetTestStub<AEDPoSContractImplContainer.AEDPoSContractImplStub>(caller);
        }

        public static ParliamentContractContainer.ParliamentContractStub GetParliamentAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.Parliament);

            var contract =
                new ParliamentContract(genesis.NodeManager, caller, parliamentAuth.ToBase58());

            return contract
                .GetTestStub<ParliamentContractContainer.ParliamentContractStub>(caller);
        }
        
        public static ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentAuthImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.Parliament);

            var contract =
                new ParliamentContract(genesis.NodeManager, caller, parliamentAuth.ToBase58());

            return contract
                .GetTestStub<ParliamentContractImplContainer.ParliamentContractImplStub>(caller);
        }

        public static ProfitContractContainer.ProfitContractStub GetProfitStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var profit = genesis.GetContractAddressByName(NameProvider.Profit);

            var contract = new ProfitContract(genesis.NodeManager, caller, profit.ToBase58());

            return contract.GetTestStub<ProfitContractContainer.ProfitContractStub>(caller);
        }
        
        public static ProfitContractImplContainer.ProfitContractImplStub GetProfitImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var profit = genesis.GetContractAddressByName(NameProvider.Profit);

            var contract = new ProfitContract(genesis.NodeManager, caller, profit.ToBase58());

            return contract.GetTestStub<ProfitContractImplContainer.ProfitContractImplStub>(caller);
        }

        public static TokenContractContainer.TokenContractStub GetTokenStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var token = genesis.GetContractAddressByName(NameProvider.Token);

            var contract = new TokenContract(genesis.NodeManager, caller, token.ToBase58());

            return contract.GetTestStub<TokenContractContainer.TokenContractStub>(caller);
        }

        public static TokenContractImplContainer.TokenContractImplStub GetTokenImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
        
            var token = genesis.GetContractAddressByName(NameProvider.Token);
        
            var contract = new TokenContract(genesis.NodeManager, caller, token.ToBase58());
        
            return contract.GetTestStub<TokenContractImplContainer.TokenContractImplStub>(caller);
        }

        public static TokenHolderContractContainer.TokenHolderContractStub GetTokenHolderStub(
            this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenHolder = genesis.GetContractAddressByName(NameProvider.TokenHolder);

            var contract = new TokenHolderContract(genesis.NodeManager, caller, tokenHolder.ToBase58());

            return contract.GetTestStub<TokenHolderContractContainer.TokenHolderContractStub>(caller);
        }
        
        public static TokenHolderContractImplContainer.TokenHolderContractImplStub GetTokenHolderImplStub(
            this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenHolder = genesis.GetContractAddressByName(NameProvider.TokenHolder);

            var contract = new TokenHolderContract(genesis.NodeManager, caller, tokenHolder.ToBase58());

            return contract.GetTestStub<TokenHolderContractImplContainer.TokenHolderContractImplStub>(caller);
        }

        public static TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);

            var contract =
                new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.ToBase58());

            return contract
                .GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(caller);
        }
        
        public static TokenConverterContractImplContainer.TokenConverterContractImplStub GetTokenConverterImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);

            var contract =
                new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.ToBase58());

            return contract
                .GetTestStub<TokenConverterContractImplContainer.TokenConverterContractImplStub>(caller);
        }

        public static TreasuryContractContainer.TreasuryContractStub GetTreasuryStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var treasury = genesis.GetContractAddressByName(NameProvider.Treasury);

            var contract = new TreasuryContract(genesis.NodeManager, caller, treasury.ToBase58());

            return contract.GetTestStub<TreasuryContractContainer.TreasuryContractStub>(caller);
        }
        public static TreasuryContractImplContainer.TreasuryContractImplStub GetTreasuryImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var treasury = genesis.GetContractAddressByName(NameProvider.Treasury);

            var contract = new TreasuryContract(genesis.NodeManager, caller, treasury.ToBase58());

            return contract.GetTestStub<TreasuryContractImplContainer.TreasuryContractImplStub>(caller);
        }

        public static VoteContractContainer.VoteContractStub GetVoteStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var vote = genesis.GetContractAddressByName(NameProvider.Vote);

            var contract = new VoteContract(genesis.NodeManager, caller, vote.ToBase58());

            return contract.GetTestStub<VoteContractContainer.VoteContractStub>(caller);
        }
        
        public static VoteContractImplContainer.VoteContractImplStub GetVoteImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var vote = genesis.GetContractAddressByName(NameProvider.Vote);

            var contract = new VoteContract(genesis.NodeManager, caller, vote.ToBase58());

            return contract.GetTestStub<VoteContractImplContainer.VoteContractImplStub>(caller);
        }

        public static ElectionContractContainer.ElectionContractStub GetElectionStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var election = genesis.GetContractAddressByName(NameProvider.Election);

            var contract = new ElectionContract(genesis.NodeManager, caller, election.ToBase58());

            return contract.GetTestStub<ElectionContractContainer.ElectionContractStub>(caller);
        }
        
        public static ElectionContractImplContainer.ElectionContractImplStub GetElectionImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var election = genesis.GetContractAddressByName(NameProvider.Election);

            var contract = new ElectionContract(genesis.NodeManager, caller, election.ToBase58());

            return contract.GetTestStub<ElectionContractImplContainer.ElectionContractImplStub>(caller);
        }

        public static CrossChainContractContainer.CrossChainContractStub GetCrossChainStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var cross = genesis.GetContractAddressByName(NameProvider.CrossChain);

            var contract = new CrossChainContract(genesis.NodeManager, caller, cross.ToBase58());

            return contract.GetTestStub<CrossChainContractContainer.CrossChainContractStub>(caller);
        }
        
        public static CrossChainContractImplContainer.CrossChainContractImplStub GetCrossChainImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var cross = genesis.GetContractAddressByName(NameProvider.CrossChain);

            var contract = new CrossChainContract(genesis.NodeManager, caller, cross.ToBase58());

            return contract.GetTestStub<CrossChainContractImplContainer.CrossChainContractImplStub>(caller);
        }

        public static AssociationContractContainer.AssociationContractStub GetAssociationAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var association = genesis.GetContractAddressByName(NameProvider.Association);

            var contract =
                new AssociationContract(genesis.NodeManager, caller, association.ToBase58());

            return contract.GetTestStub<AssociationContractContainer.AssociationContractStub>(caller);
        }
        
        public static AssociationContractImplContainer.AssociationContractImplStub GetAssociationAuthImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var association = genesis.GetContractAddressByName(NameProvider.Association);

            var contract =
                new AssociationContract(genesis.NodeManager, caller, association.ToBase58());

            return contract.GetTestStub<AssociationContractImplContainer.AssociationContractImplStub>(caller);
        }


        public static ReferendumContractContainer.ReferendumContractStub GetReferendumAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var referendumAuth = genesis.GetContractAddressByName(NameProvider.Referendum);

            var contract =
                new ReferendumContract(genesis.NodeManager, caller, referendumAuth.ToBase58());

            return contract
                .GetTestStub<ReferendumContractContainer.ReferendumContractStub>(caller);
        }
        
        public static ReferendumContractImplContainer.ReferendumContractImplStub GetReferendumAuthImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var referendumAuth = genesis.GetContractAddressByName(NameProvider.Referendum);

            var contract =
                new ReferendumContract(genesis.NodeManager, caller, referendumAuth.ToBase58());

            return contract
                .GetTestStub<ReferendumContractImplContainer.ReferendumContractImplStub>(caller);
        }

        public static ConfigurationContainer.ConfigurationStub GetConfigurationStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            var contract =
                new ConfigurationContract(genesis.NodeManager, caller, configuration.ToBase58());

            return contract.GetTestStub<ConfigurationContainer.ConfigurationStub>(caller);
        }
        
        public static ConfigurationImplContainer.ConfigurationImplStub GetConfigurationImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            var contract =
                new ConfigurationContract(genesis.NodeManager, caller, configuration.ToBase58());

            return contract.GetTestStub<ConfigurationImplContainer.ConfigurationImplStub>(caller);
        }
    }
}