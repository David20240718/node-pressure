using System.Text;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Volo.Abp.Threading;
using TokenContract = AElfChain.Common.Contracts.TokenContract;

namespace AElfChain.Common.Managers
{
    public class CrossChainManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly GenesisContract _fromChainGenesis;
        private readonly GenesisContract _toChainGenesis;

        private NodesInfo _info;

        public CrossChainManager(INodeManager fromNoeNodeManager, INodeManager toChainNodeManager, string caller = "")
        {
            FromNoeNodeManager = fromNoeNodeManager;
            ToChainNodeManager = toChainNodeManager;
            _fromChainGenesis = FromNoeNodeManager.GetGenesisContract(caller);
            FromChainToken = _fromChainGenesis.GetTokenContract(caller);
            _toChainGenesis = ToChainNodeManager.GetGenesisContract(caller);
            ToChainToken = _toChainGenesis.GetTokenContract(caller);
            FromChainCrossChain = _fromChainGenesis.GetCrossChainContract(caller);
            ToChainCrossChain = _toChainGenesis.GetCrossChainContract(caller);
        }

        public INodeManager FromNoeNodeManager { get; set; }
        public INodeManager ToChainNodeManager { get; set; }
        public CrossChainContract FromChainCrossChain;
        public CrossChainContract ToChainCrossChain;
        public TokenContract FromChainToken;
        public TokenContract ToChainToken;

        public MerklePath GetMerklePath(long blockNumber, string txId, out Hash root)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => FromNoeNodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    FromNoeNodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = Hash.LoadFromHex(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                    .ToArray();
                var txIdWithStatus = HashHelper.ComputeFrom(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
                Logger.Info($"The transaction index is {index}");
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }

        public MerklePath GetMerklePath(INodeManager nodeManager, string transactionId)
        {
            var result =
                AsyncHelper.RunSync(() => nodeManager.ApiClient.GetMerklePathByTransactionIdAsync(transactionId));

            return new MerklePath
            {
                MerklePathNodes =
                {
                    result.MerklePathNodes.Select(o => new MerklePathNode
                    {
                        Hash = Hash.LoadFromHex(o.Hash),
                        IsLeftChildNode = o.IsLeftChildNode
                    })
                }
            };
        }

        public TransactionResultDto ValidateTokenSymbol(string symbol, out string raw)
        {
            var tokenInfo = FromNoeNodeManager.GetTokenInfo(symbol);
            var validateTransaction = FromNoeNodeManager.GenerateRawTransaction(_fromChainGenesis.CallAddress
                , FromChainToken.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    Decimals = tokenInfo.Decimals,
                    Issuer = tokenInfo.Issuer,
                    Owner = tokenInfo.Owner,
                    IsBurnable = tokenInfo.IsBurnable,
                    IssueChainId = tokenInfo.IssueChainId,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply,
                    ExternalInfo = { tokenInfo.ExternalInfo.Value }
                });
            raw = validateTransaction;
            var txId = FromNoeNodeManager.SendTransaction(validateTransaction);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }
        
        public string ValidateTokenSymbols(string symbol)
        {
            var tokenInfo = FromNoeNodeManager.GetTokenInfo(symbol);
            var validateTransaction = FromNoeNodeManager.GenerateRawTransaction(_fromChainGenesis.CallAddress
                , FromChainToken.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    Decimals = tokenInfo.Decimals,
                    Issuer = tokenInfo.Issuer,
                    Owner = tokenInfo.Owner,
                    IsBurnable = tokenInfo.IsBurnable,
                    IssueChainId = tokenInfo.IssueChainId,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply,
                    ExternalInfo = { tokenInfo.ExternalInfo.Value }
                });
            return validateTransaction;
        }

        public TransactionResultDto CrossChainTransfer(string symbol, long amount, string toAccount, string account, out string raw)
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(FromNoeNodeManager.GetChainId());
            var validationChainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var issueChainId = FromChainToken.GetTokenInfo(symbol).IssueChainId;
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = issueChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = toAccount.ConvertAddress(),
                ToChainId = validationChainId
            };
            // execute cross chain transfer
            var rawTx = FromNoeNodeManager.GenerateRawTransaction(account,
                FromChainToken.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            raw = rawTx;
            var txId = FromNoeNodeManager.SendTransaction(rawTx);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }

        public string CrossChainTransferWithoutResult(string symbol, long amount, string toAccount, string account,
            out string raw)
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(FromNoeNodeManager.GetChainId());
            var validationChainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var issueChainId = FromChainToken.GetTokenInfo(symbol).IssueChainId;
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = issueChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = toAccount.ConvertAddress(),
                ToChainId = validationChainId
            };
            // execute cross chain transfer
            var rawTx = FromNoeNodeManager.GenerateRawTransaction(account,
                FromChainToken.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            raw = rawTx;
            var txId = FromNoeNodeManager.SendTransaction(rawTx);
            return txId;
        }

        public TransactionResultDto CrossChainReceive(TransactionResultDto txResult)
        {
            var param = txResult.Transaction.Params;
            var inputParam = (JObject)JsonConvert.DeserializeObject(param);

            var input = new CrossChainTransferInput
            {
                To = inputParam["to"].ToString().ConvertAddress(),
                Symbol = inputParam["symbol"].ToString(),
                Amount = long.Parse(inputParam["amount"].ToString()),
                ToChainId = int.Parse(inputParam["toChainId"].ToString()),
                IssueChainId = int.Parse(inputParam["issueChainId"].ToString()),
                Memo = inputParam["memo"]?.ToString()
            };

            var rawTx = ToChainNodeManager.GenerateRawTransaction(txResult.Transaction.From, txResult.Transaction.To,
                txResult.Transaction.MethodName, txResult.Transaction.RefBlockNumber,
                txResult.Transaction.RefBlockPrefix, txResult.Transaction.Signature, input);

            var receiveTokenInput = GenerateReceiveInput(txResult.BlockNumber, txResult.TransactionId, rawTx);
            var result = ToChainToken.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, receiveTokenInput);
            return result;
        }
        
        public TransactionResultDto CrossChainReceive(long height, string txId, string raw)
        {
            var receiveTokenInput = GenerateReceiveInput(height, txId, raw);
            var result = ToChainToken.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, receiveTokenInput);
            return result;
        }

        private CrossChainReceiveTokenInput GenerateReceiveInput(long height, string txId,
            string raw)
        {
            var fromChainId = FromNoeNodeManager.GetChainId();
            var toChainId = ToChainNodeManager.GetChainId();

            var merklePath = GetMerklePath(FromNoeNodeManager, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            CrossChainReceiveTokenInput crossChainReceiveToken;
                
            if (!fromChainId.Equals("AELF"))
            {
                var crossChainMerkleProofContext =
                    FromChainCrossChain.GetCrossChainMerkleProofContext(height);
                crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
                crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
            }
            else
            {
                crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = height,
                    TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
            }
            return crossChainReceiveToken;
        }
        
        public RegisterCrossChainTokenContractAddressInput RegisterTokenAddressInput(long height, string txId,
            string raw)
        {
            var fromChainId = FromNoeNodeManager.GetChainId();
            var toChainId = ToChainNodeManager.GetChainId();

            var merklePath = GetMerklePath(FromNoeNodeManager, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");

            RegisterCrossChainTokenContractAddressInput registerInput;
            if (!fromChainId.Equals("AELF"))
            {
                var crossChainMerkleProofContext =
                    FromChainCrossChain.GetCrossChainMerkleProofContext(height);
                registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress = ToChainToken.Contract,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
            }
            else
            {
                registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = height,
                    TokenContractAddress = ToChainToken.Contract,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
            }

            return registerInput;
        }

        public TransactionResultDto ValidateTokenAddress(string account,out string raw)
        {
            var validateTransaction = FromNoeNodeManager.GenerateRawTransaction(
                account, _fromChainGenesis.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = FromChainToken.Contract,
                    SystemContractHashName = HashHelper.ComputeFrom("AElf.ContractNames.Token")
                });
            raw = validateTransaction;
            var txId = FromNoeNodeManager.SendTransaction(validateTransaction);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }

        public bool CheckTokenAddress()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var validationAddress = FromChainToken.CallViewMethod<Address>(
                TokenMethod.GetCrossChainTransferTokenContractAddress,
                new GetCrossChainTransferTokenContractAddressInput
                {
                    ChainId = chainId
                });
            return validationAddress.Equals(ToChainToken.Contract);
        }

        public bool CheckPrivilegePreserved()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var sideChainData = FromChainCrossChain.GetChainInitializationData(chainId);
            return sideChainData.ChainCreatorPrivilegePreserved;
        }

        public long CheckMainChainIndexSideChain(long txHeight, INodeManager mainManager, INodeManager sideManager,
            CrossChainContract mainChainCross, CrossChainContract sideChainCross)
        {
            Logger.Info($"Wait main chain index side chain target height: {txHeight}");

            var mainHeight = long.MaxValue;
            var checkResult = false;
            var sideChainId = ChainHelper.ConvertBase58ToChainId(sideManager.GetChainId());
            while (!checkResult)
            {
                var indexSideChainBlock = mainChainCross.GetSideChainHeight(sideChainId);
                if (indexSideChainBlock < txHeight)
                {
                    Logger.Info("Block is not recorded ");
                    AsyncHelper.RunSync(() => Task.Delay(10000));
                    continue;
                }

                mainHeight = mainHeight == long.MaxValue
                    ? AsyncHelper.RunSync(() => mainManager.ApiClient.GetBlockHeightAsync())
                    : mainHeight;
                var indexParentBlock = sideChainCross.GetParentChainHeight();
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }

        public void CheckSideChainIndexMainChain(long txHeight)
        {
            Logger.Info($"Wait side chain index main chain target height: {txHeight}");

            while (txHeight > ToChainCrossChain.GetParentChainHeight())
            {
                Logger.Info("Block is not recorded ");
                AsyncHelper.RunSync(() => Task.Delay(10000));
            }
        }
        
        public TransactionResultDto CrossChainCreate(TransactionResultDto result, string rawTx)
        {
            var crossChainCrossToken = GenerateCreateInput(result.BlockNumber, result.TransactionId, rawTx);
            var createResult =
                ToChainToken.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken, crossChainCrossToken);
            return createResult;
        }
        
        private CrossChainCreateTokenInput GenerateCreateInput(long height, string txId, string rawTx)
        {
            var fromChainId = FromNoeNodeManager.GetChainId();
            var merklePath = GetMerklePath(FromNoeNodeManager, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            CrossChainCreateTokenInput crossChainCrossToken;
                
            if (!fromChainId.Equals("AELF"))
            {
                var crossChainMerkleProofContext =
                    FromChainCrossChain.GetCrossChainMerkleProofContext(height);
                crossChainCrossToken = new CrossChainCreateTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    MerklePath = merklePath,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight
                };
                crossChainCrossToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
            }
            else
            {
                crossChainCrossToken = new CrossChainCreateTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    MerklePath = merklePath,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                    ParentChainHeight = height
                };
            }
            return crossChainCrossToken;
        }
        
        public string CrossChainCreateWithoutResult(TransactionResultDto result, string rawTx)
        {

            var fromChainId = ChainHelper.ConvertBase58ToChainId(FromNoeNodeManager.GetChainId());
            var merklePath = GetMerklePath(FromNoeNodeManager, result.TransactionId);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = fromChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                ParentChainHeight = result.BlockNumber
            };
            CheckSideChainIndexMainChain(result.BlockNumber);

            var createResult =
                ToChainToken.ExecuteMethodWithTxId(TokenMethod.CrossChainCreateToken, crossChainCrossToken);
            return createResult;
        }
    }
}