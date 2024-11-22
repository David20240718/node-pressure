using System.Numerics;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SystemContractTest;

[TestClass]
public class ConsensusContractRandomHashTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }

    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private ConsensusContract _consensusContract;
    private ParliamentContract _parliament;

    private string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
    private string Test1 { get; } = "2ac5jcjsNsPQsinNmnfBsYfy8PJaj3LbTUJMtn51nWd4fC2s1W";
    private string Test2 { get; } = "q3JQw1YLXYz3LbQq5Joo2cCoch6af5aKxCcEa6rqi64BNsaEX";
    private string Test3 { get; } = "DBLDqxWRzEqpqrzrT98RG5UvmgeLhPCH5bFSeEqikSQRPCAf1";
    private static string RpcUrl { get; } = "127.0.0.1:8001";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ConsensusRandomHashTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _consensusContract = _genesisContract.GetConsensusContract(InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _parliament = _genesisContract.GetParliamentContract(InitAccount);
    }

    [TestMethod]
    public void CheckRandomHash()
    {
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var randomHashMap = new Dictionary<long, Hash>();
        for (var i = height - 100; i < height + 100; i++)
        {
            var hash = _consensusContract.GetRandomHash(i);
            randomHashMap.Keys.Contains(i).ShouldBeFalse();
            randomHashMap.Values.Contains(hash).ShouldBeFalse();

            randomHashMap.Add(i, hash);
            Logger.Info($"{i}: {hash.ToHex()}");

            var blockInfo = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i, false));
            var pubkey = blockInfo.Header.SignerPubkey;
            Logger.Info($"{pubkey}");
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(1000)]
    [DataRow(2000)]
    [DataRow(3000)]
    [DataRow(5000)]
    public void GetRandomHash(long height)
    {
        var hash = _consensusContract.GetRandomHash(height);
        if (height == 1)
        {
            hash.ShouldBe(Hash.Empty);
        }

        Logger.Info(hash.ToHex());
    }

    [TestMethod]
    [DataRow("UpdateTinyBlockInformation")]
    [DataRow("UpdateValue")]
    [DataRow("NextRound")]
    public void CheckConsensusMethod(string methodName)
    {
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var check = true;
        while (height > 1 && check)
        {
            var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(height, true));
            foreach (var tx in block.Body.Transactions)
            {
                var transactionInfo = NodeManager.CheckTransactionResult(tx);
                if (!transactionInfo.Transaction.MethodName.Equals(methodName)) continue;
                Logger.Info($"{methodName}: {tx}");
                var param = transactionInfo.Transaction.Params;
                param.ShouldContain("randomNumber");
                Logger.Info(param);
                var randomHash = _consensusContract.GetRandomHash(height);
                var preRandomHash = _consensusContract.GetRandomHash(height - 1);
                var signPubkey = block.Header.SignerPubkey;
                var bpAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(signPubkey));
                var bpPrivate = NodeManager.AccountManager.GetPrivateKey(bpAddress.ToBase58());
                Logger.Info($"Method: {methodName}\n" +
                            $"BlockHeight: {height}\n" +
                            $"alpha: {preRandomHash.ToHex()}\n" +
                            $"beta: {randomHash.ToHex()}\n" +
                            $"pk: {signPubkey}\n" +
                            $"sk: {bpPrivate.ToHex()}");
                check = false;
            }

            height--;
        }
    }

    [TestMethod]
    [DataRow(50)]
    [DataRow(100)]
    public void CheckVRF(long blockCount)
    {
        var path = CommonHelper.MapPath($"logs/");
        List<VRF> vrfs = new List<VRF>();
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        blockCount = blockCount > height ? height : blockCount;

        for (var i = height - blockCount; i <= height; i++)
        {
            var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i, true));
            foreach (var tx in block.Body.Transactions)
            {
                var transactionInfo = NodeManager.CheckTransactionResult(tx);
                var methodName = transactionInfo.Transaction.MethodName;
                if (!transactionInfo.Transaction.To.Equals(_consensusContract.ContractAddress)) continue;
                var param = transactionInfo.Transaction.Params;
                param.ShouldContain("randomNumber");
                Logger.Info(param);
                var randomHash = _consensusContract.GetRandomHash(i);
                var preRandomHash = _consensusContract.GetRandomHash(i - 1);
                var signPubkey = block.Header.SignerPubkey;
                var bpAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(signPubkey));
                var bpPrivate = NodeManager.AccountManager.GetPrivateKey(bpAddress.ToBase58());
                vrfs.Add(new VRF
                {
                    beta = randomHash.ToHex(),
                    alpha = preRandomHash.ToHex(),
                    pi = "",
                    pk = signPubkey,
                    sk = bpPrivate.ToHex()
                });
                Logger.Info($"Method: {methodName}\n" +
                            $"BlockHeight: {i}\n" +
                            $"alpha: {preRandomHash.ToHex()}\n" +
                            $"beta: {randomHash.ToHex()}\n" +
                            $"pk: {signPubkey}\n" +
                            $"sk: {bpPrivate.ToHex()}");
            }
        }

        using FileStream fs = new FileStream($"{path}/vrf-{blockCount}.json", FileMode.Create);
        string json = JsonConvert.SerializeObject(vrfs.ToArray(), Formatting.Indented);
        File.WriteAllText($"{path}/vrf-{blockCount}.json", json);
    }
    
    [TestMethod]
    public void RecordBlock()
    {
        var path = CommonHelper.MapPath($"logs/");
        List<VRF> vrfs = new List<VRF>();
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);

        for (var i = height - 1000; i <= height; i++)
        {
            var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i, true));
            
                var randomHash = _consensusContract.GetRandomHash(i);
                var preRandomHash = _consensusContract.GetRandomHash(i - 1);
                var signPubkey = block.Header.SignerPubkey;
                var bpAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(signPubkey));
                var bpPrivate = NodeManager.AccountManager.GetPrivateKey(bpAddress.ToBase58());
                vrfs.Add(new VRF
                {
                    beta = randomHash.ToHex(),
                    alpha = preRandomHash.ToHex(),
                    pi = "",
                    pk = signPubkey,
                    sk = bpPrivate.ToHex()
                });
                Logger.Info($"BlockHeight: {i}\n" +
                            $"alpha: {preRandomHash.ToHex()}\n" +
                            $"beta: {randomHash.ToHex()}\n" +
                            $"pk: {signPubkey}\n" +
                            $"sk: {bpPrivate.ToHex()}");
        }

        using FileStream fs = new FileStream($"{path}/vrf-1000.json", FileMode.Create);
        string json = JsonConvert.SerializeObject(vrfs.ToArray(), Formatting.Indented);
        File.WriteAllText($"{path}/vrf-1000.json", json);
    }
    
    [TestMethod]
    [DataRow(5000)]
    [DataRow(10000)]
    public void RecordBlockRandomHash(long blockCount)
    {
        var path = CommonHelper.MapPath($"logs/");
        List<RandomHash> randomHashes = new List<RandomHash>();
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        Logger.Info($"StartHeight: {height - blockCount }, EndHeight: {height}");
        for (var i = height - blockCount; i <= height; i++)
        {
            var randomHash = _consensusContract.GetRandomHash(i);
            randomHashes.Add(new RandomHash{randomHash = randomHash.ToHex()});
        }

        using FileStream fs = new FileStream($"{path}/random-{blockCount}.json", FileMode.Create);
        string json = JsonConvert.SerializeObject(randomHashes.ToArray(), Formatting.Indented);
        File.WriteAllText($"{path}/random-{blockCount}.json", json);
    }

    [TestMethod]
    public void CheckRandom()
    {
        var path = CommonHelper.MapPath($"logs/");
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var numberList = new List<long>();
        var bigNumberList = new List<long>();
        var smallNumberList = new List<long>();
        var distribution = new Dictionary<int, long>();
        for (var i = 0; i < 26; i++)
        {
            distribution.Add(i, 0);
        }

        for (var i = height - 100; i < height; i++)
        {
            var hash = _consensusContract.GetRandomHash(i);
            Logger.Info($"{i}: {hash.ToHex()}");

            var bitArraySum = (int)ConvertHashToInt64(hash, 0, 256);
            Logger.Info(bitArraySum);
            numberList.Add(bitArraySum);
            if (bitArraySum < 128)
                bigNumberList.Add(bitArraySum);
            else
                smallNumberList.Add(bitArraySum);
            var s = bitArraySum.Div(10);
            Logger.Info(s);
            distribution[s] =+ 1;
        }

        foreach (var dis in distribution)
        {
            Logger.Info($"{dis.Key}:{dis.Value}");
        }
        Logger.Info($"{bigNumberList.Count}");
        Logger.Info($"{smallNumberList.Count}");
        
        using FileStream fs = new FileStream($"{path}/number.json", FileMode.Create);
        string json = JsonConvert.SerializeObject(numberList.ToArray(), Formatting.Indented);
        File.WriteAllText($"{path}/number.json", json);
    }

    [TestMethod]
    public void CheckMinerGenerated()
    {
        var round = _consensusContract.GetRoundId();
        for (var i = 3; i < round; i++)
        {
            
            var roundInfo = _consensusContract.GetRoundInformation(i);
            var minerCount = roundInfo.RealTimeMinersInformation.Keys.Count;
            Logger.Info($"Round: {i} MinerCount: {minerCount}");

            var firstMiner = roundInfo.RealTimeMinersInformation.Values.Where(v => v.Order.Equals(1)).ToList().First();
            var lastMiner = roundInfo.RealTimeMinersInformation.Values.Where(v => v.Order.Equals(minerCount)).ToList().First();
            var currentExtraBlock = roundInfo.RealTimeMinersInformation.Values.First(v => v.IsExtraBlockProducer.Equals(true));
            var lastExtraBlock = roundInfo.ExtraBlockProducerOfPreviousRound;
            
            if(firstMiner.Pubkey.Equals(lastExtraBlock))
            {
                Logger.Error($"{firstMiner.Pubkey}");
            }
            lastMiner.Pubkey.ShouldNotBe(currentExtraBlock.Pubkey);
        }
    }

    [TestMethod]
    public void CheckMinerGeneratedThroughMinedBlock()
    {
        var round = _consensusContract.GetRoundId();
        var roundInfo = _consensusContract.GetRoundInformation(round);
        var height = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var minerProduceBlock = new Dictionary<string, long>();
        foreach (var key in roundInfo.RealTimeMinersInformation.Keys)
        {
            minerProduceBlock.Add(key, 0);
        }

        var lastBlockMiner = "";
        for (var i = height - 50000; i < height; i++)
        {
            var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i, true));
            var signPubkey = block.Header.SignerPubkey;
            lastBlockMiner = lastBlockMiner == "" ? signPubkey : lastBlockMiner;
            
            if (lastBlockMiner == signPubkey)
            {
                minerProduceBlock[signPubkey] += 1;
                if (minerProduceBlock[signPubkey] > 8)
                {
                    Logger.Info($"{i}\n {signPubkey} \n  {minerProduceBlock[signPubkey]}");
                }
            }
            else
            {
                minerProduceBlock[lastBlockMiner] = 0;
                minerProduceBlock[signPubkey] += 1;
                lastBlockMiner = signPubkey;
            }
        }
    }

    public class VRF
    {
        public string beta { get; set; }
        public string alpha { get; set; }
        public string pi { get; set; }
        public string sk { get; set; }
        public string pk { get; set; }
    }
    
    private long ConvertHashToInt64(Hash hash, long start = 0, long end = long.MaxValue)
    {
        if (start < 0 || start > end) throw new ArgumentException("Incorrect arguments.");

        var range = end.Sub(start);
        var bigInteger = new BigInteger(hash.Value.ToByteArray());
        // This is safe because range is long type.
        var index = Math.Abs((long)(bigInteger % range));
        return index.Add(start);
    }
    
    public class RandomHash
    {
        public string randomHash { get; set; }
    }
}