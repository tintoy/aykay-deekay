using Docker.DotNet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AKDK.Actors
{
    using Messages;
    using System.IO;

    /// <summary>
    ///     Actor that manages a connection to the Docker API.
    /// </summary>
    public partial class Connection
    {
        /// <summary>
        ///     Delegate representing a command (i.e. operation) to be asynchronously executed by a <see cref="Connection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The <see cref="IDockerClient"/> used to interact with the Docker API.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     The command response (will be asynchronously piped back to the <see cref="Connection"/> actor).
        /// </returns>
        public delegate Task<Response> Command(IDockerClient client, CancellationToken cancellationToken);

        /// <summary>
        ///     Request for a <see cref="Connection"/> to execute a command against the Docker API.
        /// </summary>
        public class ExecuteCommand
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="ExecuteCommand"/> message.
            /// </summary>
            /// <param name="requestMessage">
            ///     The request message for which the command is being executed.
            /// </param>
            /// <param name="command">
            ///     A delegate representing the command to execute.
            /// </param>
            public ExecuteCommand(Request requestMessage, Command command)
                : base(requestMessage.CorrelationId)
            {
                if (requestMessage == null)
                    throw new ArgumentNullException(nameof(requestMessage));
                
                if (command == null)
                    throw new ArgumentNullException(nameof(command));

                RequestMessage = requestMessage;
                Command = command;
            }

            /// <summary>
            ///     The request message for which the command is being executed.
            /// </summary>
            public Request RequestMessage { get; }

            /// <summary>
            ///     A delegate representing the command to execute.
            /// </summary>
            public Command Command { get; }
        }

        /// <summary>
        ///     Represents the result of an <see cref="ExecuteCommand"/> request.
        /// </summary>
        public class CommandResult
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="CommandResult"/> message.
            /// </summary>
            /// <param name="responseMessage">
            ///     The response message that will be sent to the actor that requested the command be executed.
            /// </param>
            public CommandResult(Response responseMessage)
                : base(responseMessage.CorrelationId)
            {
                if (responseMessage == null)
                    throw new ArgumentNullException(nameof(responseMessage));

                ResponseMessage = responseMessage;
            }

            /// <summary>
            ///     Was the command execution successful?
            /// </summary>
            public bool Success => Exception == null;

            /// <summary>
            ///     Is the command response streamed?
            /// </summary>
            public bool IsStreamed => ResponseStream != null;

            /// <summary>
            ///     The response message that will be sent to the actor that requested the command be executed.
            /// </summary>
            public Response ResponseMessage { get; }

            /// <summary>
            ///     The exception (if any) that was raised when executing the command.
            /// </summary>
            public Exception Exception => (ResponseMessage is ErrorResponse errorResponse) ? errorResponse.Exception : null;

            /// <summary>
            ///     The response stream (if the response is streamed).
            /// </summary>
            public Stream ResponseStream => (ResponseMessage is StreamedResponse streamedResponse) ? streamedResponse.ResponseStream : null;
        }
        
        /// <summary>
        ///     Request to a <see cref="Connection"/> requesting close of the underlying connection to the Docker API.
        /// </summary>
        public class Close
        {
            /// <summary>
            ///		The singleton instance of the <see cref="Close"/> message.
            /// </summary>
            public static readonly Close Instance = new Close();

            /// <summary>
            ///		Create a new <see cref="Close"/> message.
            /// </summary>
            Close()
            {
            }
        }
    }
}