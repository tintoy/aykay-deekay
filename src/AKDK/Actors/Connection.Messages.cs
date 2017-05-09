using Docker.DotNet;
using System;
using System.Threading.Tasks;

namespace AKDK.Actors
{
    using Messages;

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
        /// <param name="correlationId">
        ///     The request message correlation Id.
        /// </param>
        /// <returns>
        ///     The command response (will be asynchronously piped back to the <see cref="Connection"/> actor).
        /// </returns>
        public delegate Task<Response> Command(IDockerClient client, string correlationId);

        /// <summary>
        ///     Request for a <see cref="Connection"/> to execute a command against the Docker API.
        /// </summary>
        public class ExecuteCommand
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="ExecuteCommand"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///     The message correlation Id.
            /// </param>
            /// <param name="command">
            ///     A delegate representing the command to execute.
            /// </param>
            public ExecuteCommand(string correlationId, Command command)
                : base(correlationId)
            {
                if (command == null)
                    throw new ArgumentNullException(nameof(command));

                Command = command;
            }

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
            /// <param name="correlationId">
            ///     The message correlation Id.
            /// </param>
            public CommandResult(string correlationId)
                : base(correlationId)
            {
            }

            /// <summary>
            ///     Was the command execution successful?
            /// </summary>
            public bool Success => Exception == null;

            /// <summary>
            ///     The response that will be sent to the actor that requested the command be executed.
            /// </summary>
            public Response Response { get; }

            /// <summary>
            ///     The exception (if any) that was raised when executing the command.
            /// </summary>
            public Exception Exception => (Response is ErrorResponse errorResponse) ? errorResponse.Exception : null;            
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