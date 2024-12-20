using System.Collections.Generic;
using AElf.Client.Service;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;

namespace AElfChain.Common.Managers
{
    public interface INodeManager
    {
        AElfClient ApiClient { get; set; }
        AccountManager AccountManager { get; }
        TransactionManager TransactionManager { get; }
        string GetApiUrl();
        bool UpdateApiUrl(string url);
        string GetChainId();
        string GetGenesisContractAddress();

        //account
        string NewAccount(string password = "");
        string NewAccount(out byte[] privateKey, string password = "");
        string NewFakeAccount();
        string GetRandomAccount();
        string GetAccountPublicKey(string account, string password = "");
        List<string> ListAccounts();
        bool UnlockAccount(string account, string password = "");

        //chain
        string SendTransaction(string from, string to, string methodName, IMessage inputParameter);
        string SendTransaction(string from, string to, string methodName, IMessage inputParameter, out bool existed);
        string SendTransaction(string rawTransaction);
        List<string> SendTransactions(string rawTransactions);
        string GenerateRawTransaction(string from, string to, string methodName, IMessage inputParameter);
        string GenerateRawTransactionWithRefInfo(string from, string to, string methodName, IMessage inputParameter,
            out ReferenceInfo referenceInfo);
        string GenerateRawTransaction(string from, string to, string methodName,
            long refBlockNumber, string refBlockPrefix, string sign, IMessage inputParameter);
        Transaction GenerateTransaction(string from, string to, string methodName, IMessage inputParameter);
        TransactionResultDto CheckTransactionResult(string txId, int maxSeconds = -1);
        void CheckTransactionListResult(List<string> transactionIds);

        TResult QueryView<TResult>(string from, string to, string methodName, IMessage inputParameter)
            where TResult : IMessage<TResult>, new();

        ByteString QueryView(string from, string to, string methodName, IMessage inputParameter);
        
        //net
        List<PeerDto> NetGetPeers();
        bool NetAddPeer(string address, string username, string password);
        bool NetRemovePeer(string address, string username, string password);
        NetworkInfoOutput NetworkInfo();
    }
}