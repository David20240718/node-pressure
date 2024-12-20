using AElf.Contracts.Election;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ElectionMethod
    {
        //action
        InitialElectionContract,
        AnnounceElection,
        AnnounceElectionFor,
        QuitElection,
        Vote,
        Withdraw,
        UpdateTermNumber,
        ChangeVotingOption,
        ReplaceCandidatePubkey,
        SetCandidateAdmin,
        EnableElection,
        FixWelfareProfit,

        //view
        GetCalculateVoteWeight,
        GetElectionResult,
        GetCandidateInformation,
        GetCandidates,
        GetCandidateVote,
        GetVotedCandidates,
        GetCandidateVoteWithRecords,
        GetCandidateVoteWithAllRecords,
        GetVictories,
        GetTermSnapshot,
        GetMinersCount,
        GetElectorVoteWithAllRecords,
        GetNextElectCountDown,
        GetElectorVoteWithRecords,
        GetElectorVote,
        GetVoteWeightSetting,
        GetVoteWeightProportion,
        GetDataCenterRankingList,
        GetMinerElectionVotingItemId,
        GetCandidateAdmin,
        GetNewestPubkey,
        GetReplacedPubkey,
        GetVotersCount,
        GetVotesAmount,
        
    }

    public class ElectionContract : BaseContract<ElectionMethod>
    {
        public ElectionContract(INodeManager nodeManager, string callAddress, string electionAddress)
            : base(nodeManager, electionAddress)
        {
            SetAccount(callAddress);
        }

        public ElectionContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.Election";

        public CandidateInformation GetCandidateInformation(string account)
        {
            var result =
                CallViewMethod<CandidateInformation>(ElectionMethod.GetCandidateInformation,
                    new StringValue
                    {
                        Value = NodeManager.GetAccountPublicKey(account)
                    });
            return result;
        }

        public long GetCandidateVoteCount(string candidatePublicKey)
        {
            var candidateVote = CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVote, new StringValue
            {
                Value = candidatePublicKey
            });

            return candidateVote.AllObtainedVotedVotesAmount;
        }

        public Hash GetMinerElectionVotingItemId()
        {
            var minerElectionVotingItemId = CallViewMethod<Hash>(ElectionMethod.GetMinerElectionVotingItemId, new Empty());

            return minerElectionVotingItemId;
        }
        
        public Address GetCandidateAdmin(string pubkey)
        {
            var candidateAdmin = CallViewMethod<Address>(ElectionMethod.GetCandidateAdmin, new StringValue{Value = pubkey});

            return candidateAdmin;
        }
        
        public string GetNewestPubkey(string pubkey)
        {
            var newestPubkey = CallViewMethod<StringValue>(ElectionMethod.GetNewestPubkey, new StringValue{Value = pubkey});

            return newestPubkey.Value;
        }
        
        public string GetReplacedPubkey(string pubkey)
        {
            var replacedPubkey = CallViewMethod<StringValue>(ElectionMethod.GetReplacedPubkey, new StringValue{Value = pubkey});

            return replacedPubkey.Value;
        }
        
        public long GetVotesAmount()
        {
            var votesAmount = CallViewMethod<Int64Value>(ElectionMethod.GetVotesAmount, new Empty());

            return votesAmount.Value;
        }
        
        public long GetVotersCount()
        {
            var votersCount =  CallViewMethod<Int64Value>(ElectionMethod.GetVotersCount, new Empty());

            return votersCount.Value;
        }
        
        public PubkeyList GetCandidates()
        {
            var result =
                CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                    new Empty());

            return result;
        }

        public ElectorVote GetElectorVoteWithRecords(string pubkey)
        {
            return CallViewMethod<ElectorVote>(ElectionMethod.GetElectorVoteWithRecords, 
                new StringValue
                {
                    Value = pubkey
                });
        }
        
        public CandidateVote GetCandidateVoteWithAllRecords(string pubkey)
        {
            return CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithAllRecords, 
                new StringValue
                {
                    Value = pubkey
                });
        }
    }
}