using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;

namespace TxScope
{
    class MySampleMessageHandler : IHandleMessages<MySampleMessage>
    {
        public Task Handle(MySampleMessage message, IMessageHandlerContext context)
        {
            var sqlPersistenceSession = context.SynchronizedStorageSession.SqlPersistenceSession();
            using (var session = Program.SessionFactory
                .WithOptions()
                    .Connection(sqlPersistenceSession.Connection)
                .OpenSession() )
            {
                var customer = new Customer
                {
                    FirstName = "Name",
                    LastName = "Surname"
                };

                session.Save(customer);
            }

            return Task.CompletedTask;
        }
    }
}
