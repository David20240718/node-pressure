using System.Diagnostics;
using AElf;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Volo.Abp.Threading;

namespace DeployAndUpdateContract;

public class DeployAndUpdateService
{
    public DeployAndUpdateService(Service service, ILog logger)
    {
        _service = service;
        _logger = logger;
    }

    public ContractInfoDto DeployContracts(bool isApproval, string file, AuthorInfo author, string salt = "")
    {
        _logger.Info("======== AddWhiteList ========");
        ParliamentChangeWhiteList();
        
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var saltHash = HashHelper.ComputeFrom(salt);
        ContractInfoDto contractInfoDto;
        if (isApproval)
        {
            var contractOperation = salt == ""
                ? null
                : GenerateContractOperation(author, codeHash, saltHash, 1);

            var input = new ContractDeploymentInput
            {
                Category = 0,
                Code = ByteString.CopyFrom(codeArray),
                ContractOperation = contractOperation
            };
            
            _logger.Info("======== WithApproval ========");
            var contractProposalInfo = ProposeNewContract(input);
            ApproveByMiner(contractProposalInfo.ProposalId);
            var releaseCodeCheckInput = ReleaseApprove(contractProposalInfo);
            contractInfoDto = ReleaseCodeCheck(releaseCodeCheckInput, "deploy");
        }
        else
        {
            _logger.Info("======== AddWhiteList ========");
            ParliamentChangeWhiteList();
            
            var input = new UserContractDeploymentInput
            {
                Category = 0,
                Code = ByteString.CopyFrom(codeArray),
                Salt = salt == "" ? null : HashHelper.ComputeFrom(salt)
            };

            _logger.Info("======== WithoutApproval ========");
            contractInfoDto = DeployUserContract(input, author);
        }

        return contractInfoDto;

    }

    public ContractInfoDto UpdateContracts(bool isApproval, UpdateInfo updateInfo, string file, AuthorInfo author, string salt = "")
    {
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        var codeArray = contractReader.Read(file);
        var address = updateInfo.isSystemContract
            ? _service.GenesisService.GetContractAddressByName(updateInfo.ContractName.ConvertNameProvider())
            : updateInfo.ContractAddress.ConvertAddress();
        var info = _service.GenesisService.GetContractInfo(address);
        _logger.Info(info.Version);
        var codeHash = HashHelper.ComputeFrom(ByteString.CopyFrom(codeArray).ToByteArray());
        var saltHash = HashHelper.ComputeFrom(salt);
        var version = _service.GenesisService.GetContractInfo(address).Version;
        ContractInfoDto contractInfoDto;
        if (isApproval || updateInfo.isSystemContract)
        {
            var contractOperation = salt == ""
                ? null
                : GenerateContractOperation(author, codeHash, saltHash, version + 1);

            var input = new ContractUpdateInput
            {
                Address = address,
                Code = ByteString.CopyFrom(codeArray),
                ContractOperation = contractOperation
            };

            
            _logger.Info("========WithApproval========");
            var contractProposalInfo = ProposalUpdateContract(input);
            ApproveByMiner(contractProposalInfo.ProposalId);
            var releaseCodeCheckInput = ReleaseApprove(contractProposalInfo);
            contractInfoDto = ReleaseCodeCheck(releaseCodeCheckInput, "update");
        }
        else
        {
            var input = new ContractUpdateInput
            {
                Address = address,
                Code = ByteString.CopyFrom(codeArray)

            };
            _logger.Info("========WithoutApproval========");
            contractInfoDto = UpdateUserContract(input);
        }

        return contractInfoDto;
    }


    private ContractInfoDto DeployUserContract(UserContractDeploymentInput input, AuthorInfo authorInfo)
    {
        var author = authorInfo.Author == "" ? _service.CallAddress : authorInfo.Author;
        _service.GenesisService.SetAccount(author);
        var result = _service.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeployUserSmartContract, input);
        var logEvent = result.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
        var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;

        var returnValue =
            DeployUserSmartContractOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
        var codeHash = returnValue.CodeHash;
        _logger.Info(
            $"Code hash: {codeHash.ToHex()}\n ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}\n Proposal Id: {proposalId.ToHex()}");

        var check = CheckProposal(proposalId);
        if (!check)
            return new ContractInfoDto("",0,"","");

        var currentHeight = AsyncHelper.RunSync(_service._nodeManager.ApiClient.GetBlockHeightAsync);
        _logger.Info($"Check height: {result.BlockNumber} - {currentHeight}");

        var release = FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);
        if (release.Equals(new TransactionResultDto()))
            return new ContractInfoDto("",0,"","");

        _logger.Info($"Release Transaction: {release.TransactionId}");
        var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(ContractDeployed)));
        var indexed = releaseLogEvent.Indexed;
        var nonIndexed = releaseLogEvent.NonIndexed;
        foreach (var i in indexed)
        {
            var contractDeployedIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(i));
            _logger.Info(contractDeployedIndexed.Author == null
                ? $"Code hash: {contractDeployedIndexed.CodeHash.ToHex()}"
                : $"Author: {contractDeployedIndexed.Author}");
        }

        var contractDeployedNonIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        _logger.Info($"Address: {contractDeployedNonIndexed.Address}\n" +
                     $"Version: {contractDeployedNonIndexed.Version}\n" +
                     $"ContractVersion: {contractDeployedNonIndexed.ContractVersion}\n" +
                     $"Height: {release.BlockNumber}");

        var contractInfo = _service.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            contractDeployedNonIndexed.Address);
        _logger.Info($"Deploy Contract Info: {contractInfo}");
        
        return new ContractInfoDto(
            contractDeployedNonIndexed.Address.ToBase58(),
            release.BlockNumber,
            contractInfo.ContractVersion,
            contractInfo.Author.ToBase58()
        );
    }

    private ContractInfoDto UpdateUserContract(ContractUpdateInput input)
    {
        var author = _service.GenesisService.GetContractAuthor(input.Address);
        _service.GenesisService.SetAccount(author.ToBase58());
        var result =
            _service.GenesisService.ExecuteMethodWithResult(GenesisMethod.UpdateUserSmartContract, input);

        var logEvent = result.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
        var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
        var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;
        _logger.Info(
            $"ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}\n Proposal Id: {proposalId.ToHex()}");

        var check = CheckProposal(proposalId);
        if (!check)
            return new ContractInfoDto("",0,"","");

        var currentHeight = AsyncHelper.RunSync(_service._nodeManager.ApiClient.GetBlockHeightAsync);
        _logger.Info($"======== Find block {result.BlockNumber} to {currentHeight}");
        var release = FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);
        if (release.Equals(new TransactionResultDto()))
            return new ContractInfoDto("",0,"","");
        _logger.Info($"Release txId: {release.TransactionId}");
        var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
        var indexed = releaseLogEvent.Indexed;
        var nonIndexed = releaseLogEvent.NonIndexed;
        var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
        _logger.Info($"Address: {codeUpdatedIndexed.Address}\n" +
                     $"Height: {release.BlockNumber}");

        var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
        _logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                     $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                     $"{codeUpdatedNonIndexed.Version}\n" +
                     $"{codeUpdatedNonIndexed.ContractVersion}");

        _logger.Info("======== Check contract Info ========");
        var contractInfo = _service.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            input.Address);
        _logger.Info($"Update Contract Info: {contractInfo}");
        return new ContractInfoDto(
            codeUpdatedIndexed.Address.ToBase58(),
            release.BlockNumber,
            contractInfo.ContractVersion,
            contractInfo.Author.ToBase58()
        );
    }

    private ReleaseContractInput ProposeNewContract(ContractDeploymentInput input)
    {
        var result =
            _service.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeNewContract, input);
        var contractProposalInfo = GetLogs(result);
        _logger.Info($"{contractProposalInfo.ProposalId}\n {contractProposalInfo.ProposedContractInputHash}");
        return contractProposalInfo;
    }

    private ReleaseContractInput ProposalUpdateContract(ContractUpdateInput input)
    {
        var result =
            _service.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeUpdateContract, input);
        var contractProposalInfo = GetLogs(result);
        return contractProposalInfo;
    }

    private ReleaseContractInput GetLogs(TransactionResultDto resultDto)
    {
        var proposalId = ProposalCreated.Parser
            .ParseFrom(ByteString.FromBase64(resultDto.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed))
            .ProposalId;
        var proposalHash = ContractProposed.Parser
            .ParseFrom(ByteString.FromBase64(resultDto.Logs.First(l => l.Name.Contains(nameof(ContractProposed)))
                .NonIndexed))
            .ProposedContractInputHash;

        return new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = proposalHash
        };
    }

    public void CheckMinersAndInitAccountBalance()
    {
        var minersPubkey = _service.ConsensusService.GetCurrentMinersPubkey();
        var miners = minersPubkey.Select(m => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(m))).ToList();
        string account = null;
        if (!miners.Contains(_service.CallAccount))
            miners.Add(_service.CallAccount);
        foreach (var miner in miners)
        {
            var balance = _service.TokenService.GetUserBalance(miner.ToBase58());
            if (balance < 100000_00000000) continue;
            account = miner.ToBase58();
        }

        if (account == null)
            return;

        foreach (var miner in miners)
        {
            var balance = _service.TokenService.GetUserBalance(miner.ToBase58());
            if (balance > 10000_00000000) continue;
            _service.TokenService.SetAccount(account);
            _service.TokenService.TransferBalance(account, miner.ToBase58(), 10001_00000000);
        }
    }


    private void ApproveByMiner(Hash proposalId)
    {
        var miners = _service.ConsensusService.GetCurrentMinersPubkey();
        foreach (var minerPubkey in miners)
        {
            var miner = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minerPubkey));
            _service.ParliamentService.SetAccount(miner.ToBase58());
            _service.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
            if (_service.ParliamentService.CheckProposal(proposalId).ToBeReleased) return;
        }
    }

    private ReleaseContractInput ReleaseApprove(ReleaseContractInput input)
    {
        var release = _service.GenesisService.ReleaseApprovedContract(input, _service.CallAddress);
        var byteString =
            ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
        var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
        return new ReleaseContractInput
        {
            ProposalId = deployProposal,
            ProposedContractInputHash = input.ProposedContractInputHash
        };
    }


    private ContractInfoDto ReleaseCodeCheck(ReleaseContractInput input, string type)
    {
        var check = CheckProposal(input.ProposalId);
        if (!check)
            return new ContractInfoDto("",0,"","");
        var release =
            _service.GenesisService.ReleaseCodeCheckedContract(input, _service.CallAddress);

        Address changeAddress;
        ContractInfo contractInfo;
        if (type == "deploy")
        {
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var byteStringIndexed =
                ByteString.FromBase64(
                    release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed.First());
            var contractDeployed = ContractDeployed.Parser.ParseFrom(byteString);
            changeAddress = contractDeployed.Address;
            var author = ContractDeployed.Parser.ParseFrom(byteStringIndexed).Author;
            _logger.Info($"Deploy: {changeAddress}, \n" +
                         $"Author: {author}, Deploy height: {release.BlockNumber}");
            
            _logger.Info("======== Check contract Info ========");
            contractInfo = _service.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                changeAddress);
            _logger.Info(contractInfo);
            
            return new ContractInfoDto(
                changeAddress.ToBase58(),
                release.BlockNumber,
                contractInfo.ContractVersion,
                author.ToBase58()
            );
        }
        else
        {
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed.First());
            changeAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            var nonIndexed =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed);
            var nonCodeUpdated = CodeUpdated.Parser.ParseFrom(nonIndexed);
            var newHash = nonCodeUpdated.NewCodeHash;
            var oldHash = nonCodeUpdated.OldCodeHash;

            _logger.Info($"Update: {changeAddress}, \n" +
                         $"Update height: {release.BlockNumber}");
        }

        _logger.Info("======== Check contract Info ========");
        contractInfo = _service.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
            changeAddress);
        _logger.Info(contractInfo);
        return new ContractInfoDto(
            changeAddress.ToBase58(),
            release.BlockNumber,
            contractInfo.ContractVersion,
            contractInfo.Author.ToBase58()
            );

    }

    private bool CheckProposal(Hash proposalId)
    {
        var miners = _service.ConsensusService.GetCurrentMinersPubkey();

        var checkTimes = (miners.Count.Add(1)).Mul(8);
        var proposalInfo = _service.ParliamentService.CheckProposal(proposalId);
        _logger.Info("======== Check Proposal Info ========");
        var stopwatch = Stopwatch.StartNew();
        while (!proposalInfo.ToBeReleased && proposalInfo.ExpiredTime != null && checkTimes > 0)
        {
            Thread.Sleep(60000);
            proposalInfo = _service.ParliamentService.CheckProposal(proposalId);
            Console.Write(
                $"\r[Processing]: ProposalId={proposalId.ToHex()}, " +
                $"ToBeReleased: {proposalInfo.ToBeReleased}, " +
                $"using time:{CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
            checkTimes--;
        }

        Thread.Sleep(10000);
        stopwatch.Stop();
        return proposalInfo.ToBeReleased || proposalInfo.ExpiredTime == null;
    }

    private TransactionResultDto FindReleaseApprovedUserSmartContractMethod(long startBlock, long currentHeight)
    {
        var releaseTransaction = new TransactionResultDto();
        for (var i = startBlock; i < currentHeight; i++)
        {
            var block = AsyncHelper.RunSync(() => _service._nodeManager.ApiClient.GetBlockByHeightAsync(i));
            var transactionList = AsyncHelper.RunSync(() =>
                _service._nodeManager.ApiClient.GetTransactionResultsAsync(block.BlockHash));
            var find = transactionList.Find(
                t => t.Transaction.MethodName.Equals("ReleaseApprovedUserSmartContract"));
            releaseTransaction = find ?? releaseTransaction;
        }

        return releaseTransaction;
    }

    private void ParliamentChangeWhiteList()
    {
        var parliament = _service.ParliamentService;
        var proposalWhiteList =
            parliament.CallViewMethod<ProposerWhiteList>(
                ParliamentMethod.GetProposerWhiteList, new Empty());
        _logger.Info(proposalWhiteList);

        if (proposalWhiteList.Proposers.Contains(_service.GenesisService.Contract))
        {
            _logger.Info("======== Genesis contract is in ProposalWhiteList ========");
            return;
        }

        var defaultAddress = parliament.GetGenesisOwnerAddress();
        var addList = new List<Address>
        {
            _service.GenesisService.Contract
        };
        proposalWhiteList.Proposers.AddRange(addList);

        var changeInput = new ProposerWhiteList
        {
            Proposers = { proposalWhiteList.Proposers }
        };


        var proposalId = parliament.CreateProposal(parliament.ContractAddress,
            nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), changeInput, defaultAddress,
            _service.CallAddress);
        ApproveByMiner(proposalId);

        Thread.Sleep(10000);
        parliament.SetAccount(_service.CallAddress);
        var release = parliament.ReleaseProposal(proposalId, _service.CallAddress);
        proposalWhiteList =
            parliament.CallViewMethod<ProposerWhiteList>(
                ParliamentMethod.GetProposerWhiteList, new Empty());
        _logger.Info(proposalWhiteList);
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

    private ContractOperation GenerateContractOperation(AuthorInfo authorInfo, Hash codeHash, Hash saltHash, int version)
    {
        var chainId = ChainHelper.ConvertBase58ToChainId(_service._nodeManager.GetChainId());
        var actualSigner = authorInfo.isProxyAddress ? authorInfo.Signer : authorInfo.Author;
        var authorPrivateKey = _service._nodeManager.AccountManager.GetPrivateKey(actualSigner);
        var signature = GenerateContractSignature(authorPrivateKey, chainId, codeHash, Address.FromBase58(actualSigner),
            saltHash, version);
        return new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            Deployer = Address.FromBase58(authorInfo.Author),
            Salt = saltHash,
            Version = version,
            Signature = signature 
        };
    }

    private Service _service;
    private ILog _logger;
    
    public class ContractInfoDto {
        public ContractInfoDto(string contractAddress, long height, string version, string author)
        {
            ContractAddress = contractAddress;
            Version = version;
            Author = author;
            Height = height;
        }

        public string ContractAddress { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public long Height { get; set; }
    }
}

