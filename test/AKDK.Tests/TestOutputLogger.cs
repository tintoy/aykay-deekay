using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using System;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace AKDK.Tests
{
    /// <summary>
    ///     Akka.NET logger that writes to test output.
    /// </summary>
    public class TestOutputLogger
        : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        /// <summary>
        ///     Test output, keyed by test.
        /// </summary>
        /// <remarks>
        ///     Used to supply test output helper out-of-band when starting test.
        /// </remarks>
        static readonly ConcurrentDictionary<Guid, ITestOutputHelper> _testOutputs = new ConcurrentDictionary<Guid, ITestOutputHelper>();

        /// <summary>
        ///     The output for the current test.
        /// </summary>
        ITestOutputHelper Output { get; }

        /// <summary>
        ///     Create a new <see cref="TestOutputLogger"/>.
        /// </summary>
        public TestOutputLogger()
        {
            Console.WriteLine("Constructing test-logger actor {0}...", Context.Self.Path);

            string testIdValue = Context.System.Settings.Config.GetString("akdk.test.test_id");
            if (String.IsNullOrWhiteSpace(testIdValue))
                throw new InvalidOperationException("Missing configuration: 'akdk.test.test_id'.");

            Guid testId = Guid.Parse(testIdValue);
            ITestOutputHelper testOutput;
            if (!_testOutputs.TryRemove(testId, out testOutput))
                throw new InvalidOperationException("No output registered for test ''.");

            Output = testOutput;

            Become(Initializing);

            Console.WriteLine("Constructed test-logger actor {0}.", Context.Self.Path);
        }

        /// <summary>
        ///     Called when the logger is initialising.
        /// </summary>
        void Initializing()
        {
            Receive<InitializeLogger>(initializeLogger =>
            {
                Console.WriteLine("Initialising test-logger actor {0}...", Context.Self.Path);

                Become(Ready);

                Sender.Tell(
                    new LoggerInitialized()
                );

                Console.WriteLine("Initialised test-logger actor {0}...", Context.Self.Path);
            });
        }

        /// <summary>
        ///     Called when the logger is ready to handle log messages.
        /// </summary>
        void Ready()
        {
            Receive<Error>(error =>
            {
                Log("Error", error.Message, error.Cause);
            });
            Receive<Warning>(warning =>
            {
                Log("Warning", warning.Message);
            });
            Receive<Info>(info =>
            {
                Log("Info", info.Message);
            });
            Receive<Debug>(debug =>
            {
                Log("Debug", debug.Message);
            });
        }

        /// <summary>
        ///     Register the output helper for a test.
        /// </summary>
        /// <param name="output">
        ///     The test output helper.
        /// </param>
        /// <returns>
        ///     A new <see cref="Guid"/> representing the test.
        /// </returns>
        public static Guid RegisterTestOutput(ITestOutputHelper output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            
            Guid testId = Guid.NewGuid();

            _testOutputs[testId] = output;

            Console.WriteLine("Registered output for test '{0}'.", testId);

            return testId;
        }

        /// <summary>
        ///     Write a message to the log.
        /// </summary>
        /// <param name="category">
        ///     The log message category.
        /// </param>
        /// <param name="message">
        ///     The message to log.
        /// </param>
        void Log(string category, object message)
        {
            if (String.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'category'.", nameof(category));
            
            Output.WriteLine("{0}: {1}",
                category, FormatMessage(message)
            );
        }

        /// <summary>
        ///     Write a message to the log.
        /// </summary>
        /// <param name="category">
        ///     The log message category.
        /// </param>
        /// <param name="message">
        ///     The message to log.
        /// </param>
        /// <param name="cause">
        ///     An <see cref="Exception"/> representing the cause of the message.
        /// </param>
        void Log(string category, object message, Exception cause)
        {
            if (String.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'category'.", nameof(category));

            Output.WriteLine("{0}: {1}\nException: {2}",
                category, FormatMessage(message), cause
            );
        }

        /// <summary>
        ///     Format the specified message for display.
        /// </summary>
        /// <param name="message">
        ///     The message to display.
        /// </param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        static string FormatMessage(object message)
        {
            if (message is LogMessage logMessage)
                return String.Format(logMessage.Format, logMessage.Args);

            return message.ToString();
        }
    }
}