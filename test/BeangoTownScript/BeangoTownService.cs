using System.Text;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Contracts.BeangoTownContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json.Linq;
using Portkey.Contracts.CA;
using Shouldly;
using Volo.Abp.Threading;

namespace BeangoTownScript;

public class BeangoTownService
{
    public BeangoTownService(Service serviceMain, Service serviceSide, string beangoTownServerUrl,
        ILog logger,
        string bridgeAddress, string caAddressMain, string caAddressSide, string creatorController)
    {
        _service = serviceSide;
        _getBlockHeightServer = beangoTownServerUrl;
        _logger = logger;
        _beangoTown = new BeangoTownContract(serviceSide.NodeManager, serviceSide.CallAddress, bridgeAddress);
        _caContractMain = new CAContract(serviceMain.NodeManager, serviceMain.CallAddress, caAddressMain);
        _caContractSide = new CAContract(serviceSide.NodeManager, serviceSide.CallAddress, caAddressSide);
        _creatorController = creatorController;
        _httpClient = new HttpClient();
    }

    public string GetCaAccount()
    {
        var randomAccount = RandomAccount();
        var guardian = randomAccount.Item1;
        var caManager = randomAccount.Item2;
        var caHolderInfo = CreateCaHolderSideChain(guardian, caManager);
        var caAccountSide = caHolderInfo.CaAddress.ToBase58();

        TransferAccount(caAccountSide, "ELF");
        TransferAccount(caManager, "ELF");
        TransferAccountNft(caAccountSide, "BEANPASS-1");

        return guardian;
    }
    
    public string GetCaAccount1()
    {
        var randomAccount = RandomAccount();
        var guardian = randomAccount.Item1;
        var caManager = randomAccount.Item2;
        CreateCaHolderMainChain(guardian, caManager);

        return guardian;
    }

    public Task BeangoTownGoOnePerson()
    {
        var guardian = GetCaAccount();

        var caHolderInfo = GetHolderInfoSide(guardian);
        var manager = caHolderInfo.ManagerInfos[0].Address.ToBase58();
        var caAddress = caHolderInfo.CaAddress.ToBase58();
        var caHash = caHolderInfo.CaHash;

        _logger.Info($"startGo-caAddress:{caAddress}");
        for (int i = 0; i < 20; i++)
        {
            _logger.Info($"startGoOnce:{i}-caAddress:{caAddress}");
            BeangoTownGo1(i, manager, caAddress, caHash);
            _logger.Info($"endGoOnce:{i}-caAddress:{caAddress}");
        }
        _logger.Info($"endGo-caAddress:{caAddress}");

        return Task.CompletedTask;
    }

    public void BeangoTownGo(int time, string manager, string caAddress, Hash caHash)
    {
        _logger.Info($"startPlay-caAddress:{caAddress}-{time}");
        var txId = Play(time, manager, caHash);
        _logger.Info($"endPlay-caAddress:{caAddress}-{time}");

        _logger.Info($"startGetBoutInformation-caAddress:{caAddress}-{time}");
        var expectedBlockHeight = GetBoutInformation(txId).ExpectedBlockHeight;
        _logger.Info($"endGetBoutInformation-caAddress:{caAddress}-{time}");

        _logger.Info($"startGetApiBlockHeight-caAddress:{caAddress}-{time}");
        Thread.Sleep(4000);
        var blockHeight = AsyncHelper.RunSync(GetApiBlockHeight);

        do
        {
            Thread.Sleep(200);
            blockHeight = AsyncHelper.RunSync(GetApiBlockHeight);
            _logger.Info($"expectedBlockHeight:{expectedBlockHeight}" +
                         $"\nblockHeight:{blockHeight}");
        } while (expectedBlockHeight > blockHeight);

        _logger.Info($"endGetApiBlockHeight-caAddress:{caAddress}-{time}");

        _logger.Info($"startBingo-caAddress:{caAddress}-{time}");
        Bingo(manager, caHash, txId);
        _logger.Info($"endBingo-caAddress:{caAddress}-{time}");

        _logger.Info($"startGetPlayerInformation-caAddress:{caAddress}-{time}");
        GetPlayerInformation(caAddress);
        _logger.Info($"endGetPlayerInformation-caAddress:{caAddress}-{time}");
    }

    public void BeangoTownGo1(int time, string manager, string caAddress, Hash caHash)
    {
        _logger.Info($"startBingoNew-caAddress:{caAddress}-{time}");
        var txId = BingoNew(time, manager, caHash, caAddress);
        _logger.Info($"endBingoNew-caAddress:{caAddress}-{time}");
        // Thread.Sleep(500);

        // _logger.Info($"startGetBoutInformation-caAddress:{caAddress}-{time}");
        // var result = GetBoutInformation(txId);
        // Thread.Sleep(result.GridNum * 1000);
        // _logger.Info($"endGetBoutInformation-caAddress:{caAddress}-{time},{result}");
    
    //     _logger.Info($"startGetPlayerInformation-caAddress:{caAddress}-{time}");
    //     GetPlayerInformation(caAddress);
    //     _logger.Info($"endGetPlayerInformation-caAddress:{caAddress}-{time}");
    }

    private string Play(int time, string manager, Hash caHash)
    {
        var input = new ManagerForwardCallInput
        {
            CaHash = caHash,
            ContractAddress = _beangoTown.Contract,
            MethodName = nameof(BeangoTownContractContainer.BeangoTownContractStub.Play),
            Args = new PlayInput
            {
                ResetStart = (time % 2 == 0) ? true : false
            }.ToBytesValue().Value
        };

        var txId = _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            CAMethod.ManagerForwardCall.ToString(), input);
        Thread.Sleep(500);
        var result = _service.NodeManager.CheckTransactionResult(txId);
        var times = 5;
        while (result == new TransactionResultDto() || times == 0)
        {
            // txId = _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            //     CAMethod.ManagerForwardCall.ToString(), input);
            Thread.Sleep(500);
            result = _service.NodeManager.CheckTransactionResult(txId);
            times--;
        }

        _logger.Info($"Play: {txId}");

        return txId;
    }

    private void Bingo(string manager, Hash caHash, string playTxId)
    {
        var input = new ManagerForwardCallInput
        {
            CaHash = caHash,
            ContractAddress = _beangoTown.Contract,
            MethodName = nameof(BeangoTownContractContainer.BeangoTownContractStub.Bingo),
            Args = Hash.LoadFromHex(playTxId).ToBytesValue().Value
        };

        var txId = _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            CAMethod.ManagerForwardCall.ToString(), input);
        Thread.Sleep(500);
        var result = _service.NodeManager.CheckTransactionResult(txId);
        var times = 5;
        while (result == new TransactionResultDto() || times == 0)
        {
            // txId =  _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            //     CAMethod.ManagerForwardCall.ToString(), input);
            Thread.Sleep(500);
            result = _service.NodeManager.CheckTransactionResult(txId);
            times--;
        }

        _logger.Info($"Bingo: {txId}");
    }

    private string BingoNew(int time, string manager, Hash caHash, string caAddress)
    {
        var input = new ManagerForwardCallInput
        {
            CaHash = caHash,
            ContractAddress = _beangoTown.Contract,
            MethodName = nameof(BeangoTownContractContainer.BeangoTownContractStub.Play),
            Args = new PlayInput
            {
                ResetStart = (time % 2 == 0) ? true : false,
                DiceCount = 3,
                ExecuteBingo = true
            }.ToBytesValue().Value
        };

        var txId = _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            CAMethod.ManagerForwardCall.ToString(), input);
        _logger.Info($"SendtxId-BingoNew-caAddress:{caAddress}-{time},BingoNew-txId:{txId}");
        Thread.Sleep(500);
        var result = _service.NodeManager.CheckTransactionResult(txId);
        var times = 5;
        while (result == new TransactionResultDto() || times == 0)
        {
            // txId =  _service.NodeManager.SendTransaction(manager, _caContractSide.ContractAddress,
            //     CAMethod.ManagerForwardCall.ToString(), input);
            Thread.Sleep(500);
            result = _service.NodeManager.CheckTransactionResult(txId);
            times--;
        }

        _logger.Info($"ChecktxId-BingoNew-caAddress:{caAddress}-{time},BingoNew-txId:{txId}");
        return txId;
    }

    public BoutInformation GetBoutInformation(string playId)
    {
        var boutInformation = _beangoTown.CallViewMethod<BoutInformation>(BeangoTownMethod.GetBoutInformation,
            new GetBoutInformationInput
            {
                PlayId = Hash.LoadFromHex(playId)
            });
        var times = 2;
        while (boutInformation.Equals(new BoutInformation()) || times == 0)
        {
            Thread.Sleep(500);
            _beangoTown.CallViewMethod<BoutInformation>(BeangoTownMethod.GetBoutInformation,
                new GetBoutInformationInput
                {
                    PlayId = Hash.LoadFromHex(playId)
                });
            times--;
        }
        _logger.Info($"boutInformation: {boutInformation}");

        return boutInformation;
    }

    public void GetPlayerInformation(string account)
    {
        var playerInformation =
            _beangoTown.CallViewMethod<BoutInformation>(BeangoTownMethod.GetPlayerInformation,
                account.ConvertAddress());
        _logger.Info($"caAddress:{account}-playerInformation:{playerInformation}");
    }

    public void CreateCaHolderMainChain(string guardian, string manager)
    {
        var privateKey = "fc84b51a88e2cb8797a5bb7e9740c3eda7ffd9f8d73efb2827817b2f7f55cf82";

        var _guardian = HashHelper.ComputeFrom(guardian);

        // Get verifierServers
        var verifierServers = _caContractMain.GetVerifierServers();
        var verifierAddress = verifierServers.VerifierServers[0].VerifierAddresses[0];
        var verifierId = verifierServers.VerifierServers[0].Id;

        var verificationTime = DateTime.UtcNow;
        var salt = Guid.NewGuid().ToString("N");
        var operationType = Convert.ToInt32(OperationType.CreateCaholder).ToString();

        var signature = GenerateSignature(privateKey.HexToByteArray(), verifierAddress, verificationTime, _guardian,
            0, salt,
            operationType);

        var guardianInfo = new GuardianInfo
        {
            Type = GuardianType.OfEmail,
            IdentifierHash = _guardian,
            VerificationInfo = new VerificationInfo
            {
                Id = verifierId,
                Signature = signature,
                VerificationDoc =
                    $"{0},{_guardian.ToHex()},{verificationTime},{verifierAddress.ToBase58()},{salt},{operationType}"
            }
        };

        _caContractMain.SetAccount(_creatorController, "12345678");
        var createCAHolder = _caContractMain.ExecuteMethodWithResult(CAMethod.CreateCAHolder,
            new CreateCAHolderInput
            {
                GuardianApproved = guardianInfo,
                ManagerInfo = new ManagerInfo
                {
                    Address = manager.ConvertAddress(),
                    ExtraData = "123"
                }
            });
        createCAHolder.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    public GetHolderInfoOutput CreateCaHolderSideChain(string guardian, string manager)
    {
        var privateKey = "fc84b51a88e2cb8797a5bb7e9740c3eda7ffd9f8d73efb2827817b2f7f55cf82";

        var _guardian = HashHelper.ComputeFrom(guardian);

        // Get verifierServers
        var verifierServers = _caContractSide.GetVerifierServers();
        var verifierAddress = verifierServers.VerifierServers[0].VerifierAddresses[0];
        var verifierId = verifierServers.VerifierServers[0].Id;

        var verificationTime = DateTime.UtcNow;
        var salt = Guid.NewGuid().ToString("N");
        var operationType = Convert.ToInt32(OperationType.CreateCaholder).ToString();

        var signature = GenerateSignature(privateKey.HexToByteArray(), verifierAddress, verificationTime, _guardian,
            0, salt,
            operationType);

        var guardianInfo = new GuardianInfo
        {
            Type = GuardianType.OfEmail,
            IdentifierHash = _guardian,
            VerificationInfo = new VerificationInfo
            {
                Id = verifierId,
                Signature = signature,
                VerificationDoc =
                    $"{0},{_guardian.ToHex()},{verificationTime},{verifierAddress.ToBase58()},{salt},{operationType}"
            }
        };

        _caContractSide.SetAccount(_creatorController);
        var createCAHolder = _caContractSide.ExecuteMethodWithResult(CAMethod.CreateCAHolder,
            new CreateCAHolderInput
            {
                GuardianApproved = guardianInfo,
                ManagerInfo = new ManagerInfo
                {
                    Address = manager.ConvertAddress(),
                    ExtraData = "123"
                }
            });
        createCAHolder.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var caInfo = GetHolderInfoSide(guardian);
        return caInfo;
    }

    public GetHolderInfoOutput GetHolderInfo(string guardian)
    {
        var _guardian = HashHelper.ComputeFrom(guardian);

        var caInfo = _caContractMain.GetHolderInfo(_guardian);
        _logger.Info($"caInfo:{caInfo}");
        caInfo.ManagerInfos.Count.ShouldBe(1);
        caInfo.GuardianList.Guardians.Count.ShouldBe(1);

        return caInfo;
    }

    public GetHolderInfoOutput GetHolderInfoSide(string guardian)
    {
        var _guardian = HashHelper.ComputeFrom(guardian);

        var caInfo = _caContractSide.GetHolderInfo(_guardian);
        _logger.Info($"caInfo:{caInfo}");

        return caInfo;
    }

    private async Task<long> GetApiBlockHeight()
    {
        try
        {
            var response =
                await _httpClient.GetAsync(_getBlockHeightServer);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseBody);

            var data = (int?)responseObject["data"];
            if (data.HasValue)
            {
                long actualData = data.Value;
                Console.WriteLine("data: " + actualData);
                return actualData;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Response Failed：{ex.Message}");
        }

        return 0;
    }

    private ByteString GenerateSignature(byte[] privateKey, Address verifierAddress,
        DateTime verificationTime, Hash guardianType, int type, string salt, string operationType)
    {
        if (string.IsNullOrWhiteSpace(salt))
        {
            salt = "salt";
        }

        var data =
            $"{type},{guardianType.ToHex()},{verificationTime},{verifierAddress.ToBase58()},{salt},{operationType}";
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(ByteExtensions.ToHex(signature));
    }

    public (string, string) RandomAccount()
    {
        Random random = new Random();
        StringBuilder stringBuilder = new StringBuilder();
        for (int j = 0; j < 10; j++)
        {
            char letter = (char)random.Next(0, 9);
            stringBuilder.Append(letter);
        }

        var randomNumber = stringBuilder.ToString();

        DateTimeOffset now = DateTimeOffset.Now;
        long timestamp = now.ToUnixTimeSeconds();

        var guardian = timestamp + randomNumber + "@aelf.io";
        var account = _service.NodeManager.NewAccount();
        _logger.Info($"guardian:{guardian}" +
                     $"\naccount:{account}");

        return (guardian, account);
    }

    public void TransferAccount(string account, string symbol)
    {
        _logger.Info("Prepare chain basic token for tester.");
        var token = _service.TokenService;
        var txIds = new List<string>();

        var userBalance = token.GetUserBalance(account, symbol);
        if (userBalance > 10000000)
            return;
        token.SetAccount(_service.CallAddress);
        var txId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
        {
            To = account.ConvertAddress(),
            Amount = 10000000,
            Symbol = symbol,
            Memo = $"T-{Guid.NewGuid()}"
        });
        txIds.Add(txId);

        token.NodeManager.CheckTransactionListResult(txIds);
    }

    public void TransferAccountNft(string account, string symbol)
    {
        _logger.Info("Prepare chain basic nft for tester.");
        var token = _service.TokenService;
        var txIds = new List<string>();

        var userBalance = token.GetUserBalance(account, symbol);
        if (userBalance == 1)
            return;
        token.SetAccount(_service.CallAddress);
        var txId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
        {
            To = account.ConvertAddress(),
            Amount = 1,
            Symbol = symbol,
            Memo = $"T-{Guid.NewGuid()}"
        });
        txIds.Add(txId);

        token.NodeManager.CheckTransactionListResult(txIds);
    }

    private void methodStartLog(string method)
    {
        using (StreamWriter writer = File.AppendText(logFilePath))
        {
            writer.WriteLine($"{method} start:{DateTime.Now}");
        }
    }

    private void methodEndLog(string method)
    {
        using (StreamWriter writer = File.AppendText(logFilePath))
        {
            writer.WriteLine($"{method} end:{DateTime.Now}");
        }
    }

    private Service _service;
    private string _getBlockHeightServer;
    private ILog _logger;
    private BeangoTownContract _beangoTown;
    private CAContract _caContractMain;
    private CAContract _caContractSide;
    private string _creatorController;
    static object lockObj = new object();
    string logFilePath = "log.txt";
    private HttpClient _httpClient;

    private new Queue<List<Dictionary<string, List<string>>>> QueueTransaction { get; set; }
}