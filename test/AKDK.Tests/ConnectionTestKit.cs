using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using System;
using Xunit.Abstractions;

namespace AKDK.Tests
{
    using Actors;

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
        ///     Create The Akka configuration (with logging) used by the tests.
        /// </summary>
        /// <param name="output">
        ///     The <see cref="ITestOutputHelper"/> for the current test.
        /// </param>
        /// <returns>
        ///     The configuration.
        /// </returns>
        protected static Config TestConfigWithLogger(ITestOutputHelper output)
        {
            Guid testId = TestOutputLogger.RegisterTestOutput(output);

            Config loggerConfig = ConfigurationFactory.ParseString($@"
                akdk.test.test_id = ""{testId}""
                akka.loggers = [ ""AKDK.Tests.TestOutputLogger,AKDK.Tests"" ]

                akka.suppress-json-serializer-warning = on
            ");

            return loggerConfig.WithFallback(DefaultConfig);
        }

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
        ///     Create a new <see cref="ConnectionTestKit"/>.
        /// </summary>
        /// <param name="actorSystemName">
        ///     The name of the actor system used to host the tests.
        /// </param>
        protected ConnectionTestKit(string actorSystemName, ITestOutputHelper output)
            : base(TestConfigWithLogger(output), actorSystemName, output)
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
