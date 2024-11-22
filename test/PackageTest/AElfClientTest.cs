
using System.Net.Http.Headers;
using System.Text;
using AElf;
using AElf.Client;
using AElf.Cryptography;
using AElf.Types;
using AElf.Client.Dto;
using AElf.Client.Extensions;
using AElf.Client.Service;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using AElf.Contracts.MultiToken;
using Volo.Abp.Threading;
using StringExtensions = AElf.Client.Extensions.StringExtensions;

namespace PackageTest;

[TestClass]
public class AElfClientTest
{
    private AElfClient Client;
    private AElfClient SideClient;
    
    private IHttpService _httpService;
    private const string BaseUrl = "http://127.0.0.1:8000";
    private const string SideChainUrl = "http://127.0.0.1:8001";
    private const string UserName = "aelf";
    private const string Password = "12345678";
    private const string InitAccount = "Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk";
    private const string TestAccount = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
    private string _genesisAddress;
    private string GenesisAddress => GetGenesisContractAddress();

    private const string InitPublicKey =
        "046ca9986b8a1f48fe1c632fa03733795f5d0c196c48bb372a6eb2709a57d0959804ae55951c8f833d551e02cb48f107bfd56c18752d5886a6ec9336e3d75a4a53";

    private const string InitPrivateKey = "";
    private ILog Logger { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        Log4NetHelper.LogInit("AElfClientTest");
        Logger = Log4NetHelper.GetLogger();
        Client = new AElfClient(BaseUrl, userName: UserName, password: Password);
        SideClient = new AElfClient(SideChainUrl, userName: UserName, password: Password);
        _httpService = new HttpService(60);
    }

    [TestMethod]
    [DataRow("/api/net/networkInfo")]
    public async Task TestGetResponseAsync(string uri)
    {
        var result = await _httpService.GetResponseAsync<NetworkInfoOutput>(BaseUrl+uri);
        result.ProtocolVersion.ShouldBe(1);
    }

    [TestMethod]
    public async Task TestPostResponseAsync()
    {
        var url = BaseUrl + "/api/blockChain/calculateTransactionFee";
        var signedTx= TestTransactionBuild();
        var rawTransaction = signedTx.ToByteArray().ToHex();
        var parameters = new Dictionary<string, string>
        {
            {"RawTransaction", rawTransaction}
        };
        var result = await _httpService.PostResponseAsync<CalculateTransactionFeeOutput>(url, parameters);
        Logger.Info(result);
        result.ShouldNotBeNull();
        
        // string uri = "/api/net/peer";
        // var parameters = new Dictionary<string, string>
        // {
        //     {"address", "http://127.0.0.1:8001"}
        // };
        // try
        // {
        //     var result = await _httpService.PostResponseAsync<bool>(BaseUrl + uri, parameters, authenticationHeaderValue: GetAuthenticationHeaderValue());
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e.Message);
        //     throw;
        // }
    }

    [TestMethod]
    [DataRow("Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk")]
    public void TestToAddress(string address)
    {
        var result = StringExtensions.ToAddress(address);
        var type = result.Value.GetType();
        type.ShouldBe(typeof(Google.Protobuf.ByteString));
    }

    [TestMethod]
    public void TestGetTransactionFees()
    {
        var transactionResultDto = new TransactionResultDto
        {
            Logs = new[]
            {
                new LogEventDto
                {
                    Name = "TransactionFeeCharged",
                    NonIndexed = "CgNFTEYQwKbhDA=="
                }
            }
        };
    
        var transactionFees = transactionResultDto.GetTransactionFees();
        Logger.Info(transactionFees);
    
        transactionResultDto = new TransactionResultDto();
        transactionFees = transactionResultDto.GetTransactionFees();
        transactionFees.Count.ShouldBe(0);
    }

    [TestMethod]
    public void TestDispose()
    {
        Client.Dispose();
    }

    [TestMethod]
    public async Task TestBlock()
    {
        var blockHeight = await Client.GetBlockHeightAsync();
        blockHeight.GetType().ShouldBe(typeof(System.Int64));
        var block = await Client.GetBlockByHeightAsync(blockHeight);
        block.GetType().ShouldBe(typeof(AElf.Client.Dto.BlockDto));
        var blockByHash = await Client.GetBlockByHashAsync(block.BlockHash);
        Logger.Info(block.BlockHash);
        blockByHash.GetType().ShouldBe(typeof(AElf.Client.Dto.BlockDto));
        Logger.Info(blockByHash.Header.Height);
    }

    [TestMethod]
    public async Task TestGetChainStatusAsync()
    {
        var chainStatus = await Client.GetChainStatusAsync();
        chainStatus.GetType().ShouldBe(typeof(AElf.Client.Dto.ChainStatusDto));
        chainStatus.ChainId.ShouldBe("AELF");
    }

    [TestMethod]
    [DataRow("JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE")]
    public async Task TestGetContractFileDescriptorSetAsync(string address)
    {
        var set  = await Client.GetContractFileDescriptorSetAsync(address);
        Logger.Info(set.GetType());
    }

    [TestMethod]
    public async Task TestGetContractFileDescriptorSetAsync_Fail()
    {
        try
        {
            await Client.GetContractFileDescriptorSetAsync("123");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            e.Message.ShouldBe("Invalid address format");
        }
    }

    [TestMethod]
    public async Task TestGetTaskQueueStatusAsync()
    {
        var taskQueueStatus = await Client.GetTaskQueueStatusAsync();
        taskQueueStatus[0].GetType().ShouldBe(typeof(AElf.Client.Dto.TaskQueueInfoDto));
        var queueStatus = JsonConvert.SerializeObject(taskQueueStatus, Formatting.Indented);
        Logger.Info(queueStatus);
        taskQueueStatus[0].Name.ShouldBe("PeerReconnectionQueue");
    }

    [TestMethod]
    public async Task TestGetChainIdAsync()
    {
        var chainId = await Client.GetChainIdAsync();
        Logger.Info(chainId);
        chainId.ShouldBe(9992731);
    }

    [TestMethod]
    public async Task TestIsConnectedAsync()
    {
        var isConnected = await Client.IsConnectedAsync();
        isConnected.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task TestIsConnectedAsync_Fail()
    {
        var isConnected = await Client.IsConnectedAsync();
        isConnected.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TestGetGenesisContractAddressAsync()
    {
        var genesisAddress = await Client.GetGenesisContractAddressAsync();
        Logger.Info(genesisAddress);
        var chainStatus = await Client.GetChainStatusAsync();
        genesisAddress.ShouldBe(chainStatus.GenesisContractAddress);
    }

    [TestMethod]
    public async Task TestGenerateTransactionAsync()
    {
        var fromAddress = InitAccount;
        var toAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var methodName = "GetPrimaryTokenSymbol";
        var transaction = await GenerateTransaction(fromAddress, toAddress, methodName);
        Logger.Info(transaction);
        transaction.MethodName.ShouldBe(methodName);
    }

    [TestMethod]
    public async Task TestGenerateTransactionAsync_Fail()
    {
        try
        {
            var tokenContractAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
            var fromAddress = InitAccount;
            var toAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE".ToAddress().ToBase58();
            var methodName = "GetPrimaryTokenSymbol";
            var param = new Empty();
            await Client.GenerateTransactionAsync(fromAddress, toAddress, methodName, param);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [TestMethod]
    [DataRow("AElf.ContractNames.Token", "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE")]
    public async Task TestGetContractAddressByNameAsync(string contractName, string expectAddress)
    {
        var contractAddress = await Client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(contractName));
        Logger.Info(contractAddress);
        contractAddress.ShouldBe(expectAddress.ToAddress());
    }

    [TestMethod]
    [DataRow(InitAccount)]
    public async Task TestGetFormattedAddressAsync_MainChain(string address)
    {
        string formattedAddress = await Client.GetFormattedAddressAsync(address.ToAddress());
        Logger.Info(formattedAddress);
        formattedAddress.ShouldBe("ELF_" + address + "_AELF");
    }
    
    [TestMethod]
    [DataRow(InitAccount)]
    public async Task TestGetFormattedAddressAsync_SideChain(string address)
    {
        string formattedAddress = await SideClient.GetFormattedAddressAsync(address.ToAddress());
        Logger.Info(formattedAddress);
        formattedAddress.ShouldBe("ELF_" + address + "_tDVV");
    }

    [TestMethod]
    public async Task<Transaction> TestSignTransactionWithString()
    {
        string privateKeyHex = "";
        var fromAddress = InitAccount;
        var toAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var methodName = "GetPrimaryTokenSymbol";
        var transaction = await GenerateTransaction(fromAddress, toAddress, methodName);
        var signedTransaction = Client.SignTransaction(privateKeyHex, transaction);
        signedTransaction.MethodName.ShouldBe(methodName);
        return signedTransaction;
    }

    [TestMethod]
    public async Task TestSignTransactionWithByteArray()
    {
        var fromAddress = InitAccount;
        var toAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var methodName = "GetPrimaryTokenSymbol";
        var transaction = await GenerateTransaction(fromAddress, toAddress, methodName);
        string privateKeyHex = "";
        var privateKey = ByteArrayHelper.HexStringToByteArray(privateKeyHex);
        var signedTransaction = Client.SignTransaction(privateKey, transaction);
        signedTransaction.MethodName.ShouldBe(methodName);
        var rawTransaction = signedTransaction.ToByteArray().ToHex();
        Logger.Info(rawTransaction);
        var result = await Client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = rawTransaction
        });
        Logger.Info(result.TransactionId);
        result.TransactionId.ShouldNotBeEmpty();
    }

    [TestMethod]
    [DataRow(InitPublicKey, InitAccount)]
    public void TestGetAddressFromPubKey(string publicKey, string expectAddress)
    {
        var address = Client.GetAddressFromPubKey(publicKey);
        Logger.Info(address);
        address.ShouldBe(expectAddress);
    }
    
    [TestMethod]
    [DataRow(InitPrivateKey, InitAccount)]
    public void TestGetAddressFromPrivateKey(string privateKey, string expectAddress)
    {
        var address = Client.GetAddressFromPrivateKey(privateKey);
        Logger.Info(address);
        address.ShouldBe(expectAddress);
    }

    [TestMethod]
    public void TestGenerateKeyPairInfo()
    {
        var keyPairInfo = Client.GenerateKeyPairInfo();
        keyPairInfo.GetType().ShouldBe(typeof(AElf.Client.Model.KeyPairInfo));
        keyPairInfo.PublicKey.ShouldNotBeEmpty();
        keyPairInfo.PrivateKey.ShouldNotBeEmpty();
        keyPairInfo.Address.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task TestAddPeerAsync()
    {
        var addressToAdd = "127.0.0.1:6801";

        var addSuccess = await Client.AddPeerAsync(addressToAdd, UserName, Password);
        addSuccess.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task TestAddPeerAsync_Fail()
    {
        var addressToAdd = "127";

        var addSuccess = await Client.AddPeerAsync(addressToAdd, UserName, Password);
        addSuccess.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TestRemovePeerAsync()
    {
        var addressToRemove = "127.0.0.1:6801";
        var removeResult = await Client.RemovePeerAsync(addressToRemove, UserName, Password);
        removeResult.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TestRemovePeerAsync_Fail()
    {
        var addressToRemove = "127";
        var removeResult = await Client.RemovePeerAsync(addressToRemove, UserName, Password);
        removeResult.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TestGetPeersAsync()
    {
        var peerList = await Client.GetPeersAsync(false);
        Logger.Info(peerList);
        if (peerList.Count == 1)
        {
            peerList[0].IpAddress.ShouldBe("127.0.0.1:6801");
        }
    }

    [TestMethod]
    public async Task TestGetNetworkInfoAsync()
    {
        var networkInfo = await Client.GetNetworkInfoAsync();
        networkInfo.ProtocolVersion.ShouldBe(1);
        networkInfo.Version.ShouldBe("1.0.0.0");
    }

    [TestMethod]
    public async Task TestGetTransactionPoolStatusAsync()
    {
        var poolStatus = await Client.GetTransactionPoolStatusAsync();
        poolStatus.Queued.ShouldBe(0);
        poolStatus.Validated.ShouldBe(0);
    }

    [TestMethod]
    public async Task TestExecuteTransactionAsync()
    {
        var signedTx = TestTransactionBuild();
        var rawTransaction = signedTx.ToByteArray().ToHex();
        var transactionResult = await Client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = rawTransaction
        });
        Logger.Info(rawTransaction);
        Logger.Info(transactionResult);
        transactionResult.ShouldNotBeNull();
        transactionResult.ShouldBe("0a03454c46");
    }

    [TestMethod]
    public async Task TestExecuteTransactionAsync_Fail()
    {
        try
        {
            var transactionResult = await Client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = null
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [TestMethod]
    public async Task<CreateRawTransactionOutput> TestCreateRawTransactionAsync()
    {
        var address = GenesisAddress;
        var status = await Client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;
        var createRaw = await Client.CreateRawTransactionAsync(new CreateRawTransactionInput
        {
            From = InitAccount,
            To = address,
            MethodName = "GetContractAddressByName",
            Params = new JObject
            {
                ["value"] = HashHelper.ComputeFrom("AElf.ContractNames.Token").Value.ToBase64()
            }.ToString(),
            RefBlockNumber = height,
            RefBlockHash = blockHash
        });
        Logger.Info(createRaw.RawTransaction);
        createRaw.RawTransaction.ShouldNotBeEmpty();
        return createRaw;
    }

    [TestMethod]
    public async Task TestExecuteRawTransactionAsync()
    {
        var createRaw = TestCreateRawTransactionAsync().Result;
        var transactionId = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(createRaw.RawTransaction));
        var signature = GetSignatureWith(InitPrivateKey, transactionId.ToByteArray()).ToHex();
        Logger.Info(createRaw.RawTransaction);
        Logger.Info(signature);
        var rawTransactionResult = await Client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
        {
            RawTransaction = createRaw.RawTransaction,
            Signature = signature
        });
        Logger.Info("rawTransactionResult:" + rawTransactionResult);
        rawTransactionResult.ShouldBe("\"JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE\"");
    }

    [TestMethod]
    public async Task TestSendRawTransactionAsync()
    {
        var createRaw = TestCreateRawTransactionAsync().Result;
        var transactionId = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(createRaw.RawTransaction));
        var signature = GetSignatureWith(InitPrivateKey, transactionId.ToByteArray()).ToHex();
        var rawTransactionResult = await Client.SendRawTransactionAsync(new SendRawTransactionInput
        {
            Transaction = createRaw.RawTransaction,
            Signature = signature,
            ReturnTransaction = true
        });
        Logger.Info(rawTransactionResult);
        rawTransactionResult.TransactionId.ShouldBe(transactionId.ToHex());
    }
    
    [TestMethod]
    public async Task TestSendTransactionAsync()
    {
        var toAddress = GenesisAddress;
        var methodName = "GetContractAddressByName";
        var param = HashHelper.ComputeFrom("AElf.ContractNames.Vote");

        var transaction = await Client.GenerateTransactionAsync(InitAccount, toAddress, methodName, param);
        var txWithSign = Client.SignTransaction(InitPrivateKey, transaction);
        var rawTransaction = txWithSign.ToByteArray().ToHex();
        Logger.Info(rawTransaction);
        var result = await Client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = rawTransaction
        });
        Logger.Info(result.TransactionId);
        result.TransactionId.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task TestSendTransactionsAsync()
    {
        var toAddress = GenesisAddress;
        var methodName = "GetContractAddressByName";
        var param1 = HashHelper.ComputeFrom("AElf.ContractNames.Token");
        var param2 = HashHelper.ComputeFrom("AElf.ContractNames.Vote");

        var parameters = new List<Hash> {param1, param2};
        var sb = new StringBuilder();

        foreach (var param in parameters)
        {
            var tx = await Client.GenerateTransactionAsync(InitAccount, toAddress, methodName, param);
            var txWithSign = Client.SignTransaction(InitPrivateKey, tx);
            var rawTransaction = txWithSign.ToByteArray().ToHex();
            Logger.Info(rawTransaction);
            sb.Append(txWithSign.ToByteArray().ToHex() + ',');
        }

        var result = await Client.SendTransactionsAsync(new SendTransactionsInput
        {
            RawTransactions = sb.ToString().Substring(0, sb.Length - 1)
        });
        Logger.Info(result);
        result.Length.ShouldBe(2);
    }

    [TestMethod]
    [DataRow("64540153b78cc132f7904ee0d99aec53db567b8e69c13517d5d3914ddee299aa")]
    public async Task TestGetTransactionResultAsync(string txId)
    {
        var txResult = await Client.GetTransactionResultAsync(txId);
        txResult.TransactionId.ShouldBe(txId);
        txResult.Status.ShouldBe("MINED");
    }

    [TestMethod]
    [DataRow("64540153b78cc132f7904ee0d99aec53db567b8e69c13517d5d3914ddee299aa")]
    public async Task TestGetMerklePathByTransactionIdAsync(string txId)
    {
        var merklePath = await Client.GetMerklePathByTransactionIdAsync(txId);
        merklePath.GetType().ShouldBe(typeof(AElf.Client.Dto.MerklePathDto));
        merklePath.MerklePathNodes.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task TestCalculateTransactionFeeAsync()
    {
        var signedTx= TestTransactionBuild();
        var rawTransaction = signedTx.ToByteArray().ToHex();
        var input = new CalculateTransactionFeeInput
        {
            RawTransaction = rawTransaction
        };
        Logger.Info(rawTransaction);
        var txFee = await Client.CalculateTransactionFeeAsync(input);
        Logger.Info(txFee.TransactionFee);
        txFee.TransactionFee.Values.ToList()[0].ShouldBe(21635000);
    }

    [TestMethod]
    public Transaction TestTransactionBuild()
    {
        var methodName = "GetPrimaryTokenSymbol";
        var contractAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var transactionBuilder = new TransactionBuilder(Client);
        transactionBuilder.UsePrivateKey(ByteArrayHelper.HexStringToByteArray(InitPrivateKey));
        transactionBuilder.UseContract(contractAddress);
        transactionBuilder.UseMethod(methodName);
        transactionBuilder.UseParameter(new Empty());
        var signedTx = transactionBuilder.Build();
        Logger.Info(signedTx);
        signedTx.GetType().ShouldBe(typeof(Transaction));
        signedTx.MethodName.ShouldBe(methodName);
        signedTx.To.ShouldBe(contractAddress.ToAddress());
        return signedTx;
    }
    
    private AuthenticationHeaderValue GetAuthenticationHeaderValue()
    {
        var byteArray = Encoding.ASCII.GetBytes($"{UserName}:{Password}");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }
    
    private async Task<Transaction> GenerateTransaction(string fromAddress, string toAddress, string methodName, IMessage input = null)
    {
        if (input == null)
        {
            input = new Empty();
        }
        var transaction = await Client.GenerateTransactionAsync(fromAddress, toAddress.ToAddress().ToBase58(), methodName, input);
        Logger.Info(transaction);
        transaction.MethodName.ShouldBe(methodName);
        return transaction;
    }
    
    private string GetGenesisContractAddress()
    {
        if (_genesisAddress != null) return _genesisAddress;

        var statusDto = AsyncHelper.RunSync(Client.GetChainStatusAsync);
        _genesisAddress = statusDto.GenesisContractAddress;

        return _genesisAddress;
    }
    
    private ByteString GetSignatureWith(string privateKey, byte[] txData)
    {
        // Sign the hash
        var signature = CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), txData);
        return ByteString.CopyFrom(signature);
    }
    
}


