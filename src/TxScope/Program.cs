using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Pipeline;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;

namespace TxScope
{
    class Program
    {
        public static ISessionFactory SessionFactory;

        static async Task Main(string[] args)
        {
            var serviceName = typeof(Program).Namespace;
            Console.Title = serviceName;

            SessionFactory = Fluently.Configure()
                .Database(
                    MsSqlConfiguration.MsSql2008.ConnectionString("Data Source=.;Initial Catalog=TxScopeSample;User Id=sa;Password=yourStrong(!)Password")
                    .ShowSql()
                )
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Program>())
                .ExposeConfiguration(cfg => new SchemaExport(cfg)
                .Create(true, true))
                .BuildSessionFactory();

            var endpointConfiguration = new EndpointConfiguration(serviceName);

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.EnableOutbox();
            endpointConfiguration.SendFailedMessagesTo("error");

            endpointConfiguration.Pipeline
                .Register(new TxScopeBehavior(), "Build TX Scope");

            endpointConfiguration.UseTransport<RabbitMQTransport>()
                .UseConventionalRoutingTopology()
                .ConnectionString("host=localhost");

            var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.ConnectionBuilder(
                connectionBuilder: () =>
                {
                    return new SqlConnection("Data Source=.;Initial Catalog=TxScopeSample;User Id=sa;Password=yourStrong(!)Password");
                });

            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            await endpointInstance.SendLocal(new MySampleMessage());

            Console.WriteLine($"{serviceName} started. Press any key to stop.");
            Console.ReadLine();

            await endpointInstance.Stop();
        }
    }

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
