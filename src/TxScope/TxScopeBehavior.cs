using NServiceBus.Pipeline;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace TxScope
{
    public class TxScopeBehavior : Behavior<ITransportReceiveContext>
    {
        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await next().ConfigureAwait(false);

                tx.Complete();
            }
        }
    }
}
