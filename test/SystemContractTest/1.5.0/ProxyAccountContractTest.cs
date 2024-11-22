using AElf;
using AElf.Contracts.AgentInterface;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.ProxyAccountContract;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
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
using Volo.Abp.Threading;

namespace SystemContractTest;

[TestClass]
public class ProxyAccountContractTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private List<INodeManager> SideNodeManagers { get;} = new();
    private AuthorityManager AuthorityManager { get; set; }
    private List<AuthorityManager> SideAuthorities { get; } = new();

    private TokenContract _tokenContract;
    private List<TokenContract> _sideTokenContracts { get; } = new();


    private GenesisContract _genesisContract;
    private List<GenesisContract> _sideGenesisContracts { get; } = new();

    
    private string _proxyContractAddress = "";
    private string _authortyContract = "";

    private List<string> _sideProxyContractAddresss = new()
        { ""};

    private ProxyAccountContract _proxyAccountContract;
    private List<ProxyAccountContract> _sideProxyAccountContracts { get; } = new();


    private string InitAccount { get; } = "";

    private static string RpcUrl { get; } = "";
    private static readonly List<string> SideRpcUrl = new() { "" };
    private bool isNeedSide = true;

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("AuthorityTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _proxyAccountContract = _proxyContractAddress == ""
            ? new ProxyAccountContract(NodeManager, InitAccount)
            : new ProxyAccountContract(NodeManager, InitAccount, _proxyContractAddress);
        
        if (isNeedSide)
        {
            for (var i = 0; i < SideRpcUrl.Count; i++)
            {
                var sideNodeManager = new NodeManager(SideRpcUrl[i]);
                var sideAuthority = new AuthorityManager(sideNodeManager, InitAccount);
                var sideGenesisContract = GenesisContract.GetGenesisContract(sideNodeManager, InitAccount);
                var sideTokenContract = sideGenesisContract.GetTokenContract();
                var sideProxyAccountContract = _sideProxyContractAddresss[i] == ""
                    ? new ProxyAccountContract(sideNodeManager, InitAccount)
                    : new ProxyAccountContract(sideNodeManager, InitAccount, _sideProxyContractAddresss[i]);

                SideNodeManagers.Add(sideNodeManager);
                SideAuthorities.Add(sideAuthority);
                _sideGenesisContracts.Add(sideGenesisContract);
                _sideTokenContracts.Add(sideTokenContract);
                _sideProxyAccountContracts.Add(sideProxyAccountContract);
            }
        }

        CreateSeedToken();
    }
    
    [TestMethod]
    public void InitializeProxy()
    {
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Initialize, new Empty());
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var admin = _proxyAccountContract.GetAdmin();
        admin.ShouldBe(InitAccount.ConvertAddress());
        foreach (var sideProxyAccountContract in _sideProxyAccountContracts)
        {
            var sideResult = sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Initialize, new Empty());
            sideResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var sideAdmin = sideProxyAccountContract.GetAdmin();
            sideAdmin.ShouldBe(InitAccount.ConvertAddress());
        }
    }

    [TestMethod]
    public void InitializeTest()
    {
        var test = new AgentInterfaceContract(NodeManager, InitAccount, _authortyContract);
        test.ExecuteMethodWithResult(InterfaceMethod.Initialize, _proxyAccountContract.Contract);
        test.ExecuteMethodWithResult(InterfaceMethod.CreateToken, new ManagerCreateTokenInput
        {
            Amount = 1,
            Decimals = 0,
            Owner = InitAccount.ConvertAddress(),
            Issuer = InitAccount.ConvertAddress()
        });
    }

    [TestMethod]
    public void SetContract()
    {
        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var input = new SetProxyAccountContractsInput();
        input.ProxyAccountInfos.Add(new ProxyAccountInfo
        {
            ContractAddress = _proxyAccountContract.Contract,
            ChainId = chainId
        });
        if (_sideProxyAccountContracts.Any())
        {
            foreach (var sideProxyAccountContract in _sideProxyAccountContracts)
            {
                var sideChainId = ChainHelper.ConvertBase58ToChainId(sideProxyAccountContract.NodeManager.GetChainId());
                input.ProxyAccountInfos.Add(new ProxyAccountInfo
                {
                    ContractAddress = sideProxyAccountContract.Contract,
                    ChainId = sideChainId
                });
            }

            foreach (var result in _sideProxyAccountContracts.Select(sideProxyAccountContract =>
                         sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.SetProxyAccountContracts, input)))
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        var mainResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.SetProxyAccountContracts, input);
        mainResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    //2ec2Wac9pbjwe4kXu3HrWDNr9HmsRGeMPoWaXsSpXdcEZzXWx
    //zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg
    //boRRoPggrskXeLKJZ1dkUNja4Eph57QggRtp45yP9HxYaXwdr 
    //2MKuLXnz8JYXc2vnUN99p5Cs9naRGZpWZC2VdofG6Ch5cD7ao6
    //xWCRANHVtdYsvjHLZoHn8bDfSz76CkqVYpXS8JSUxcxHfnaun
    //UMMGRuKzMokW9Xt85rhtKJFb6Ljx7fv8SmvUxTedoLqhCrSJ6
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void CreateVirtualAddressTest(int managerCount)
    {
        var managerList = new List<ManagementAddress>();
        if (managerCount == 1)
        {
            managerList.Add(new ManagementAddress { Address = InitAccount.ConvertAddress() });
        }
        else
        {
            for (var i = 0; i < managerCount; i++)
            {
                var m = NodeManager.NewAccount("12345678");
                managerList.Add(new ManagementAddress { Address = m.ConvertAddress() });
            }
        }
        var createChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
            new AElf.Contracts.ProxyAccountContract.CreateInput
            {
                ManagementAddresses = { managerList }
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated")).NonIndexed;
        var proxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
        proxyAccountCreated.ManagementAddresses.Value.ShouldBe(managerList);
        proxyAccountCreated.CreateChainId.ShouldBe(createChainId);
        Logger.Info(proxyAccountCreated);
        CheckVirtualInfo(proxyAccountCreated, _proxyAccountContract);
    }

    [TestMethod]
    public void CreateVirtualOnSide()
    {
        foreach (var sideProxyAccountContract in _sideProxyAccountContracts)
            CreateVirtualAddressManagerWithVirtual(sideProxyAccountContract);
    }

    //26EiGf4PSZK9Uegokkt2r3oeTE7vymop8RxvvABEFv4Z4Ff6kY
    //2f8uKf6FrhgtACqJQxgsMbYG65uVe3aiscrR9JDdevAWGMsfXz   
    //2YwAuseHPArE9kfXteg7xdvfX9LRhJ6qromageaSWgd57XQW43
    [TestMethod]
    public void CreateVirtualAddressManagerWithVirtual(ProxyAccountContract proxyAccountContract)
    {
        var createChainId = ChainHelper.ConvertBase58ToChainId(proxyAccountContract.NodeManager.GetChainId());
        var virtualManagerAddress = CreateVirtual(proxyAccountContract);
        var managerList = new[] { new ManagementAddress { Address = virtualManagerAddress } };
        var result = proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
            new AElf.Contracts.ProxyAccountContract.CreateInput
            {
                ManagementAddresses = { managerList }
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated")).NonIndexed;
        var proxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
        var virtualAddress = proxyAccountCreated.ProxyAccountAddress;
        var virtualHash = proxyAccountCreated.ProxyAccountHash;
        proxyAccountCreated.ManagementAddresses.Value.ShouldBe(managerList);
        proxyAccountCreated.CreateChainId.ShouldBe(createChainId);

        var getVirtualAddress =
            proxyAccountContract.GetProxyAccountAddress(createChainId, virtualHash);
        getVirtualAddress.ShouldBe(virtualAddress);

        var getAgentInfo = proxyAccountContract.GetProxyAccountByHash(virtualHash);
        var getAgentInfoByAddress = proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        getAgentInfo.ShouldBe(getAgentInfoByAddress);
        getAgentInfo.ManagementAddresses.ShouldBe(managerList);
        getAgentInfo.ProxyAccountHash.ShouldBe(virtualHash);
        getAgentInfo.CreateChainId.ShouldBe(createChainId);
        Logger.Info(proxyAccountCreated);
        Logger.Info(getAgentInfo);
    }

    [TestMethod]
    [DataRow("GYXsU6jpKf5WYtkzKeRwKPS6TFwN4AJV3B9dyYSVvYqqSFo2w")]
    public void ValidationVirtualAddress(string virtualAddress)
    {
        var sideProxyAccountContract = _sideProxyAccountContracts.First();
        var getAgentInfo =
            sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        var input = new ValidateProxyAccountExistsInput()
        {
            ProxyAccountHash = getAgentInfo.ProxyAccountHash,
            CreateChainId = getAgentInfo.CreateChainId,
            ManagementAddresses = { getAgentInfo.ManagementAddresses }
        };
        Logger.Info(getAgentInfo);
        var result = sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ValidateProxyAccountExists, input);
    }

    //{ "managers": { "value": [ "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg" ] },
    //"createChainId": 9992731,
    //"virtualHash": "4db03d60f4c84bf982967f3ae09bae7055c82afbb86981fdfe79b94d69e024d2",
    //"virtualAddress": "2EKw1reedKHtF4WBpbsPcidU6EW6eEtV46JwwsdgZTXvQGakB5" }
    [TestMethod]
    [DataRow("GYXsU6jpKf5WYtkzKeRwKPS6TFwN4AJV3B9dyYSVvYqqSFo2w")]
    public void SideChainCrossCreateTest(string virtualAddress, int sideIndex)
    {
        SideChainCrossCreate(virtualAddress, sideIndex);
    }

    private Address SideChainCrossCreate(string virtualAddress, int sideIndex)
    {
        var crossChainManager = new CrossChainManager(NodeManager, SideNodeManagers[sideIndex], InitAccount);
        var getProxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        var input = new ValidateProxyAccountExistsInput
        {
            ProxyAccountHash = getProxyInfo.ProxyAccountHash,
            CreateChainId = getProxyInfo.CreateChainId,
            ManagementAddresses = { getProxyInfo.ManagementAddresses }
        };
        var rawTx = crossChainManager.FromNoeNodeManager.GenerateRawTransaction(InitAccount,
            _proxyAccountContract.ContractAddress, ProxyMethod.ValidateProxyAccountExists.ToString(),
            input);
        Logger.Info($"Transaction rawTx is: {rawTx}");
        var txId = NodeManager.SendTransaction(rawTx);
        var txResult = NodeManager.CheckTransactionResult(txId);
        var txHeight = txResult.BlockNumber;

        // get transaction info            
        var status = txResult.Status.ConvertTransactionResultStatus();
        status.ShouldBe(TransactionResultStatus.Mined);

        Logger.Info(
            $"Validate VirtualAddress block: {txHeight},\n" +
            $" rawTx: {rawTx}, \n" +
            $"txId:{txId} to chain {crossChainManager.ToChainNodeManager.GetChainId()}");

        crossChainManager.CheckSideChainIndexMainChain(txResult.BlockNumber);

        var merklePath = crossChainManager.GetMerklePath(txHeight, txId, out var root);
        var crossInput = new CrossChainSyncProxyAccountInput
        {
            FromChainId = getProxyInfo.CreateChainId,
            ParentChainHeight = txHeight,
            MerklePath = merklePath,
            TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
        };

        var sideProxyAccountContract = _sideProxyAccountContracts[sideIndex];
        var result =
            sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.CrossChainSyncProxyAccount, crossInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        if (result.Logs.Any(l => l.Name.Equals("ProxyAccountCreated")))
        {
            Logger.Info("Create");
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated"));
            var crossProxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountCreated.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountCreated.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            crossProxyAccountCreated.CreateChainId.ShouldBe(getProxyInfo.CreateChainId);
            Logger.Info(crossProxyAccountCreated);
            CheckVirtualInfo(crossProxyAccountCreated, sideProxyAccountContract);
            return crossProxyAccountCreated.ProxyAccountAddress;
        }
        else
        {
            Logger.Info("Reset");
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountManagersReset"));
            var crossProxyAccountManagersReset =
                ProxyAccountManagementAddressReset.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountManagersReset.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountManagersReset.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            Logger.Info(crossProxyAccountManagersReset);
            return crossProxyAccountManagersReset.ProxyAccountAddress;
        }
    }

    [TestMethod]
    [DataRow("FZ2VVhYCwk5Y8BcVKf8RxfKL1DkdPjmFLoKZQwjyZNzDDGEe7", 0)]
    public void MainChainCrossCreate(string virtualAddress, int sideIndex)
    {
        var crossChainManager = new CrossChainManager(SideNodeManagers[sideIndex], NodeManager, InitAccount);
        var sideProxyAccountContract = _sideProxyAccountContracts[sideIndex];
        var getProxyInfo =
            sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        var input = new ValidateProxyAccountExistsInput
        {
            ProxyAccountHash = getProxyInfo.ProxyAccountHash,
            CreateChainId = getProxyInfo.CreateChainId,
            ManagementAddresses = { getProxyInfo.ManagementAddresses }
        };
        var rawTx = crossChainManager.FromNoeNodeManager.GenerateRawTransaction(InitAccount,
            sideProxyAccountContract.ContractAddress, ProxyMethod.ValidateProxyAccountExists.ToString(),
            input);
        Logger.Info($"Transaction rawTx is: {rawTx}");
        var txId = crossChainManager.FromNoeNodeManager.SendTransaction(rawTx);
        var txResult = crossChainManager.FromNoeNodeManager.CheckTransactionResult(txId);
        var txHeight = txResult.BlockNumber;

        // get transaction info            
        var status = txResult.Status.ConvertTransactionResultStatus();
        status.ShouldBe(TransactionResultStatus.Mined);

        Logger.Info(
            $"Validate VirtualAddress block: {txHeight},\n" +
            $" rawTx: {rawTx}, \n" +
            $"txId:{txId} to chain {crossChainManager.ToChainNodeManager.GetChainId()}");

        crossChainManager.CheckMainChainIndexSideChain(txResult.BlockNumber,
            crossChainManager.ToChainNodeManager,
            crossChainManager.FromNoeNodeManager,
            crossChainManager.ToChainCrossChain,
            crossChainManager.FromChainCrossChain);

        var merklePath = crossChainManager.GetMerklePath(txResult.BlockNumber, txId, out var root);
        var crossInput = new CrossChainSyncProxyAccountInput
        {
            FromChainId = getProxyInfo.CreateChainId,
            MerklePath = merklePath
        };
        // verify side chain transaction
        var crossChainMerkleProofContext =
            crossChainManager.FromChainCrossChain.GetCrossChainMerkleProofContext(txResult.BlockNumber);
        crossInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
            .MerklePathFromParentChain.MerklePathNodes);
        crossInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
        crossInput.TransactionBytes =
            ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.CrossChainSyncProxyAccount, crossInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        if (result.Logs.Any(l => l.Name.Equals("ProxyAccountCreated")))
        {
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated"));
            var crossProxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountCreated.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountCreated.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            crossProxyAccountCreated.CreateChainId.ShouldBe(getProxyInfo.CreateChainId);
            Logger.Info(crossProxyAccountCreated);
            CheckVirtualInfo(crossProxyAccountCreated, _proxyAccountContract);
        }
        else
        {
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountManagersReset"));
            var crossProxyAccountManagersReset =
                ProxyAccountManagementAddressReset.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountManagersReset.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountManagersReset.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            Logger.Info(crossProxyAccountManagersReset);
        }
    }

    // 1 to 2 
    [TestMethod]
    [DataRow("2hNSGix8wAjtKxMfVN1TWaBCGFF5nG74gAAnLApSckjXrn3wjd", 1 , 0)]
    public void SideToSideCreate(string virtualAddress, int fromIndex, int toIndex)
    {
        var sideProxyAccountContract = _sideProxyAccountContracts[fromIndex];
        var toSideProxyAccountContract = _sideProxyAccountContracts[toIndex];
        var crossChainManager =
            new CrossChainManager(SideNodeManagers[fromIndex], SideNodeManagers[toIndex], InitAccount);
        var fromSide2Main = new CrossChainManager(SideNodeManagers[fromIndex], NodeManager, InitAccount);
        
        var getProxyInfo =
            sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        var input = new ValidateProxyAccountExistsInput
        {
            ProxyAccountHash = getProxyInfo.ProxyAccountHash,
            CreateChainId = getProxyInfo.CreateChainId,
            ManagementAddresses = { getProxyInfo.ManagementAddresses }
        };
        var rawTx = crossChainManager.FromNoeNodeManager.GenerateRawTransaction(InitAccount,
            sideProxyAccountContract.ContractAddress, ProxyMethod.ValidateProxyAccountExists.ToString(),
            input);
        Logger.Info($"Transaction rawTx is: {rawTx}");
        var txId = crossChainManager.FromNoeNodeManager.SendTransaction(rawTx);
        var txResult = crossChainManager.FromNoeNodeManager.CheckTransactionResult(txId);
        var txHeight = txResult.BlockNumber;

        // get transaction info            
        var status = txResult.Status.ConvertTransactionResultStatus();
        status.ShouldBe(TransactionResultStatus.Mined);

        Logger.Info(
            $"Validate VirtualAddress block: {txHeight},\n" +
            $" rawTx: {rawTx}, \n" +
            $"txId:{txId} to chain {crossChainManager.ToChainNodeManager.GetChainId()}");

        var mainChainIndexHeight = fromSide2Main.CheckMainChainIndexSideChain(txResult.BlockNumber,
            fromSide2Main.ToChainNodeManager,
            fromSide2Main.FromNoeNodeManager,
            fromSide2Main.ToChainCrossChain,
            fromSide2Main.FromChainCrossChain);
        
        var merklePath = crossChainManager.GetMerklePath(txResult.BlockNumber, txId, out var root);
        var crossInput = new CrossChainSyncProxyAccountInput
        {
            FromChainId = getProxyInfo.CreateChainId,
            MerklePath = merklePath
        };
        // verify side chain transaction
        var crossChainMerkleProofContext =
            crossChainManager.FromChainCrossChain.GetCrossChainMerkleProofContext(txResult.BlockNumber);
        crossInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
            .MerklePathFromParentChain.MerklePathNodes);
        crossInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
        crossInput.TransactionBytes =
            ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

        crossChainManager.CheckSideChainIndexMainChain(mainChainIndexHeight);
        
        var result = toSideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.CrossChainSyncProxyAccount, crossInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        if (result.Logs.Any(l => l.Name.Equals("ProxyAccountCreated")))
        {
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated"));
            var crossProxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountCreated.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountCreated.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            crossProxyAccountCreated.CreateChainId.ShouldBe(getProxyInfo.CreateChainId);
            Logger.Info(crossProxyAccountCreated);
            CheckVirtualInfo(crossProxyAccountCreated, toSideProxyAccountContract);
        }
        else
        {
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountManagementAddressReset"));
            var crossProxyAccountManagersReset =
                ProxyAccountManagementAddressReset.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            crossProxyAccountManagersReset.ManagementAddresses.Value.ShouldBe(getProxyInfo.ManagementAddresses);
            crossProxyAccountManagersReset.ProxyAccountHash.ShouldBe(getProxyInfo.ProxyAccountHash);
            Logger.Info(crossProxyAccountManagersReset);
        }
    }

    #region reset manager

    [TestMethod]
    [DataRow("Add", "9U1CaWmGLbdJJefekedWWS818raJcSQGFcbPeFwTHTmSmUSL7")]
    // [DataRow("Remove", "boRRoPggrskXeLKJZ1dkUNja4Eph57QggRtp45yP9HxYaXwdr")]
    // [DataRow("Reset", "2gCvJVghPEDgycFXXC1W3kRWyBVGfHYTn3RQhUEM6zdTZQPvdu")]
    public void ChangeManager(string type, string virtualAddress)
    {
        var proxy = _sideProxyAccountContracts.First();
        var proxyInfo = proxy.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        Logger.Info(proxyInfo);
        var manger = proxyInfo.ManagementAddresses.First().Address;
        var newProxyInfo = new ProxyAccount();
        CheckBalance(manger.ToBase58(), "side");
        switch (type)
        {
            case "Add":
                var addAddress = NodeManager.NewAccount("12345678");
                proxy.SetAccount(manger.ToBase58());
                var addResult = proxy.ExecuteMethodWithResult(ProxyMethod.AddManagementAddress,
                    new AddManagementAddressInput
                    {
                        ManagementAddress = new ManagementAddress { Address = addAddress.ConvertAddress() },
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                newProxyInfo =
                    proxy.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
                newProxyInfo.ManagementAddresses.ShouldContain(new ManagementAddress
                    { Address = addAddress.ConvertAddress() });
                var addLogs = addResult.Logs.First(l => l.Name.Equals(nameof(ProxyAccountManagementAddressAdded)));
                var add = ProxyAccountManagementAddressAdded.Parser.ParseFrom(
                    ByteString.FromBase64(addLogs.NonIndexed));
                add.ManagementAddress.Address.ShouldBe(addAddress.ConvertAddress());
                add.ProxyAccountAddress.ShouldBe(virtualAddress.ConvertAddress());
                add.ProxyAccountHash.ShouldBe(proxyInfo.ProxyAccountHash);
                break;
            case "Remove":
                var removeAddress = proxyInfo.ManagementAddresses.Last();
                proxy.SetAccount(manger.ToBase58());
                var removeResult = proxy.ExecuteMethodWithResult(ProxyMethod.RemoveManagementAddress,
                    new RemoveManagementAddressInput
                    {
                        ManagementAddress = removeAddress,
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                newProxyInfo =
                    proxy.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
                newProxyInfo.ManagementAddresses.ShouldNotContain(removeAddress);
                var removeLogs =
                    removeResult.Logs.First(l => l.Name.Equals(nameof(ProxyAccountManagementAddressRemoved)));
                var remove =
                    ProxyAccountManagementAddressRemoved.Parser.ParseFrom(ByteString.FromBase64(removeLogs.NonIndexed));
                remove.ManagementAddress.ShouldBe(removeAddress);
                remove.ProxyAccountAddress.ShouldBe(virtualAddress.ConvertAddress());
                remove.ProxyAccountHash.ShouldBe(proxyInfo.ProxyAccountHash);
                break;

            case "Reset":
                var resetAddressList = new List<ManagementAddress>();
                resetAddressList.Add(new ManagementAddress
                    { Address = NodeManager.NewAccount("12345678").ConvertAddress() });
                proxy.SetAccount(manger.ToBase58());
                var resetResult = proxy.ExecuteMethodWithResult(ProxyMethod.ResetManagementAddress,
                    new ResetManagementAddressInput
                    {
                        ManagementAddresses = { resetAddressList },
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                resetResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                newProxyInfo =
                    proxy.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
                newProxyInfo.ManagementAddresses.ShouldNotBeSameAs(proxyInfo.ManagementAddresses);
                var resetLogs = resetResult.Logs.First(l => l.Name.Equals(nameof(ProxyAccountManagementAddressReset)));
                var reset = ProxyAccountManagementAddressReset.Parser.ParseFrom(
                    ByteString.FromBase64(resetLogs.NonIndexed));
                reset.ManagementAddresses.Value.ShouldBe(resetAddressList);
                reset.ProxyAccountAddress.ShouldBe(virtualAddress.ConvertAddress());
                reset.ProxyAccountHash.ShouldBe(proxyInfo.ProxyAccountHash);
                break;
        }

        Logger.Info(newProxyInfo);
    }

    [TestMethod]
    [DataRow("Add", "GYXsU6jpKf5WYtkzKeRwKPS6TFwN4AJV3B9dyYSVvYqqSFo2w")]
    [DataRow("Remove", "GYXsU6jpKf5WYtkzKeRwKPS6TFwN4AJV3B9dyYSVvYqqSFo2w")]
    [DataRow("Reset", "GYXsU6jpKf5WYtkzKeRwKPS6TFwN4AJV3B9dyYSVvYqqSFo2w")]
    public void ChangeManager_NoPermission(string type, string virtualAddress)
    {
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        Logger.Info(proxyInfo);
        var manger = NodeManager.NewAccount("12345678");
        var newProxyInfo = new ProxyAccount();
        CheckBalance(manger, "main");
        switch (type)
        {
            case "Add":
                var addAddress = NodeManager.NewAccount("12345678");
                _proxyAccountContract.SetAccount(manger);
                var addResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.AddManagementAddress,
                    new AddManagementAddressInput
                    {
                        ManagementAddress = new ManagementAddress { Address = addAddress.ConvertAddress() },
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                addResult.Status.ConvertTransactionResultStatus().ShouldNotBe(TransactionResultStatus.Mined);
                addResult.Error.ShouldContain("No permission.");
                break;
            case "Remove":
                var removeAddress = proxyInfo.ManagementAddresses.Last();
                _proxyAccountContract.SetAccount(manger);
                var removeResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.RemoveManagementAddress,
                    new RemoveManagementAddressInput
                    {
                        ManagementAddress = removeAddress,
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                removeResult.Status.ConvertTransactionResultStatus().ShouldNotBe(TransactionResultStatus.Mined);
                removeResult.Error.ShouldContain("No permission.");
                break;

            case "Reset":
                var resetAddressList = new List<ManagementAddress>();
                resetAddressList.Add(new ManagementAddress
                    { Address = NodeManager.NewAccount("12345678").ConvertAddress() });
                resetAddressList.Add(new ManagementAddress { Address = manger.ConvertAddress() });
                _proxyAccountContract.SetAccount(manger);
                var resetResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ResetManagementAddress,
                    new ResetManagementAddressInput
                    {
                        ManagementAddresses = { resetAddressList },
                        ProxyAccountHash = proxyInfo.ProxyAccountHash
                    });
                resetResult.Status.ConvertTransactionResultStatus().ShouldNotBe(TransactionResultStatus.Mined);
                resetResult.Error.ShouldContain("No permission.");
                break;
        }

        Logger.Info(newProxyInfo);
    }


    [TestMethod]
    public void RemoveFailed()
    {
        var virtualAddress = CreateVirtual(_proxyAccountContract);
        var agentInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        Logger.Info(agentInfo);
        var manager = agentInfo.ManagementAddresses.First().Address.ToBase58();
        CheckBalance(manager, "main");
        var removeAddress = InitAccount.ConvertAddress();
        _proxyAccountContract.SetAccount(manager);
        var removeResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.RemoveManagementAddress,
            new RemoveManagementAddressInput
            {
                ManagementAddress = new ManagementAddress { Address = InitAccount.ConvertAddress() },
                ProxyAccountHash = agentInfo.ProxyAccountHash
            });
        removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    #endregion

    #region settings

    //SetAdmin SetMaxManagementAddressCount
    [TestMethod]
    public void SetMaxManagementAddressCount()
    {
        var proxy = _sideProxyAccountContracts.First();
        var maxCount = 2;
        var admin = proxy.GetAdmin();
        proxy.SetAccount(admin.ToBase58());
        CheckBalance(admin.ToBase58(), "side");
        var result = proxy.ExecuteMethodWithResult(ProxyMethod.SetMaxManagementAddressCount,
            new Int32Value { Value = maxCount });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var maxManager = proxy.GetMaxManagementAddressCount();
        maxManager.Value.ShouldBe(maxCount);
    }


    [TestMethod]
    public void SetMaxManagementAddressCount_Failed()
    {
        var admin = _proxyAccountContract.GetAdmin();
        var newAddress = NodeManager.NewAccount("12345678");
        CheckBalance(newAddress, "main");
        _proxyAccountContract.SetAccount(newAddress);
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.SetMaxManagementAddressCount,
            new Int32Value { Value = 5 });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("No permission.");
    }

    #endregion

    #region ForwardCall

    [TestMethod]
    public void ForwardCall()
    {
        var manger = "2YwAuseHPArE9kfXteg7xdvfX9LRhJ6qromageaSWgd57XQW43";
        var virtualAddress = "2f8uKf6FrhgtACqJQxgsMbYG65uVe3aiscrR9JDdevAWGMsfXz";
        var actualVirtualAddress = "26EiGf4PSZK9Uegokkt2r3oeTE7vymop8RxvvABEFv4Z4Ff6kY";
        var toAccount = NodeManager.NewAccount("12345678");
        var agentInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress.ConvertAddress());
        var actualAgentInfo =
            _proxyAccountContract.GetProxyAccountByProxyAccountAddress(actualVirtualAddress.ConvertAddress());

        CheckBalance(manger, "main");
        CheckBalance(actualVirtualAddress, "main");
        var originVirtualBalance = _tokenContract.GetUserBalance(actualVirtualAddress);
        var transferInput = new TransferInput
        {
            To = toAccount.ConvertAddress(),
            Amount = 100000000,
            Symbol = "ELF"
        };

        var inlineInput = new ForwardCallInput
        {
            ContractAddress = _tokenContract.Contract,
            MethodName = nameof(TokenMethod.Transfer),
            ProxyAccountHash = actualAgentInfo.ProxyAccountHash,
            Args = transferInput.ToByteString()
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = _proxyAccountContract.Contract,
            MethodName = nameof(ProxyMethod.ForwardCall),
            ProxyAccountHash = agentInfo.ProxyAccountHash,
            Args = inlineInput.ToByteString()
        };

        _proxyAccountContract.SetAccount(manger);
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var balance = _tokenContract.GetUserBalance(toAccount);
        balance.ShouldBe(100000000);
        var virtualBalance = _tokenContract.GetUserBalance(actualVirtualAddress);
        virtualBalance.ShouldBe(originVirtualBalance.Sub(100000000));
    }

    [TestMethod]
    [DataRow("7S64ARfCUZS9ixmBpn4VKJoAmzZduV8yS6jRvjpudDQWzjcvZ","2SXY7gcyBxfQoJJ9dAHDUoGvaJESdvj4xHSiVd7DEum31YoXVm")]
    public void ForwardCall_SetProfitsReceiver(string proxyAccount, string receiverAccount)
    {
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manger = proxyInfo.ManagementAddresses.First().Address;
        var election = _genesisContract.GetElectionContract(InitAccount);
        var treasury = _genesisContract.GetTreasuryContract(InitAccount);
        var profit = _genesisContract.GetProfitContract(InitAccount);
        var backupSchemeId = Hash.LoadFromHex("05126285e477dd931cb08a810558543c85d73c0a089d45099cf9300395a2ed04");
        var candidates =
            election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                new Empty());
        var pubkey = candidates.Value.First().ToByteArray().ToHex();
        var admin = election.GetCandidateAdmin(pubkey);
        admin.ShouldBe(proxyAccount.ConvertAddress());
        var originReceive = treasury.GetProfitReceiver(pubkey);

        var setProfitReceiverInput = new AElf.Contracts.Treasury.SetProfitsReceiverInput
        {
            Pubkey = pubkey,
            ProfitsReceiverAddress = Address.FromBase58(receiverAccount)
        };
        
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = treasury.Contract,
            MethodName = nameof(TreasuryMethod.SetProfitsReceiver),
            ProxyAccountHash = proxyInfo.ProxyAccountHash,
            Args = setProfitReceiverInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manger.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var afterReceiver = treasury.GetProfitReceiver(pubkey);
        afterReceiver.ShouldBe(receiverAccount.ConvertAddress());
        
        var receiverProfit = profit.GetProfitDetails(receiverAccount, backupSchemeId);
        var afterSetProfit = profit.GetProfitDetails(originReceive.ToBase58(), backupSchemeId);
        Logger.Info(afterSetProfit.Details);
        Logger.Info(receiverProfit.Details);
    }
    
    [TestMethod]
    [DataRow("sv8RfjBcBavquLgvf82nKRNgEi4xkYmDktHWADBEzVmdDybaw")]
    public void ForwardCall_Replace(string proxyAccount)
    {
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manger = proxyInfo.ManagementAddresses.First().Address;
        CheckBalance(manger.ToBase58(), "main");
        var election = _genesisContract.GetElectionContract(InitAccount);
        var candidates =
            election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                new Empty());
        var pubkey = candidates.Value.First().ToByteArray().ToHex();
        var admin = election.GetCandidateAdmin(pubkey);
        admin.ShouldBe(proxyAccount.ConvertAddress());

        var replaceAccount = NodeManager.NewAccount("12345678");
        var newPubkey = NodeManager.AccountManager.GetPublicKey(replaceAccount);
        var replaceInput = new ReplaceCandidatePubkeyInput
        {
            NewPubkey = newPubkey,
            OldPubkey = pubkey
        };
        
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = election.Contract,
            MethodName = nameof(ElectionMethod.ReplaceCandidatePubkey),
            ProxyAccountHash = proxyInfo.ProxyAccountHash,
            Args = replaceInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manger.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        
        candidates =
            election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                new Empty());
        var afterPubkey = candidates.Value.First().ToByteArray().ToHex();
        afterPubkey.ShouldBe(newPubkey);
    }
    
    [TestMethod]
    [DataRow("sv8RfjBcBavquLgvf82nKRNgEi4xkYmDktHWADBEzVmdDybaw")]
    public void ForwardCall_Quit(string proxyAccount)
    {
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manger = proxyInfo.ManagementAddresses.First().Address;
        CheckBalance(manger.ToBase58(), "main");
        var election = _genesisContract.GetElectionContract(InitAccount);
        var candidates =
            election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                new Empty());
        var pubkey = candidates.Value.First().ToByteArray().ToHex();
        var admin = election.GetCandidateAdmin(pubkey);
        admin.ShouldBe(proxyAccount.ConvertAddress());
        
        var quitInput =new StringValue { Value = pubkey };
        
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = election.Contract,
            MethodName = nameof(ElectionMethod.QuitElection),
            ProxyAccountHash = proxyInfo.ProxyAccountHash,
            Args = quitInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manger.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        
        candidates =
            election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                new Empty());
        candidates.ShouldBe(new PubkeyList());
    }

    [TestMethod]
    [DataRow("2SXY7gcyBxfQoJJ9dAHDUoGvaJESdvj4xHSiVd7DEum31YoXVm")]
    public void ForwardCall_ClaimProfits( string receiverAccount)
    {
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(receiverAccount.ConvertAddress());
        var manger = proxyInfo.ManagementAddresses.First().Address;
        CheckBalance(manger.ToBase58(), "main");
        var profit = _genesisContract.GetProfitContract(InitAccount);
        var backupSchemeId = Hash.LoadFromHex("05126285e477dd931cb08a810558543c85d73c0a089d45099cf9300395a2ed04");
        var profitMap = profit.GetProfitsMap(receiverAccount, backupSchemeId);
        var profitAmountFull = profitMap.Value["ELF"];
        Logger.Info($"profit amount: {profitAmountFull}");
        var beforeBalance = _tokenContract.GetUserBalance(receiverAccount);
        var input = new ClaimProfitsInput
        {
            SchemeId = backupSchemeId,
            Beneficiary = receiverAccount.ConvertAddress()
        };
        
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = profit.Contract,
            MethodName = nameof(ProfitMethod.ClaimProfits),
            ProxyAccountHash = proxyInfo.ProxyAccountHash,
            Args = input.ToByteString()
        };
        
        _proxyAccountContract.SetAccount(manger.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        
        var afterBalance = _tokenContract.GetUserBalance(receiverAccount);
        afterBalance.ShouldBe(beforeBalance.Add(profitAmountFull));
        
        var receiverProfit = profit.GetProfitDetails(receiverAccount, backupSchemeId);
        Logger.Info(receiverProfit.Details);

    }


    [TestMethod]
    [DataRow("Portkey.Contracts.CA-1.3.0", 
        "2EdMJxNKvWxaNZbykKpUZygWLJafDzRdbtba96BRsSaBLeRZmt", "side")]
    public void ForwardCall_Deploy(string contractFileName, string proxyAccount, string type)
    {
        var genesis = type == "main" ? _genesisContract : _sideGenesisContracts.First();
        var proxy = type == "main" ? _proxyAccountContract : _sideProxyAccountContracts.First();
        var proxyAccountInfo = proxy.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        var contractReader = new SmartContractReader();
        var codeArray = contractReader.Read(contractFileName);
        
        var deployInput = new ContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Category = 0
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = genesis.Contract,
            MethodName = nameof(GenesisMethod.DeployUserSmartContract),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = deployInput.ToByteString()
        };

        CheckBalance(manager.ToBase58(), type);
        proxy.SetAccount(manager.ToBase58());
        
        var txResult = proxy.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        txResult.Status.ConvertTransactionResultStatus().ShouldBe(
            type == "main" 
                ? TransactionResultStatus.Failed
                : TransactionResultStatus.Mined);

        if (!txResult.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined)) return;
        var logEvent = txResult.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
        var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
        codeCheckRequired.Category.ShouldBe(0);
        codeCheckRequired.IsSystemContract.ShouldBeFalse();
        codeCheckRequired.IsUserContract.ShouldBeTrue();
        var proposalLogEvent = txResult.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        var codeHash = HashHelper.ComputeFrom(codeCheckRequired.Code.ToByteArray());

        Logger.Info(
            $"Code hash: {codeHash}\n" +
            $"ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}\n " +
            $"Proposal Id: {proposalId.ToHex()}");

        // var check = CheckProposal(proposalId);
        // check.ShouldBeTrue();
        Thread.Sleep(60000);

        var currentHeight = AsyncHelper.RunSync(genesis.NodeManager.ApiClient.GetBlockHeightAsync);
        var smartContractRegistration = genesis.GetSmartContractRegistrationByCodeHash(codeHash);
        smartContractRegistration.ShouldNotBeNull();
        Logger.Info($"Check height: {txResult.BlockNumber} - {currentHeight}");

        var release = genesis.FindReleaseApprovedUserSmartContractMethod(txResult.BlockNumber, currentHeight);
        Logger.Info(release.TransactionId);

        var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(ContractDeployed)));
        var indexed = releaseLogEvent.Indexed;
        var nonIndexed = releaseLogEvent.NonIndexed;
        foreach (var i in indexed)
        {
            var contractDeployedIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(i));
            Logger.Info(contractDeployedIndexed.Author == null
                ? $"Code hash: {contractDeployedIndexed.CodeHash.ToHex()}"
                : $"Author: {contractDeployedIndexed.Author}");
        }

        var contractDeployedNonIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        Logger.Info($"Address: {contractDeployedNonIndexed.Address}\n" +
                    $"{contractDeployedNonIndexed.Name}\n" +
                    $"{contractDeployedNonIndexed.Version}\n" +
                    $"{contractDeployedNonIndexed.ContractVersion}\n" +
                    $"Height: {release.BlockNumber}");
    }
    
    [TestMethod]
    [DataRow("AElf.Contracts.ProxyAccountContract-1.4.0", 
        "2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n", 
        "2EdMJxNKvWxaNZbykKpUZygWLJafDzRdbtba96BRsSaBLeRZmt", "side")]
    public void ForwardCall_Update(string contractFileName, string contractAddress, string proxyAccount, string type)
    {
        var genesis = type == "main" ? _genesisContract : _sideGenesisContracts.First();
        var proxy = type == "main" ? _proxyAccountContract : _sideProxyAccountContracts.First();
        var author = genesis.GetContractAuthor(Address.FromBase58(contractAddress));
        author.ShouldBe(proxyAccount.ConvertAddress());
        
        var proxyAccountInfo = proxy.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        var contractReader = new SmartContractReader();
        var codeArray = contractReader.Read(contractFileName);

        var updateInput = new ContractUpdateInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Address = Address.FromBase58(contractAddress)
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = genesis.Contract,
            MethodName = nameof(GenesisMethod.UpdateUserSmartContract),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = updateInput.ToByteString()
        };

        CheckBalance(manager.ToBase58(), type);
        proxy.SetAccount(manager.ToBase58());
        
        var txResult = proxy.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        txResult.Status.ConvertTransactionResultStatus().ShouldBe(
            type == "main" 
                ? TransactionResultStatus.Failed
                : TransactionResultStatus.Mined);

        if (!txResult.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined)) return;
        var logEvent = txResult.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
            var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
            codeCheckRequired.Category.ShouldBe(0);
            codeCheckRequired.IsSystemContract.ShouldBeFalse();
            codeCheckRequired.IsUserContract.ShouldBeTrue();
            var proposalLogEvent = txResult.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;

            Logger.Info(
                $"ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}");

            // var check = CheckProposal(proposalId);
            // check.ShouldBeTrue();
            Thread.Sleep(20000);

            var currentHeight = AsyncHelper.RunSync(genesis.NodeManager.ApiClient.GetBlockHeightAsync);

            var release = genesis.FindReleaseApprovedUserSmartContractMethod(txResult.BlockNumber, currentHeight);
            Logger.Info(release.TransactionId);

            var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
            var indexed = releaseLogEvent.Indexed;
            var nonIndexed = releaseLogEvent.NonIndexed;
            var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
            Logger.Info($"Address: {codeUpdatedIndexed.Address}");

            var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
            Logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                        $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                        $"{codeUpdatedNonIndexed.Version}\n" +
                        $"{codeUpdatedNonIndexed.ContractVersion}");

            var smartContractRegistration =
                genesis.GetSmartContractRegistrationByCodeHash(codeUpdatedNonIndexed.NewCodeHash);
            smartContractRegistration.ShouldNotBeNull();
            var contractInfo = genesis.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                contractAddress.ConvertAddress());
            Logger.Info(contractInfo);

            contractInfo.CodeHash.ShouldBe(codeUpdatedNonIndexed.NewCodeHash);
            contractInfo.Version.ShouldBe(codeUpdatedNonIndexed.Version);
            contractInfo.ContractVersion.ShouldBe(codeUpdatedNonIndexed.ContractVersion);
    }
    
    [TestMethod]
    [DataRow("EBridge.Contracts.Regiment", 
        "2EdMJxNKvWxaNZbykKpUZygWLJafDzRdbtba96BRsSaBLeRZmt")]
    public void ForwardCall_ProposalNew(string contractFileName, string proxyAccount)
    {
        var genesis =  _sideGenesisContracts.First();
        var proxy = _sideProxyAccountContracts.First();
        var proxyAccountInfo = proxy.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        var contractReader = new SmartContractReader();
        var codeArray = contractReader.Read(contractFileName);
        
        var deployInput = new ContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Category = 0
        };
        
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = genesis.Contract,
            MethodName = nameof(GenesisMethod.ProposeNewContract),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = deployInput.ToByteString()
        };
        
        CheckBalance(manager.ToBase58(), "side");
        proxy.SetAccount(manager.ToBase58());
        
        var txResult = proxy.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        
        var proposalId = ProposalCreated.Parser
            .ParseFrom(ByteString.FromBase64(txResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)).ProposalId;
        var proposalHash = ContractProposed.Parser
            .ParseFrom(ByteString.FromBase64(txResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed))
            .ProposedContractInputHash;
        Logger.Info(
            $"ProposalInput: {proposalHash.ToHex()}\n " +
            $"Proposal Id: {proposalId.ToHex()}");

        // var proposalId = Hash.LoadFromHex("702730c74b26aa36997cc88b8e55ddb97febb9bdb6406ccfceaec197e1fe3005");
        // var proposalHash = Hash.LoadFromHex("3fb6843088f430b2a49af47b868ac6099f1f38972d82330f6d2edec646e4a214");
        
        var releaseInput = new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = proposalHash
        };

        var parliament = genesis.GetParliamentContract(InitAccount);
        var miners = SideAuthorities.First().GetMinApproveMiners();
        parliament.MinersApproveProposal(proposalId, miners);
        
        var releaseApproveForwardCall = new ForwardCallInput
        {
            ContractAddress = genesis.Contract,
            MethodName = nameof(GenesisMethod.ReleaseApprovedContract),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = releaseInput.ToByteString()
        };
        proxy.SetAccount(manager.ToBase58());
        
        var releaseApproveResult = proxy.ExecuteMethodWithResult(ProxyMethod.ForwardCall, releaseApproveForwardCall);
        releaseApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var byteString =
            ByteString.FromBase64(releaseApproveResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
        var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

        Logger.Info($"{deployProposal}\n {proposalHash}");
        
        Thread.Sleep(20000);
        
        var releaseCodeCheckInput = new ReleaseContractInput
        {
            ProposedContractInputHash = proposalHash,
            ProposalId = deployProposal
        };
        
        var releaseCodeCheckForwardCall = new ForwardCallInput
        {
            ContractAddress = genesis.Contract,
            MethodName = nameof(GenesisMethod.ReleaseCodeCheckedContract),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = releaseCodeCheckInput.ToByteString()
        };

        var releaseCodeCheckResult = proxy.ExecuteMethodWithResult(ProxyMethod.ForwardCall, releaseCodeCheckForwardCall);
        releaseApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var codeCheckByteString =
            ByteString.FromBase64(releaseCodeCheckResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
        var byteStringIndexed =
            ByteString.FromBase64(
                releaseCodeCheckResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed.First());
        var contractDeployed = ContractDeployed.Parser.ParseFrom(codeCheckByteString);
        var deployAddress = contractDeployed.Address;
        var contractVersion = contractDeployed.ContractVersion;
        var author = ContractDeployed.Parser.ParseFrom(byteStringIndexed).Author;
        Logger.Info($"{deployAddress}, {author}, {releaseCodeCheckResult.BlockNumber}");

        var contractInfo =
            genesis.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                deployAddress);
        Logger.Info(contractInfo);
        contractInfo.ContractVersion.ShouldBe(contractVersion);
    }
    
    
    [TestMethod]
    [DataRow("2cqVet8FLEVs3XyfvmURcBFXUBsZPZqD8GSY29dsvKuDbpxbnd")]
    public void ForwardCall_Vote(string proxyAccount)
    {
        var proxyAccountInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        CheckBalance(manager.ToBase58(), "main");
        var symbol = "ELF";
        var minValue = new Timestamp {Nanos = 0, Seconds = -62135596800L};
        var maxValue = new Timestamp {Nanos = 999999999, Seconds = 253402300799L};
        VotingRegisterInput votingRegisterInput = new VotingRegisterInput
        {
            IsLockToken = true,
            AcceptedCurrency = symbol,
            TotalSnapshotNumber = long.MaxValue,
            StartTimestamp = minValue,
            EndTimestamp = maxValue,
        };
        _tokenContract.TransferBalance(InitAccount, proxyAccount, 1000_000000000, symbol);
        var beforeVote = _tokenContract.GetUserBalance(proxyAccount, symbol);
        var votingItemId = HashHelper.ConcatAndCompute(
            HashHelper.ComputeFrom(votingRegisterInput),
            HashHelper.ComputeFrom(InitAccount.ConvertAddress()));
        var voteInput = new VoteInput
        {
            Voter = proxyAccount.ConvertAddress(),
            VoteId = HashHelper.ComputeFrom("VOTE"),
            Amount = 1000,
            VotingItemId = votingItemId,
            Option = "Vote",
            IsChangeTarget = true
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = _genesisContract.GetVoteContract(InitAccount).Contract,
            MethodName = nameof(VoteMethod.Vote),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = voteInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manager.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var log = result.Logs.First(l => l.Name.Contains(nameof(Voted))).NonIndexed;
        var votedInfo = Voted.Parser.ParseFrom(ByteString.FromBase64(log));
        votedInfo.Amount.ShouldBe(1000);
        Logger.Info(votedInfo);

        var afterVote = _tokenContract.GetUserBalance(proxyAccount, "ELF");
        afterVote.ShouldBe(beforeVote - 1000);
    }
    
    [TestMethod]
    [DataRow("2cqVet8FLEVs3XyfvmURcBFXUBsZPZqD8GSY29dsvKuDbpxbnd","8043c001dddf3411f3f779bad6dc2d33ee3a372249813e34e829f026d1e5b692")]
    public void ForwardCall_Withdraw(string proxyAccount, string voteId)
    {
        var vote = _genesisContract.GetVoteContract(InitAccount);
        var proxyAccountInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        CheckBalance(manager.ToBase58(), "main");
        var symbol = "ELF";
        var beforeWithdraw = _tokenContract.GetUserBalance(proxyAccount, symbol);

        var voteInfo = vote.CallViewMethod<VotingRecord>(VoteMethod.GetVotingRecord, Hash.LoadFromHex(voteId));
        var amount = voteInfo.Amount;
        voteInfo.Voter.ShouldBe(proxyAccount.ConvertAddress());
        voteInfo.IsWithdrawn.ShouldBeFalse();
            
        var input = new WithdrawInput
        {
            VoteId = Hash.LoadFromHex(voteId)
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = vote.Contract,
            MethodName = nameof(VoteMethod.Withdraw),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = input.ToByteString()
        };
        
        _proxyAccountContract.SetAccount(manager.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var withdrawLog = result.Logs.First(l => l.Name.Equals("Withdrawn")).NonIndexed;
        var withdrawn = Withdrawn.Parser.ParseFrom(ByteString.FromBase64(withdrawLog));
        Logger.Info(withdrawn);
        var afterBalance = _tokenContract.GetUserBalance(proxyAccount, symbol);
        beforeWithdraw.ShouldBe(afterBalance - amount);
            
        voteInfo = vote.CallViewMethod<VotingRecord>(VoteMethod.GetVotingRecord, Hash.LoadFromHex(voteId));
        voteInfo.IsWithdrawn.ShouldBeTrue();
    }
    
    
    [TestMethod]
    [DataRow("aNiD8d3vMtGpEdiLZ14i8fkpddgwyYyf4CNnMLyTTB1PFiLjn")]
    public void ForwardCall_ElectionVote(string proxyAccount)
    {
        var proxyAccountInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount.ConvertAddress());
        var manager = proxyAccountInfo.ManagementAddresses.First().Address;
        CheckBalance(manager.ToBase58(), "main");
        var election = _genesisContract.GetElectionContract();
        var candidates = election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
            new Empty());
        var candidatePubkey = candidates.Value.First().ToByteArray().ToHex();
        var voteAmount = 100000000;
        _tokenContract.TransferBalance(manager.ToBase58(), proxyAccount, voteAmount);

        var voteInput = new VoteMinerInput
        {
            CandidatePubkey = candidatePubkey,
            Amount = voteAmount,
            EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(91)).ToTimestamp()
        };

        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = election.Contract,
            MethodName = nameof(ElectionMethod.Vote),
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = voteInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manager.ToBase58());
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var log = result.Logs.First(l => l.Name.Contains(nameof(Voted))).NonIndexed;
        var votedInfo = Voted.Parser.ParseFrom(ByteString.FromBase64(log));
        Logger.Info(votedInfo);
    }


    #endregion

    #region EOA CreateToken

    [TestMethod]
    [DataRow("NFTTEST-1", "NFT-item")]
    public void CreateAndIssueToken(string symbol, string type)
    {
        var issuer = NodeManager.NewAccount("12345678");
        var owner = NodeManager.NewAccount("12345678");
        switch (type)
        {
            case "FT":
                _tokenContract.CheckToken(symbol, issuer, InitAccount); 
                var tokenInfo = _tokenContract.GetTokenInfo(symbol);
                CheckBalance(issuer, "main");
                _tokenContract.SetAccount(issuer);
                var result = _tokenContract.IssueBalance(issuer, InitAccount, 10000000, symbol);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
                balance.ShouldBe(10000000);
                Logger.Info(tokenInfo);
                break;
            case "NFT":
                _tokenContract.CheckToken(symbol, issuer, owner);
                var collectionInfo = _tokenContract.GetTokenInfo(symbol);
                CheckBalance(owner, "main");
                CheckBalance(issuer, "main");
                var item = symbol.Split("-").First() + "-" + 1;
                _tokenContract.SetAccount(owner);
                var createItem = _tokenContract.CreateToken(issuer, owner, 1000, item, 0);
                createItem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var itemInfo = _tokenContract.GetTokenInfo(item);
                _tokenContract.SetAccount(issuer);
                var issueItem = _tokenContract.IssueBalance(issuer, issuer, 1, item);
                issueItem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                balance = _tokenContract.GetUserBalance(issuer, item);
                balance.ShouldBe(1);
                Logger.Info(collectionInfo);
                Logger.Info(itemInfo);
                break;
            case "NFT-item":
                var collection = symbol.Split("-").First() + "-" + 0;
                collectionInfo = _tokenContract.GetTokenInfo(collection);
                owner = collectionInfo.Owner.ToBase58();
                issuer = collectionInfo.Issuer.ToBase58();
                _tokenContract.SetAccount(owner);
                createItem = _tokenContract.CreateToken(issuer, owner, 1000, symbol, 0);
                createItem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                itemInfo = _tokenContract.GetTokenInfo(symbol);
                _tokenContract.SetAccount(issuer);
                issueItem = _tokenContract.IssueBalance(issuer, issuer, 1, symbol);
                issueItem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                balance = _tokenContract.GetUserBalance(issuer, symbol);
                balance.ShouldBe(1);
                Logger.Info(collectionInfo);
                Logger.Info(itemInfo);
                break;
        }
    }

    [TestMethod]
    public void SetOwnerAndIssueProxy()
    {
        var collection = "HHH-0";
        var virtualAddress = CreateVirtual(_proxyAccountContract);
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        Logger.Info(proxyInfo);
        var manager = proxyInfo.ManagementAddresses.First().Address.ToBase58();
        CheckBalance(manager, "main");
        Logger.Info($"Create NFT-Collection {collection}");
        _tokenContract.CheckToken(collection, InitAccount, virtualAddress.ToBase58());
        var tokenInfo = _tokenContract.GetTokenInfo(collection);
        Logger.Info(tokenInfo);
        // Create -item
        Logger.Info("Add manager");
        _proxyAccountContract.SetAccount(manager);
        ChangeManager("Add", virtualAddress.ToBase58());
        proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        Logger.Info(proxyInfo);
        var item = collection.Split("-").First();
        var i = 1;
        foreach (var m in proxyInfo.ManagementAddresses)
        {
            CheckBalance(m.Address.ToBase58(), "main");
            var itemSymbol = item + "-" + i;
            Logger.Info($"manager {m.Address.ToBase58()} create {itemSymbol}");

            var createInput = new AElf.Contracts.MultiToken.CreateInput
            {
                Symbol = itemSymbol,
                TokenName = $"{itemSymbol}",
                TotalSupply = 2,
                Decimals = 0,
                Issuer = virtualAddress,
                Owner = virtualAddress
            };
            var forwardCallInput = new ForwardCallInput
            {
                ProxyAccountHash = proxyInfo.ProxyAccountHash,
                ContractAddress = _tokenContract.Contract,
                MethodName = nameof(TokenMethod.Create),
                Args = createInput.ToByteString()
            };
            _proxyAccountContract.SetAccount(m.Address.ToBase58());
            var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, forwardCallInput);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var itemInfo = _tokenContract.GetTokenInfo(itemSymbol);
            Logger.Info(itemInfo);
            itemInfo.Issuer.ShouldBe(virtualAddress);
            itemInfo.Owner.ShouldBe(virtualAddress);

            foreach (var pm in proxyInfo.ManagementAddresses)
            {
                Logger.Info($"manager {pm.Address.ToBase58()} issue {itemSymbol}");
                CheckBalance(pm.Address.ToBase58(), "main");
                var issueInput = new IssueInput
                {
                    Symbol = itemSymbol,
                    To = pm.Address,
                    Amount = 1
                };
                var issueForwardCallInput = new ForwardCallInput
                {
                    ProxyAccountHash = proxyInfo.ProxyAccountHash,
                    ContractAddress = _tokenContract.Contract,
                    MethodName = nameof(TokenMethod.Issue),
                    Args = issueInput.ToByteString()
                };

                _proxyAccountContract.SetAccount(pm.Address.ToBase58());
                var issueResult =
                    _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, issueForwardCallInput);
                issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = _tokenContract.GetUserBalance(pm.Address.ToBase58(), itemSymbol);
                balance.ShouldBe(1);
            }

            i++;
        }
    }

    [TestMethod]
    [DataRow("EEE")]
    public void SetOwnerAndIssueProxyOnSideChain(string nft, int sideIndex)
    {
        var main2Side = new CrossChainManager(NodeManager, SideNodeManagers[sideIndex], InitAccount);
        var sideProxyAccountContract = _sideProxyAccountContracts[sideIndex];
        var sideTokenContract = _sideTokenContracts[sideIndex];
        var collection = $"{nft}-0";
        var sideChainId = ChainHelper.ConvertBase58ToChainId(sideProxyAccountContract.NodeManager.GetChainId());

        Logger.Info("Create fake ca account on main chain");
        var fakeCaAddress = CreateVirtual(_proxyAccountContract);
        var caInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(fakeCaAddress);
        Logger.Info(caInfo);
        var manager = caInfo.ManagementAddresses.First().Address.ToBase58();
        Logger.Info("Cross create ca account on side chain");
        var sideFakeCaAddress = SideChainCrossCreate(fakeCaAddress.ToBase58(), sideIndex);
        var sideCaInfo = sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(sideFakeCaAddress);
        Logger.Info(sideCaInfo);

        Logger.Info("Ca address create proxyAddress");
        var proxyManager = new[] { fakeCaAddress.ToBase58(), sideFakeCaAddress.ToBase58() };
        var caProxyAccount = CreateVirtual(_proxyAccountContract, proxyManager.ToList());
        var caProxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(caProxyAccount);
        Logger.Info(caProxyInfo);
        Logger.Info("Ca address cross create proxyAddress");
        var sideCaProxyAccount = SideChainCrossCreate(caProxyAccount.ToBase58(), sideIndex);
        var sideCaProxyInfo = sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(sideCaProxyAccount);
        Logger.Info(sideCaProxyInfo);

        CheckBalance(manager, "main");
        Logger.Info($"Create NFT-Collection {collection}");
        _tokenContract.CheckToken(collection, InitAccount, fakeCaAddress.ToBase58(), sideChainId);
        var tokenInfo = _tokenContract.GetTokenInfo(collection);
        Logger.Info(tokenInfo);

        Logger.Info("Cross Create collection");
        var validationTokenResult = main2Side.ValidateTokenSymbol(collection, out var raw);
        var crossChainCreate = main2Side.CrossChainCreate(validationTokenResult, raw);
        crossChainCreate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        // Create -item
        var item = collection.Split("-").First();
        var i = 1;
        CheckBalance(manager, "main");
        var itemSymbol = item + "-" + i;
        Logger.Info($"manager {manager} through \n" +
                    $"ca address {fakeCaAddress} create {itemSymbol}");
        var createInput = new AElf.Contracts.MultiToken.CreateInput
        {
            Symbol = itemSymbol,
            TokenName = $"{itemSymbol}",
            TotalSupply = 2,
            Decimals = 0,
            IssueChainId = sideChainId,
            Issuer = sideCaProxyAccount,
            Owner = fakeCaAddress
        };
        var forwardCallInput = new ForwardCallInput
        {
            ProxyAccountHash = caInfo.ProxyAccountHash,
            ContractAddress = _tokenContract.Contract,
            MethodName = nameof(TokenMethod.Create),
            Args = createInput.ToByteString()
        };
        _proxyAccountContract.SetAccount(manager);
        var createResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, forwardCallInput);
        createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var itemInfo = _tokenContract.GetTokenInfo(itemSymbol);
        Logger.Info(itemInfo);

        Logger.Info("Cross Create item");
        var validationItemResult = main2Side.ValidateTokenSymbol(itemSymbol, out var itemRaw);
        var crossChainCreateItem = main2Side.CrossChainCreate(validationItemResult, itemRaw);
        crossChainCreateItem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        Logger.Info("ca address through proxy issue item on side chain");
        var issueInput = new IssueInput
        {
            Symbol = itemSymbol,
            Amount = 1,
            To = sideFakeCaAddress
        };

        var proxyForwardCallInput = new ForwardCallInput
        {
            ProxyAccountHash = sideCaProxyInfo.ProxyAccountHash,
            ContractAddress = sideTokenContract.Contract,
            MethodName = nameof(TokenMethod.Issue),
            Args = issueInput.ToByteString()
        };
        var caForwardCallInput = new ForwardCallInput
        {
            ProxyAccountHash = sideCaInfo.ProxyAccountHash,
            ContractAddress = sideProxyAccountContract.Contract,
            MethodName = nameof(ProxyMethod.ForwardCall),
            Args = proxyForwardCallInput.ToByteString()
        };
        CheckBalance(manager, "side");
        sideProxyAccountContract.SetAccount(manager);
        var result = sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall, caForwardCallInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var balance = sideTokenContract.GetUserBalance(sideFakeCaAddress.ToBase58(), itemSymbol);
        balance.ShouldBe(1);
    }

    [TestMethod]
    [DataRow("NFTTEST-0", "NFTTEST-1")]
    public void CreateTokenAndIssueTokenOnSide(string symbol, string itemSymbol)
    {
        var info = _tokenContract.GetTokenInfo(symbol);
        var owner = info.Owner ?? info.Issuer;
        var issuer = NodeManager.NewAccount("12345678");
        var issueChainId = info.IssueChainId;
        CheckBalance(owner.ToBase58(),"main");
        CheckBalance(issuer,"side");

        _tokenContract.SetAccount(owner.ToBase58());
        _tokenContract.CreateToken(issuer, owner.ToBase58(), 1000, itemSymbol, 0, true, issueChainId);
        Logger.Info(_tokenContract.GetTokenInfo(itemSymbol));

        Logger.Info("Cross Create item");
        var crossChainManager = new CrossChainManager(NodeManager, SideNodeManagers.First(), InitAccount);
        var result = crossChainManager.ValidateTokenSymbol(itemSymbol, out var raw);
        var crossChainCreate = crossChainManager.CrossChainCreate(result, raw);
        crossChainCreate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var sideToken = _sideTokenContracts.First();
        var sideTokenInfo = sideToken.GetTokenInfo(itemSymbol);
        var sideIssuer = sideTokenInfo.Issuer;
        Logger.Info(sideTokenInfo);
        
        Logger.Info("issue item on side");
        sideToken.SetAccount(sideIssuer.ToBase58());
        sideToken.IssueBalance(sideIssuer.ToBase58(), sideIssuer.ToBase58(), 1, itemSymbol);
        var balance = sideToken.GetUserBalance(sideIssuer.ToBase58(), itemSymbol);
        Logger.Info(balance);
    }


    
    [TestMethod]
    [DataRow("SEED-1")]
    [DataRow("SEED-2")]
    [DataRow("SEED-3")]
    [DataRow("SEED-4")]
    [DataRow("TEST")]
    [DataRow("ABC")]
    [DataRow("NFT-0")]
    [DataRow("NFTTEST-0")]
    public void GetTokenInfo(string symbol)
    {
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info(tokenInfo);
        
        var sideTokenInfo = _sideTokenContracts.First().GetTokenInfo(symbol);
        Logger.Info(sideTokenInfo);
    }

    #endregion

    [TestMethod]
    [DataRow("0810837775ba9816d82a077aef5437b6617414f4d73eb7837d710ca42c1b4ed7", "ProxyAccountCreated")]
    public void CheckLogs(string txId, string logName)
    {
        var txResult = NodeManager.CheckTransactionResult(txId);
        var logs = txResult.Logs.First(l => l.Name.Equals(logName)).NonIndexed;
        var created = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
        Logger.Info(created);

        var createChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var virtualHash = created.ProxyAccountHash;
        var virtualAddress = created.ProxyAccountAddress;
        var getVirtualAddress =
            _proxyAccountContract.GetProxyAccountAddress(createChainId, virtualHash);
        getVirtualAddress.ShouldBe(virtualAddress);
    }

    [TestMethod]
    [DataRow("2hNSGix8wAjtKxMfVN1TWaBCGFF5nG74gAAnLApSckjXrn3wjd","9U1CaWmGLbdJJefekedWWS818raJcSQGFcbPeFwTHTmSmUSL7","1","0")]
    public void GetProxyInfoTest(string originVirtual, string checkVirtual, string originChain, string checkChain)
    {
        var originProxy = originChain == ""
            ? _proxyAccountContract
            : _sideProxyAccountContracts[int.Parse(originChain)];
        var checkProxy = checkChain == ""
            ? _proxyAccountContract
            : _sideProxyAccountContracts[int.Parse(checkChain)];
        var proxyOriginInfo = GetProxyAccountInfo(originProxy, originVirtual.ConvertAddress());
        var proxyCheckInfo = GetProxyAccountInfo(checkProxy, checkVirtual.ConvertAddress());
        Logger.Info(proxyOriginInfo);
        Logger.Info(proxyCheckInfo);
        proxyOriginInfo.ManagementAddresses.ShouldBe(proxyCheckInfo.ManagementAddresses);
        proxyOriginInfo.ProxyAccountHash.ShouldBe(proxyCheckInfo.ProxyAccountHash);
    }

    private ProxyAccount GetProxyAccountInfo(ProxyAccountContract proxyAccountContract, Address virtualAddress,
        Hash virtualHash = null)
    {
        var agent = virtualAddress == new Address()
            ? proxyAccountContract.GetProxyAccountByHash(virtualHash)
            : proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        return agent;
    }

    private void CreateSeedToken()
    {
        _tokenContract.CreateSEED0Token();
    }

    private Address CreateVirtual(ProxyAccountContract proxyAccountContract, List<string>? managers = null)
    {
        var managerList = new List<ManagementAddress>();
        if (managers != null)
            managerList.AddRange(
                managers.Select(manager => new ManagementAddress { Address = manager.ConvertAddress() }));
        else
        {
            var manager = NodeManager.NewAccount("12345678");
            managerList.Add(new ManagementAddress { Address = manager.ConvertAddress() });
        }

        var result = proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
            new AElf.Contracts.ProxyAccountContract.CreateInput
            {
                ManagementAddresses = { managerList }
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated")).NonIndexed;
        var proxyCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
        var virtualAddress = proxyCreated.ProxyAccountAddress;
        Logger.Info(proxyCreated);
        return virtualAddress;
    }

    private void CheckVirtualInfo(ProxyAccountCreated proxyCreated, ProxyAccountContract proxyAccountContract)
    {
        var chainId = ChainHelper.ConvertBase58ToChainId(proxyAccountContract.NodeManager.GetChainId());
        var virtualHash = proxyCreated.ProxyAccountHash;
        var virtualAddress = proxyCreated.ProxyAccountAddress;
        var managerList = proxyCreated.ManagementAddresses;
        var getVirtualAddress =
            proxyAccountContract.GetProxyAccountAddress(chainId, virtualHash);
        getVirtualAddress.ShouldBe(virtualAddress);

        var getProxyInfo = proxyAccountContract.GetProxyAccountByHash(virtualHash);
        var getAgentInfoByAddress = proxyAccountContract.GetProxyAccountByProxyAccountAddress(virtualAddress);
        getProxyInfo.ShouldBe(getAgentInfoByAddress);
        getProxyInfo.ManagementAddresses.ShouldBe(managerList.Value);
        getProxyInfo.ProxyAccountHash.ShouldBe(virtualHash);
        Logger.Info(getProxyInfo);
    }

    private void CheckBalance(string address, string type)
    {
        if (type == "main")
        {
            var balance = _tokenContract.GetUserBalance(address);
            if (balance <= 10000_00000000)
                _tokenContract.TransferBalance(InitAccount, address, 10000_00000000);
        }
        else
        {
            foreach (var sideTokenContract in _sideTokenContracts)
            {
                var balance = sideTokenContract.GetUserBalance(address);
                if (balance <= 10000_00000000)
                    sideTokenContract.TransferBalance(InitAccount, address, 10000_00000000);
            }
        }
    }
}