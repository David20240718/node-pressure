using AElf;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Vote;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace SystemContractTest;

[TestClass]
public class NetStander2_1TransactionTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }

    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private ConsensusContract _consensusContract;
    private ElectionContract _electionContract;
    private VoteContract _voteContract;

    private string InitAccount { get; } = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
    private static string RpcUrl { get; } = "127.0.0.1:8000";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ConsensusRandomHashTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes-new");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _consensusContract = _genesisContract.GetConsensusContract(InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _electionContract = _genesisContract.GetElectionContract(InitAccount);
        _voteContract = _genesisContract.GetVoteContract(InitAccount);
    }

    [TestMethod]
    // [DataRow("R3ehgbKyGkhLzYC6PVuffpAobe5iomsACBKLwnjKGH2EQc5iV","2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W")]
    [DataRow("2pmw7ZpB8yxL4ifKU8uH233b6bwE3wnhGb6BXxvFWSuiFd7v1G",
        "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W")]
    public void NewAccountCandidate(string account, string admin)
    {
        _tokenContract.TransferBalance(InitAccount, account, 10_1000_00000000);
        _electionContract.SetAccount(account);
        var result = _electionContract.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, admin.ConvertAddress());
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    [DataRow("gGJ7JBCVftbx3uTUJsexU2UhwWQRuFtz1REDSyjwd4pKWmKFs", 200000000000, 1, 0)]
    public void VoteCandidate(string voter, long amount, long lockTime, int candidateIndex)
    {
        var term = _consensusContract.GetCurrentTermInformation();
        Logger.Info($"Term: {term.TermNumber}");
        _tokenContract.TransferBalance(InitAccount, voter, amount + 100000000);
        var candidates = _electionContract.GetCandidates().Value;
        var voteCandidate = candidates[candidateIndex].ToByteArray();
        Logger.Info($"Address: {Address.FromPublicKey(voteCandidate)} \nPubkey: {voteCandidate.ToHex()}");
        _electionContract.SetAccount(voter);
        var vote = _electionContract.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
        {
            CandidatePubkey = voteCandidate.ToHex(),
            Amount = amount,
            EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromHours(lockTime)).ToTimestamp()
        });
        vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var voteId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(vote.ReturnValue));
        var logVoteId = Voted.Parser
            .ParseFrom(ByteString.FromBase64(
                vote.Logs.First(l => l.Name.Equals(nameof(Voted))).NonIndexed))
            .VoteId;
        Logger.Info($"VoteId: {voteId.ToHex()}");
        voteId.ShouldBe(logVoteId);

        var voteRecord = _voteContract.CallViewMethod<VotingRecord>(VoteMethod.GetVotingRecord, voteId);
        voteRecord.Amount.ShouldBe(amount);
        Logger.Info($"vote id is: {voteId}\n" +
                    $"{voteRecord.Amount}\n" +
                    $"time: {lockTime}");
        vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var result =
            _electionContract.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                new StringValue { Value = voteCandidate.ToHex() });
        result.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
    }

    [TestMethod]
    public void CheckInValue()
    {
        var round = _consensusContract.GetRoundId();
        var list = new List<Round>();
        for (var i = round - 10; i < round + 5; i++)
        {
            var info = _consensusContract.GetRoundInformation(i);
            list.Add(info);
        }

        Logger.Info(list);
    }


    [TestMethod]
    public void SetMaximumMinersCount()
    {
        var amount = 5;
        var maximumBlocksCount = _consensusContract.GetMaximumMinersCount().Value;
        Logger.Info($"{maximumBlocksCount}");
        var input = new Int32Value { Value = amount };
        var result = AuthorityManager.ExecuteTransactionWithAuthority(_consensusContract.ContractAddress,
            nameof(ConsensusMethod.SetMaximumMinersCount), input, InitAccount);
        result.Status.ShouldBe(TransactionResultStatus.Mined);
        var newMaximumBlocksCount = _consensusContract.GetMaximumMinersCount().Value;
        newMaximumBlocksCount.ShouldBe(maximumBlocksCount < amount ? maximumBlocksCount : amount);
        Logger.Info($"{newMaximumBlocksCount}");
    }

}