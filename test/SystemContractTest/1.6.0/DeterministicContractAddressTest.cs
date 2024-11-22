using System.Diagnostics;
using AElf;
using AElf.Contracts.ProxyAccountContract;
using AElf.Cryptography;
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
public class DeterministicContractAddressTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }

    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private ParliamentContract _parliamentContract;
    private AssociationContract _associationContract;

    private ProxyAccountContract _proxyAccountContract;

    private INodeManager SideNodeManager { get; set; }
    private AuthorityManager SideAuthorityManager { get; set; }
    private CrossChainManager _mainToSide;

    private GenesisContract _sideGenesisContract;
    private TokenContract _sideTokenContract;
    private ParliamentContract _sideParliamentContract;
    private AssociationContract _sideAssociationContract;

    private ProxyAccountContract _sideProxyAccountContract;

    //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
    //RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y
    private string _proxyContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
    private string _sideProxyContractAddress = "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y";
    private string InitAccount { get; } = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
    private static string RpcUrl { get; } = "http://127.0.0.1:8000";
    private static string SideRpcUrl { get; } = "https://127.0.0.1:8011";

    private readonly bool isNeedSide = false;

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("DeterministicContractAddressTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _parliamentContract = _genesisContract.GetParliamentContract(InitAccount);
        _associationContract = _genesisContract.GetAssociationAuthContract(InitAccount);
        _proxyAccountContract = _proxyContractAddress == ""
            ? new ProxyAccountContract(NodeManager, InitAccount)
            : new ProxyAccountContract(NodeManager, InitAccount, _proxyContractAddress);
        if (!isNeedSide) return;
        SideNodeManager = new NodeManager(SideRpcUrl);
        SideAuthorityManager = new AuthorityManager(SideNodeManager, InitAccount);
        _sideGenesisContract = GenesisContract.GetGenesisContract(SideNodeManager, InitAccount);
        _sideTokenContract = _sideGenesisContract.GetTokenContract();
        _sideParliamentContract = _sideGenesisContract.GetParliamentContract(InitAccount);
        _sideAssociationContract = _sideGenesisContract.GetAssociationAuthContract(InitAccount);

        _sideProxyAccountContract = _sideProxyContractAddress == ""
            ? new ProxyAccountContract(SideNodeManager, InitAccount)
            : new ProxyAccountContract(SideNodeManager, InitAccount, _sideProxyContractAddress);
        // _mainToSide = new CrossChainManager(NodeManager, SideNodeManager, InitAccount);
        // CrossChainTransfer("ELF", InitAccount, InitAccount, 1000000_00000000);
    }

    // Deploy：2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS - proxyAccount
    [TestMethod]
    public void InitializeProxy()
    {
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Initialize, new Empty());
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var admin = _proxyAccountContract.GetAdmin();
        admin.ShouldBe(InitAccount.ConvertAddress());
        var sideResult = _sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Initialize, new Empty());
        sideResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var sideAdmin = _sideProxyAccountContract.GetAdmin();
        sideAdmin.ShouldBe(InitAccount.ConvertAddress());
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
        var sideChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManager.GetChainId());
        input.ProxyAccountInfos.Add(new ProxyAccountInfo
        {
            ContractAddress = _sideProxyAccountContract.Contract,
            ChainId = sideChainId
        });


        var result = _sideProxyAccountContract.ExecuteMethodWithResult(ProxyMethod.SetProxyAccountContracts, input);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var mainResult = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.SetProxyAccountContracts, input);
        mainResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    [DataRow("AElf.Contracts.TestContract.B", "Token")]
    public void ProposalNewContract(string file, string token)
    {
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        //
        var deployer = NodeManager.NewAccount(out var privateKey, "12345678");
        // var deployer = "2MofVSAiGN5YekWNcHaTQcF4yAfK9VpvfS4YMa6PA9YpTU1ZWn";
        // var privateKey = NodeManager.AccountManager.GetPrivateKey(deployer); 
        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = 1;
        //
        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, Address.FromBase58(deployer), salt, version);
        var computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);
        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = Address.FromBase58(deployer),
            Salt = salt,
            Version = version,
            Signature = signature
        };

        var deployAddress =
            AuthorityManager.DeployContractWithAuthorityAndContractOperation(InitAccount, codeArray, contractOperation);
        deployAddress.ShouldBe(computedAddress);
        var contractInfo = _genesisContract.GetContractInfo(deployAddress);
        contractInfo.Deployer.ShouldBe(Address.FromBase58(deployer));
        contractInfo.SerialNumber.ShouldBe(0);

        // on side
        if (!isNeedSide) return;
        var sideChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManager.GetChainId());
        var sideChainSignature =
            GenerateContractSignature(privateKey, sideChainId, codeHash, Address.FromBase58(deployer), salt, version);
        computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);
        var sideContractOperation = new ContractOperation
        {
            ChainId = sideChainId,
            CodeHash = codeHash,
            Deployer = Address.FromBase58(deployer),
            Salt = salt,
            Version = version,
            Signature = sideChainSignature
        };
        var sideDeployAddress =
            SideAuthorityManager.DeployContractWithAuthorityAndContractOperation(InitAccount, codeArray,
                sideContractOperation);
        sideDeployAddress.ShouldBe(computedAddress);
        var sideContractInfo = _sideGenesisContract.GetContractInfo(sideDeployAddress);
        sideContractInfo.Deployer.ShouldBe(Address.FromBase58(deployer));
        sideContractInfo.SerialNumber.ShouldBe(0);
        Logger.Info(sideContractInfo);
        sideDeployAddress.ShouldBe(deployAddress);
    }

    
    [TestMethod]
    [DataRow("AElf.Contracts.TestContract.B")]
    public void ProposalNewContractWithNull(string file)
    {
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);

        var deployAddress =
            AuthorityManager.DeployContractWithAuthorityAndContractOperation(InitAccount, codeArray, null);
        var contractInfo = _genesisContract.GetContractInfo(deployAddress);
        contractInfo.SerialNumber.ShouldNotBe(0);
    }
    
    [TestMethod]
    [DataRow("AElf.Contracts.TestContract.VirtualTransactionEvent-1", "Test")]
    public void ProxyAccountProposalNewContract(string file, string token)
    {
        var proxyInfo = CreateProxyCount(1, "side", out var proxyAddress);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = SideAuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var signerAddress = SideNodeManager.NewAccount(out var privateKey, "12345678");
        ProxySetSigner(signerAddress, proxyAddress);

        var chainId = ChainHelper.ConvertBase58ToChainId(SideNodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = 1;

        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, proxyAddress, salt, version);

        var computedAddress = BuildContractAddressWithSalt(proxyAddress, salt);
        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = proxyAddress,
            Salt = salt,
            Version = version,
            Signature = signature
        };

        var deployAddress =
            SideAuthorityManager.DeployContractWithAuthorityAndContractOperation(InitAccount, codeArray, contractOperation);
        deployAddress.ShouldBe(computedAddress);
        var contractInfo = _sideGenesisContract.GetContractInfo(deployAddress);
        contractInfo.Deployer.ShouldBe(proxyAddress);
        contractInfo.SerialNumber.ShouldBe(0);
        Logger.Info(contractInfo);
        var getSigner = _sideGenesisContract.GetSigner(contractInfo.Deployer);
        getSigner.Value.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("AElf.Contracts.HelloWorldContract", "Test1")]
    public void OrganizationProposalNewContract(string file, string token)
    {
        var list = NodeManager.ListAccounts().Take(3);
        var organization = AuthorityManager.CreateAssociationOrganization(list);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var signerAddress = NodeManager.NewAccount(out var privateKey, "12345678");
        OrganizationSetSigner(signerAddress, organization);

        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = 1;

        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, organization, salt, version);

        var computedAddress = BuildContractAddressWithSalt(organization, salt);
        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = organization,
            Salt = salt,
            Version = version,
            Signature = signature
        };

        var deployAddress =
            AuthorityManager.DeployContractWithAuthorityAndContractOperation(InitAccount, codeArray, contractOperation);
        deployAddress.ShouldBe(computedAddress);
        var contractInfo = _genesisContract.GetContractInfo(deployAddress);
        contractInfo.Deployer.ShouldBe(organization);
        contractInfo.SerialNumber.ShouldBe(0);
        Logger.Info(contractInfo);
        var getSigner = _genesisContract.GetSigner(organization);
        getSigner.Value.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("2om4giRxe4a3NU7QmbfGSQ5TE2xFZ7VYgns6zXXGaybZGgEQY2", "AElf.Contracts.TestContractForPlugin-origin-1.2.0",
        "Token")]
    public void ProposalUpdateContract(string contractAddress, string file, string token)
    {
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());

        var deployer = contractInfo.Deployer;
        // var deployer = Address.FromBase58("HNucQFqTJVLKJm6EENt8JkzHBEJKUn2hcJDcvvCthnYc4SPsE");
        var privateKey = NodeManager.AccountManager.GetPrivateKey(deployer.ToBase58(), "12345678");

        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = contractInfo.Version.Add(1);

        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, deployer, salt, version);
        var computedAddress = BuildContractAddressWithSalt(deployer, salt);
        computedAddress.ToBase58().ShouldBe(contractAddress);
        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = deployer,
            Salt = salt,
            Version = version,
            Signature = signature
        };

        AuthorityManager.UpdatedContractWithAuthorityAndContractOperation(InitAccount, contractAddress, codeArray,
            null);
        contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        contractInfo.Deployer.ShouldBe(deployer);
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Version.ShouldBe(contractOperation.Version);
        Logger.Info(contractInfo);
    }

    [TestMethod]
    [DataRow("2E3p3mrZuv4Bz3YdPDHH2tM5D18qZ9vGMG1aR7snCGqeLz2umL", "AElf.Contracts.TestContractForPlugin-new-1.2.0",
        "Test")]
    public void ProxyAccountProposalUpdateContract(string contractAddress, string file, string token)
    {
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var proxyInfo = _proxyAccountContract.GetProxyAccountByProxyAccountAddress(contractInfo.Deployer);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var signerAddress = NodeManager.NewAccount(out var privateKey, "12345678");
        ProxySetSigner(signerAddress, contractInfo.Deployer);

        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = contractInfo.Version.Add(1);

        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, contractInfo.Deployer, salt, version);

        var computedAddress = BuildContractAddressWithSalt(contractInfo.Deployer, salt);
        computedAddress.ToBase58().ShouldBe(contractAddress);
        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = contractInfo.Deployer,
            Salt = salt,
            Version = version,
            Signature = signature
        };

        AuthorityManager.UpdatedContractWithAuthorityAndContractOperation(InitAccount, contractAddress, codeArray,
            contractOperation);
        contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        contractInfo.Deployer.ShouldBe(contractInfo.Deployer);
        contractInfo.SerialNumber.ShouldBe(0);
        Logger.Info(contractInfo);
        var getSigner = _genesisContract.GetSigner(contractInfo.Deployer);
        getSigner.Value.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("2TUeKCE2PakWke5gmSYiio2B412zmLdbUswfUeDBFDwZ76rYFA", "Contracts.BeangoTownContract-1.0.0.2", "Test1")]
    public void OrganizationProposalUpdateContract(string contractAddress, string file, string token)
    {
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var organization = contractInfo.Deployer;
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var signerAddress = NodeManager.NewAccount(out var privateKey, "12345678");
        OrganizationSetSigner(signerAddress, organization);

        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = contractInfo.Version.Add(1);

        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, organization, salt, version);

        var computedAddress = BuildContractAddressWithSalt(organization, salt);
        contractAddress.ShouldBe(computedAddress.ToBase58());

        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = organization,
            Salt = salt,
            Version = version,
            Signature = signature
        };

        AuthorityManager.UpdatedContractWithAuthorityAndContractOperation(InitAccount, contractAddress, codeArray,
            contractOperation);
        contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        contractInfo.Deployer.ShouldBe(organization);
        contractInfo.SerialNumber.ShouldBe(0);
        Logger.Info(contractInfo);
        var getSigner = _genesisContract.GetSigner(organization);
        getSigner.Value.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("2suTLaN4ZLUyiAEh2TzdKHYPZWy62Do3MsjTV4h8tg4nTG2qVY", "AElf.Contracts.TestContractForPlugin-origin-1.2.0",
        "Token")]
    public void ProposalUpdatePermissionCheck(string contractAddress, string file, string token)
    {
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        codeArray = AuthorityManager.GenerateUniqContractCode(codeArray);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());

        var fakeDeployer = NodeManager.NewAccount(out var privateKey, "12345678");

        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = contractInfo.Version.Add(1);
        var signature =
            GenerateContractSignature(privateKey, chainId, codeHash, Address.FromBase58(fakeDeployer), salt, version);

        var contractOperation = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = Address.FromBase58(fakeDeployer),
            Salt = salt,
            Version = version,
            Signature = signature
        };

        var input = new ContractUpdateInput
        {
            Address = contractAddress.ConvertAddress(),
            Code = ByteString.CopyFrom(codeArray),
            ContractOperation = contractOperation
        };

        var proposalUpdate = _genesisContract.ExecuteMethodWithResult(GenesisMethod.ProposeUpdateContract, input);
        proposalUpdate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        proposalUpdate.Error.ShouldContain("No permission.");
    }

    [TestMethod]
    [DataRow("Contracts.BeangoTownContract-1.0.0.3", "Token")]
    public void DeployUserSmartContract(string file, string token)
    {
        var deployer = NodeManager.NewAccount("12345678");
        _tokenContract.TransferBalance(InitAccount, deployer, 1000_00000000);
        var salt = HashHelper.ComputeFrom(token);
        // var salt = Hash.Empty;
        var computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);

        var result = _genesisContract.DeployUserSmartContract(file, salt, deployer);
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _parliamentContract.CheckDeployOrUpdateProposal(proposalId, AuthorityManager.GetCurrentMiners());
        var currentHeight = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var release = _genesisContract.FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
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
                    $"Version: {contractDeployedNonIndexed.Version}\n" +
                    $"ContractVersion: {contractDeployedNonIndexed.ContractVersion}\n" +
                    $"Height: {release.BlockNumber}");

        contractDeployedNonIndexed.Address.ShouldBe(computedAddress);
        var contractInfo = _genesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            contractDeployedNonIndexed.Address);
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(Address.FromBase58(deployer));
        Logger.Info($"Deploy Contract Info: {contractInfo}");

        if (!isNeedSide) return;

        computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);
        _sideTokenContract.TransferBalance(InitAccount, deployer, 1000000000000);
        result = _sideGenesisContract.DeployUserSmartContract(file, salt, deployer);
        proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _sideParliamentContract.CheckDeployOrUpdateProposal(proposalId, SideAuthorityManager.GetCurrentMiners());
        currentHeight = AsyncHelper.RunSync(SideNodeManager.ApiClient.GetBlockHeightAsync);
        release = _sideGenesisContract.FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
        releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(ContractDeployed)));
        indexed = releaseLogEvent.Indexed;
        nonIndexed = releaseLogEvent.NonIndexed;
        foreach (var i in indexed)
        {
            var contractDeployedIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(i));
            Logger.Info(contractDeployedIndexed.Author == null
                ? $"Code hash: {contractDeployedIndexed.CodeHash.ToHex()}"
                : $"Author: {contractDeployedIndexed.Author}");
        }

        contractDeployedNonIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        Logger.Info($"Address: {contractDeployedNonIndexed.Address}\n" +
                    $"Version: {contractDeployedNonIndexed.Version}\n" +
                    $"ContractVersion: {contractDeployedNonIndexed.ContractVersion}\n" +
                    $"Height: {release.BlockNumber}");

        contractDeployedNonIndexed.Address.ShouldBe(computedAddress);
        contractInfo = _sideGenesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            contractDeployedNonIndexed.Address);
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(Address.FromBase58(deployer));
        Logger.Info($"Deploy Contract Info: {contractInfo}");
        contractDeployedNonIndexed.Address.ShouldBe(computedAddress);
    }
    
    [TestMethod]
    [DataRow("TAfeBaVmVHqxwfbzNXMubczh6zr5s9MZKJTeixDkCiAuNCf3R","Contracts.BeangoTownContract-1.0.0.4", "Token")]
    public void UpdateUserSmartContract(string contractAddress, string file, string token)
    {
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var deployer = contractInfo.Deployer;
        _tokenContract.TransferBalance(InitAccount, deployer.ToBase58(), 1000_00000000);
        // var salt = HashHelper.ComputeFrom(token);
        var salt = Hash.Empty;
        var computedAddress = BuildContractAddressWithSalt(deployer, salt);

        var result = _genesisContract.UpdateUserSmartContract(file, contractAddress, deployer.ToBase58());
        result.Transaction.From.ShouldBe(deployer.ToBase58());
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _parliamentContract.CheckDeployOrUpdateProposal(proposalId, AuthorityManager.GetCurrentMiners());
        var currentHeight = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var release = _genesisContract.FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
        var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
        var indexed = releaseLogEvent.Indexed;
        var nonIndexed = releaseLogEvent.NonIndexed;
        var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
        Logger.Info($"Address: {codeUpdatedIndexed.Address}\n" + 
                    $"Height: {release.BlockNumber}");

        var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        Logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                    $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                    $"{codeUpdatedNonIndexed.Version}\n" +
                    $"{codeUpdatedNonIndexed.ContractVersion}");

        contractAddress.ShouldBe(computedAddress.ToBase58());
        contractInfo = _genesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            Address.FromBase58(contractAddress));
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(deployer);
        Logger.Info($"Update Contract Info: {contractInfo}");
    }
    
    [TestMethod]
    [DataRow("AElf.Contracts.TestContract.VirtualTransactionEvent-acs12", "VirtualTransactionEvent")]
    public void ProxyDeployUserSmartContract(string file, string token)
    {
        var proxyInfo = CreateProxyCount(1, "side", out var proxyAddress);
        _sideTokenContract.TransferBalance(InitAccount, proxyAddress.ToBase58(), 10_00000000);
        var salt = HashHelper.ComputeFrom(token);
        var computedAddress = BuildContractAddressWithSalt(proxyAddress, salt);

        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);

        var input = new UserContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Category = 0,
            Salt = salt
        };

        var result = _sideProxyAccountContract.ForwardCall(input, _sideGenesisContract.Contract,
            nameof(GenesisMethod.DeployUserSmartContract), proxyInfo, 0);
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _sideParliamentContract.CheckDeployOrUpdateProposal(proposalId, AuthorityManager.GetCurrentMiners());
        var currentHeight = AsyncHelper.RunSync(SideNodeManager.ApiClient.GetBlockHeightAsync);
        var release =
            _sideGenesisContract.FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
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
                    $"Version: {contractDeployedNonIndexed.Version}\n" +
                    $"ContractVersion: {contractDeployedNonIndexed.ContractVersion}\n" +
                    $"Height: {release.BlockNumber}");

        contractDeployedNonIndexed.Address.ShouldBe(computedAddress);
        var contractInfo = _sideGenesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            contractDeployedNonIndexed.Address);
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(proxyAddress);
        Logger.Info($"Deploy Contract Info: {contractInfo}");
    }
    
    [TestMethod]
    [DataRow("YtAKjHmUqA5Y5Fgwg7SDoNwFUGt8qEaovvxetH1t2xk2hQWeC", "AElf.Contracts.TestContract.A-1.1.0","Token")]
    public void ProxyUpdateUserSmartContract(string contractAddress, string file, string token)
    {
        var contractInfo = _sideGenesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var author = contractInfo.Author;
        var deployer = contractInfo.Deployer;
        var proxyInfo = _sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(deployer);
        var salt = HashHelper.ComputeFrom(token);
        var computedAddress = BuildContractAddressWithSalt(deployer, salt);

        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);

        var input = new UserContractUpdateInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Address = Address.FromBase58(contractAddress)
        };

        var result = _sideProxyAccountContract.ForwardCall(input, _sideGenesisContract.Contract,
            nameof(GenesisMethod.UpdateUserSmartContract), proxyInfo, 0);
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _sideParliamentContract.CheckDeployOrUpdateProposal(proposalId, AuthorityManager.GetCurrentMiners());
        var currentHeight = AsyncHelper.RunSync(SideNodeManager.ApiClient.GetBlockHeightAsync);
        var release =
            _sideGenesisContract.FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
        var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
        var indexed = releaseLogEvent.Indexed;
        var nonIndexed = releaseLogEvent.NonIndexed;
        var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
        Logger.Info($"Address: {codeUpdatedIndexed.Address}\n" + 
                    $"Height: {release.BlockNumber}");

        var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        Logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                    $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                    $"{codeUpdatedNonIndexed.Version}\n" +
                    $"{codeUpdatedNonIndexed.ContractVersion}");

        contractAddress.ShouldBe(computedAddress.ToBase58());
        contractInfo = _sideGenesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            Address.FromBase58(contractAddress));
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(deployer);
        Logger.Info($"Update Contract Info: {contractInfo}");
    }

    [TestMethod]
    [DataRow("Contracts.BeangoTownContract-1.0.0.6", "Token")]
    public void AssociationDeployUserSmartContract(string file, string token)
    {
        var list = SideNodeManager.ListAccounts().Take(3);
        var organization = SideAuthorityManager.CreateAssociationOrganization(list);
        var organizationInfo = _sideAssociationContract.GetOrganization(organization);
        var proposer = organizationInfo.ProposerWhiteList.Proposers.First().ToBase58();
        _sideTokenContract.TransferBalance(InitAccount, proposer, 1000_00000000);
        var salt = HashHelper.ComputeFrom(token);
        var computedAddress = BuildContractAddressWithSalt(organization, salt);

        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);

        var input = new UserContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray),
            Category = 0,
            Salt = salt
        };

        foreach (var member in organizationInfo.OrganizationMemberList.OrganizationMembers)
        {
            _sideTokenContract.TransferBalance(InitAccount, member.ToBase58(), 10000000000);
        }

        var proposal = _sideAssociationContract.CreateProposal(_sideGenesisContract.ContractAddress,
            nameof(GenesisMethod.DeployUserSmartContract),
            input, organization, proposer);
        _sideAssociationContract.ApproveWithAssociation(proposal, organization);
        var releaseDeploy = _sideAssociationContract.ReleaseProposal(proposal, proposer);
        releaseDeploy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var proposalLogEvent = releaseDeploy.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _sideParliamentContract.CheckDeployOrUpdateProposal(proposalId, AuthorityManager.GetCurrentMiners());
        var currentHeight = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
        var release =
            _sideGenesisContract.FindReleaseApprovedUserSmartContractMethod(releaseDeploy.BlockNumber, currentHeight);

        Logger.Info($"Release Transaction: {release.TransactionId}");
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
                    $"Version: {contractDeployedNonIndexed.Version}\n" +
                    $"ContractVersion: {contractDeployedNonIndexed.ContractVersion}\n" +
                    $"Height: {release.BlockNumber}");

        contractDeployedNonIndexed.Address.ShouldBe(computedAddress);
        var contractInfo = _sideGenesisContract.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            contractDeployedNonIndexed.Address);
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.Deployer.ShouldBe(organization);
        Logger.Info($"Deploy Contract Info: {contractInfo}");
    }

    [TestMethod]
    [DataRow("Contracts.BeangoTownContract-1.0.0.7", "Contracts.BeangoTownContract-1.0.0.8", "Token")]
    public void DeployUserSameTime(string file1, string file2, string token)
    {
        var deployer = NodeManager.NewAccount("12345678");
        var salt = HashHelper.ComputeFrom(token);
        var computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);
        Logger.Info(computedAddress);
        _tokenContract.TransferBalance(InitAccount, deployer, 100000_00000000);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray1 = contractReader.Read(file1);
        var codeArray2 = contractReader.Read(file2);

        var input1 = new UserContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray1),
            Category = 0,
            Salt = salt
        };
        var input2 = new UserContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray2),
            Category = 0,
            Salt = salt
        };
        var txId = _genesisContract.DeployUserSmartContractWithoutResult(input1, deployer);
        var txId2 = _genesisContract.DeployUserSmartContractWithoutResult(input2, deployer);

        NodeManager.CheckTransactionResult(txId);
        NodeManager.CheckTransactionResult(txId2);
    }

    [TestMethod]
    [DataRow("Contracts.BeangoTownContract-1.0.0.7", "Contracts.BeangoTownContract-1.0.0.8", "Token")]
    public void ProposalNewSameTime(string file1, string file2, string token)
    {
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray1 = contractReader.Read(file1);
        codeArray1 = AuthorityManager.GenerateUniqContractCode(codeArray1);
        var codeHash1 = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray1).ToByteArray());

        var codeArray2 = contractReader.Read(file2);
        codeArray2 = AuthorityManager.GenerateUniqContractCode(codeArray2);
        var codeHash2 = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray2).ToByteArray());

        var deployer = NodeManager.NewAccount(out var privateKey, "12345678");
        // var deployer = "2MofVSAiGN5YekWNcHaTQcF4yAfK9VpvfS4YMa6PA9YpTU1ZWn";
        // var privateKey = NodeManager.AccountManager.GetPrivateKey(deployer); 
        _tokenContract.TransferBalance(InitAccount, deployer, 10000_00000000);
        var chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        var salt = HashHelper.ComputeFrom(token);
        var version = 1;
        //
        var signature1 =
            GenerateContractSignature(privateKey, chainId, codeHash1, Address.FromBase58(deployer), salt, version);
        var signature2 =
            GenerateContractSignature(privateKey, chainId, codeHash2, Address.FromBase58(deployer), salt, version);

        var computedAddress = BuildContractAddressWithSalt(Address.FromBase58(deployer), salt);
        Logger.Info(computedAddress);
        var contractOperation1 = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash1,
            Deployer = Address.FromBase58(deployer),
            Salt = salt,
            Version = version,
            Signature = signature1
        };

        var contractOperation2 = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash2,
            Deployer = Address.FromBase58(deployer),
            Salt = salt,
            Version = version,
            Signature = signature2
        };

        var input1 = new ContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray1),
            Category = KernelHelper.DefaultRunnerCategory,
            ContractOperation = contractOperation1
        };

        var input2 = new ContractDeploymentInput
        {
            Code = ByteString.CopyFrom(codeArray2),
            Category = KernelHelper.DefaultRunnerCategory,
            ContractOperation = contractOperation2
        };

        var txId1 = _genesisContract.ProposeNewContractWithoutResult(input1, InitAccount);
        var txId2 = _genesisContract.ProposeNewContractWithoutResult(input2, InitAccount);

        NodeManager.CheckTransactionResult(txId1);
        NodeManager.CheckTransactionResult(txId2);
    }


    [TestMethod]
    public void GetSigner()
    {
        var contractAddress = "2E3p3mrZuv4Bz3YdPDHH2tM5D18qZ9vGMG1aR7snCGqeLz2umL";
        var contractInfo = _genesisContract.GetContractInfo(Address.FromBase58(contractAddress));
        var getSigner = _genesisContract.GetSigner(contractInfo.Deployer);
        getSigner.Value.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetContractInfo()
    {
        var contract = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        var info = _genesisContract.GetContractInfo(Address.FromBase58(contract));
    }

    private ProxyAccount CreateProxyCount(int managerCount, string chain, out Address proxyAddress)
    {
        INodeManager nodeManager;
        TokenContract tokenContract;
        ProxyAccountContract proxyAccountContract;

        if (chain == "side")
        {
            nodeManager = SideNodeManager;
            tokenContract = _sideTokenContract;
            proxyAccountContract = _sideProxyAccountContract;
        }
        else
        {
            nodeManager = NodeManager;
            tokenContract = _tokenContract;
            proxyAccountContract = _proxyAccountContract;
        }

        var managerList = new List<ManagementAddress>();

        for (var i = 0; i < managerCount; i++)
        {
            var m = nodeManager.NewAccount("12345678");
            tokenContract.TransferBalance(InitAccount, m, 10_00000000);
            managerList.Add(new ManagementAddress { Address = m.ConvertAddress() });
        }

        var createChainId = ChainHelper.ConvertBase58ToChainId(nodeManager.GetChainId());

        proxyAccountContract.SetAccount(managerList.First().Address.ToBase58());
        var result = proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
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
        proxyAddress = proxyAccountCreated.ProxyAccountAddress;
        return proxyAccountContract.GetProxyAccountByHash(proxyAccountCreated.ProxyAccountHash);
    }

    private void ProxySetSigner(string singerAddress, Address proxyAccount)
    {
        var proxyAccountInfo = _sideProxyAccountContract.GetProxyAccountByProxyAccountAddress(proxyAccount);
        var result = _sideProxyAccountContract.ForwardCall(Address.FromBase58(singerAddress), _sideGenesisContract.Contract,
            nameof(GenesisMethod.SetSigner), proxyAccountInfo, 0);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var getSigner = _sideGenesisContract.GetSigner(proxyAccount);
        getSigner.ShouldBe(Address.FromBase58(singerAddress));
    }

    private void OrganizationSetSigner(string singerAddress, Address organization)
    {
        var organizationInfo = _associationContract.GetOrganization(organization);
        var proposer = organizationInfo.ProposerWhiteList.Proposers.First().ToBase58();
        foreach (var member in organizationInfo.OrganizationMemberList.OrganizationMembers)
        {
            _tokenContract.TransferBalance(InitAccount, member.ToBase58(), 10000000000);
        }

        var result = _associationContract.CreateProposal(_genesisContract.ContractAddress,
            nameof(GenesisMethod.SetSigner),
            Address.FromBase58(singerAddress), organization, proposer);
        _associationContract.ApproveWithAssociation(result, organization);
        var release = _associationContract.ReleaseProposal(result, proposer);
        release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var getSigner = _genesisContract.GetSigner(organization);
        getSigner.ShouldBe(Address.FromBase58(singerAddress));
    }


    private Address BuildContractAddressWithSalt(Address deployer, Hash salt)
    {
        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(deployer), salt);
        return Address.FromBytes(hash.ToByteArray());
    }

    private ByteString GenerateContractSignature(byte[] privateKey, int chainId, Hash codeHash,
        Address address, Hash salt, int version)
    {
        var data = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = address,
            Salt = salt,
            Version = version
        };
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }

    private void CrossChainTransfer(string symbol, string fromAddress, string toAddress, long amount)
    {
        var transfer = _mainToSide.CrossChainTransfer(symbol, amount, toAddress, fromAddress, out var raw);
        _mainToSide.CheckSideChainIndexMainChain(transfer.BlockNumber);
        _mainToSide.CrossChainReceive(transfer.BlockNumber, transfer.TransactionId, raw);
    }
}