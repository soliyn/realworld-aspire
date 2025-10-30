using System.Transactions;

namespace RealWorldAspire.ApiService.Tests.Features;

public class HandlerTestBase : IDisposable
{
    private readonly TransactionScope _transactionScope;

    protected HandlerTestBase()
    {
        _transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    }
    
    public void Dispose()
    {
        _transactionScope.Dispose();
    }

}