using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit2;

namespace AKDK.Tests
{
    using Actors;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///     The base class for actor tests that require a simulated <see cref="Connection"/> actor.
    /// </summary>
    public abstract class ConnectionTestKit
        : TestKit
    {
        /// <summary>
        ///     The Akka configuration used by the tests.
        /// </summary>
        protected static readonly Config TestConfig =
            ConfigurationFactory.ParseString(@"
                akka.suppress-json-serializer-warning = on
            ")
            .WithFallback(DefaultConfig);

        /// <summary>
        ///     Create a new <see cref="ConnectionTestKit"/>.
        /// </summary>
        /// <param name="actorSystemName">
        ///     The name of the actor system used to host the tests.
        /// </param>
        protected ConnectionTestKit(string actorSystemName)
            : base(TestConfig, actorSystemName)
        {
            ConnectionTestProbe = CreateTestProbe(name: "connection");
        }

        /// <summary>
        ///     The <see cref="TestProbe"/> representing the <see cref="Connection"/> actor.
        /// </summary>
        protected TestProbe ConnectionTestProbe { get; }

        /// <summary>
        ///     Create a new <see cref="Client"/> actor that uses <see cref="ConnectionTestProbe"/>.
        /// </summary>
        /// <param name="connection">
        ///     An optional reference to the <see cref="Connection"/> actor to use (uses <see cref="ConnectionTestProbe"/> if not specified).
        /// </param>
        /// <param name="name">
        ///     An optional name for the <see cref="Client"/> actor (defaults to "client" if not specified).
        /// </param>
        /// <returns>
        ///     A reference to the <see cref="Client"/> actor.
        /// </returns>
        protected virtual IActorRef CreateClient(IActorRef connection = null, string name = null)
        {
            if (connection == null)
                connection = ConnectionTestProbe;

            return Sys.ActorOf(
                Props.Create(
                    () => new Client(connection)
                ),
                name: name ?? "client"
            );
        }
    }
}
