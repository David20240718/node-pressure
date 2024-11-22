using Google.Protobuf;

namespace AElfTestContractScript;

public interface IContractService
{
    void InitializeContract();
    IContractService UpdateService(Service service);
    Task SendMultiTransactionsOneBytOneTasks(string symbol, long size, List<string> tester, string method);
    List<string> SendMultiTransactions(string symbol, long size, List<string> tester, string method,
        out DateTime sendTime);
    List<string> CreateProxyMethodTest(List<ProxyService.ProxyAccountInfo> proxyAccountInfos, int count, string contract,
        string method, IMessage input,
        out DateTime sendTime);
}