using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Portkey.Contracts.CA;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class CAContractTest_singleNode
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private int _chainId;
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private CAContract _caContract;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private readonly AElfKeyStore _keyStore;

        private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
        private static string RpcUrl { get; } = "192.168.67.18:8000";
        private AuthorityManager AuthorityManager { get; set; }

        private string CA = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("CAContractTest");
            Logger = Log4NetHelper.GetLogger();
            // NodeInfoHelper.SetConfig("nodes-env-main_ca");

            NodeManager = new NodeManager(RpcUrl);

            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenSub = _genesisContract.GetTokenStub(InitAccount);

            if (CA.Equals(""))
                _caContract = new CAContract(NodeManager, InitAccount);
            else
                _caContract = new CAContract(NodeManager, InitAccount, CA);
        }

        [TestMethod]
        public void init()
        {
            // Create account
            var verifierAddress = NodeManager.AccountManager.NewAccount("wanghuan");
            Logger.Info($"verifierAddress:{verifierAddress}");
            var privateKey = NodeManager.AccountManager.GetPrivateKey(verifierAddress);
            Logger.Info($"privateKey:{privateKey}");
            // var verifierAddress = "jPrX7JoKsnoB1sk1pqkrsXg7yB1ZtcVGi6bPZFmzfR7DVgr7f";

            // Initialize
            var result = _caContract.ExecuteMethodWithResult(CAMethod.Initialize, new InitializeInput
            {
                ContractAdmin = InitAccount.ConvertAddress()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Add verifierServer endPoints
            result = _caContract.ExecuteMethodWithResult(CAMethod.AddVerifierServerEndPoints,
                new AddVerifierServerEndPointsInput
                {
                    Name = "Portkey",
                    ImageUrl = "url",
                    EndPoints = { "127.0.0.1" },
                    VerifierAddressList = { verifierAddress.ConvertAddress() }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetVerifierServers()
        {
            // Get verifierServers
            var verifierServers = _caContract.GetVerifierServers();
            Logger.Info($"verifierServers:{verifierServers}");
        }

        [TestMethod]
        public void GetCreatorControllers()
        {
            // Get verifierServers
            var creatorControllers = _caContract.GetCreatorControllers();
            Logger.Info($"creatorControllers:{creatorControllers}");
        }

        // [DataRow("wang@aelf.io")]
        [DataRow("jiping@aelf.io")]
        [TestMethod]
        public void CreateCaHolder(string guardianAccount)
        {
            // Get verifierServers
            var verifierServers = _caContract.GetVerifierServers();
            var verifierAddress = verifierServers.VerifierServers[0].VerifierAddresses[0];
            var verifierId = verifierServers.VerifierServers[0].Id;
            Logger.Info($"verifierId:{verifierId}" +
                        $"\nverifierAddress:{verifierAddress}");

            var verificationTime = DateTime.UtcNow;
            var salt = Guid.NewGuid().ToString("N");
            // var operationType = Convert.ToInt32(OperationType.CreateCaholder).ToString();
            // Create account
            // var privateKey = NodeManager.AccountManager.GetPrivateKey(verifierAddress.ToString());
            var privateKey = "0700e2351c07574749605669d35d2f7075c578a7124881b348dccf7f3987e1fc";
            Logger.Info($"privateKey:{privateKey}");

            // Create manager
            var manager = NodeManager.AccountManager.NewAccount("wanghuan");
            Logger.Info($"manager:{manager}");


            // Create CAHolder
            var signature = GenerateSignature(privateKey.HexToByteArray(), verifierAddress,
                verificationTime, guardianAccount, 0);
            _caContract.SetAccount("ucxifHkQcqob3zL3c2WppfEQn46oADEmm6bJdHsfNgX8ye2tw", "wanghuan");
            var createCAHolder = _caContract.ExecuteMethodWithResult(CAMethod.CreateCAHolder,
                new CreateCAHolderInput
                {
                    GuardianApproved = new GuardianInfo
                    {
                        Type = GuardianType.OfEmail,
                        IdentifierHash = Hash.LoadFromHex(guardianAccount),
                        VerificationInfo = new VerificationInfo
                        {
                            Id = verifierId,
                            Signature = signature,
                            // VerificationDoc =
                            //     $"{0},{guardianAccount},{verificationTime},{verifierAddress},{salt},{}"
                        }
                    },
                    ManagerInfo = new ManagerInfo
                    {
                        Address = manager.ConvertAddress(),
                        ExtraData = "123"
                    },
                    JudgementStrategy = new StrategyNode
                    {
                    }
                });
            createCAHolder.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var holderInfo = _caContract.GetHolderInfoByGuardianType(guardianAccount);
            Logger.Info($"holderInfo:{holderInfo}");
        }

        private ByteString GenerateSignature(byte[] privateKey, Address verifierAddress,
            DateTime verificationTime, string guardianType, int type)
        {
            var data = $"{type},{guardianType},{verificationTime},{verifierAddress.ToBase58()}";
            var dataHash = HashHelper.ComputeFrom(data);
            var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
            return ByteStringHelper.FromHexString(signature.ToHex());
        }

        [TestMethod]
        public void ManagerTransfer()
        {
            var symbol = "ELF";
            var amount = 1;
            var holderInfo = _caContract.GetHolderInfoByGuardianType("huan0@aelf.io");
            var fromUserCaHash = holderInfo.CaHash;
            var fromUserCaAddress = holderInfo.CaAddress.ToBase58();
            var fromUserManagers = holderInfo.ManagerInfos[0].Address.ToBase58();
            Logger.Info($"\nfromUserCaHash:{fromUserCaHash}" +
                        $"\nfromUserCaAddress:{fromUserCaAddress}" +
                        $"\nfromUserManagers:{fromUserManagers}");

            var fromUserCaAddressBalance = _tokenContract.GetUserBalance(fromUserCaAddress, symbol);
            var fromUserBalance = _tokenContract.GetUserBalance(fromUserManagers, symbol);
            Logger.Info($"\nfromUserCaAddressBalance:{fromUserCaAddressBalance}" +
                        $"\nfromUserBalance:{fromUserBalance}");

            if (fromUserBalance <= 100_00000000 || fromUserCaAddressBalance <= 100_00000000)
            {
                var transferResult = _tokenContract.TransferBalance(InitAccount, fromUserCaAddress, 100_00000000);
                transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                transferResult = _tokenContract.TransferBalance(InitAccount, fromUserManagers, 100_00000000);
                transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            for (int i = 1; i <= 5; i++)
            {
                var holderInfoTo = _caContract.GetHolderInfoByGuardianType($"huan{i}@aelf.io");
                Logger.Info($"\nholderInfoTo.CaHash:{holderInfoTo.CaHash}" +
                            $"\nholderInfoTo.CaAddress:{holderInfoTo.CaAddress}" +
                            $"\nholderInfoTo.Managers:{holderInfoTo.ManagerInfos[0]}");

                var input = new ManagerTransferInput
                {
                    CaHash = fromUserCaHash,
                    To = holderInfoTo.CaAddress,
                    Symbol = symbol,
                    Amount = amount,
                    Memo = "ca transfer."
                };

                var txId = NodeManager.SendTransaction(fromUserManagers, _caContract.ContractAddress,
                    nameof(CAMethod.ManagerTransfer), input);
                Logger.Info($"txId{i}:{txId}");
            }
        }

        [TestMethod]
        public void ManagerTransferMulti()
        {
            var symbol = "ELF";
            var amount = 1;

            var guardian1 = "wang1@aelf.io";
            var guardian2 = "wang2@aelf.io";
            var guardian3 = "wang3@aelf.io";
            var guardian4 = "wang4@aelf.io";
            var guardian5 = "wang5@aelf.io";
            var guardian6 = "wang6@aelf.io";
            var user1 = FromUser(guardian1, symbol);
            var user2 = FromUser(guardian2, symbol);
            var user3 = FromUser(guardian3, symbol);

            var holderInfoTo1 = _caContract.GetHolderInfoByGuardianType(guardian4);
            var holderInfoTo2 = _caContract.GetHolderInfoByGuardianType(guardian5);
            var holderInfoTo3 = _caContract.GetHolderInfoByGuardianType(guardian6);

            var input1 = new ManagerTransferInput
            {
                CaHash = user1.CaHash,
                To = holderInfoTo1.CaAddress,
                Symbol = symbol,
                Amount = amount,
                Memo = "ca transfer."
            };
            var input2 = new ManagerTransferInput
            {
                CaHash = user2.CaHash,
                To = holderInfoTo2.CaAddress,
                Symbol = symbol,
                Amount = amount,
                Memo = "ca transfer."
            };
            var input3 = new ManagerTransferInput
            {
                CaHash = user3.CaHash,
                To = holderInfoTo3.CaAddress,
                Symbol = symbol,
                Amount = amount,
                Memo = "ca transfer."
            };

            var txId1 = NodeManager.SendTransaction(user1.ManagerInfos[0].Address.ToBase58(),
                _caContract.ContractAddress,
                nameof(CAMethod.ManagerTransfer), input1);
            var txId2 = NodeManager.SendTransaction(user2.ManagerInfos[0].Address.ToBase58(),
                _caContract.ContractAddress,
                nameof(CAMethod.ManagerTransfer), input2);
            var txId3 = NodeManager.SendTransaction(user3.ManagerInfos[0].Address.ToBase58(),
                _caContract.ContractAddress,
                nameof(CAMethod.ManagerTransfer), input3);
            Logger.Info($"\ntxId1:{txId1}" +
                        $"\ntxId2:{txId2}" +
                        $"\ntxId2:{txId2}");
        }

        private GetHolderInfoOutput FromUser(string guardianType, string symbol)
        {
            var holderInfo = _caContract.GetHolderInfoByGuardianType(guardianType);
            var fromUserCaHash = holderInfo.CaHash;
            var fromUserCaAddress = holderInfo.CaAddress.ToBase58();
            var fromUserManagers = holderInfo.ManagerInfos[0].Address.ToBase58();
            Logger.Info($"\nfromUserCaHash:{fromUserCaHash}" +
                        $"\nfromUserCaAddress:{fromUserCaAddress}" +
                        $"\nfromUserManagers:{fromUserManagers}");

            var fromUserCaAddressBalance = _tokenContract.GetUserBalance(fromUserCaAddress, symbol);
            var fromUserBalance = _tokenContract.GetUserBalance(fromUserManagers, symbol);
            Logger.Info($"\nfromUserCaAddressBalance:{fromUserCaAddressBalance}" +
                        $"\nfromUserBalance:{fromUserBalance}");

            if (fromUserBalance <= 100_00000000 || fromUserCaAddressBalance <= 100_00000000)
            {
                var transferResult = _tokenContract.TransferBalance(InitAccount, fromUserCaAddress, 100_00000000);
                transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                transferResult = _tokenContract.TransferBalance(InitAccount, fromUserManagers, 100_00000000);
                transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            return holderInfo;
        }

        [TestMethod]
        public void NewAccount()
        {
            var user = NodeManager.AccountManager.NewAccount("wanghuan");

            var privateKey = NodeManager.AccountManager.GetPrivateKey(user);
            Logger.Info($"user:{user}" +
                        $"\nprivateKey:{privateKey.ToHex()}");
        }
    }
}