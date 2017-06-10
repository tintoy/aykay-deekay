using System;
using System.Collections.Immutable;

namespace AKDK.Messages
{
    /// <summary>
    ///		Request creation of a new container from the Docker API.
    /// </summary>
    public class CreateContainer
        : Request
    {
        /// <summary>
        ///		Create a new <see cref="CreateContainer"/> message.
        /// </summary>
        /// <param name="image">
        ///     The Id, name, or tag of the image from which the container will be created.
        /// </param>
        /// <param name="name">
        ///     An optional name for the container.
        ///     
        ///     If not specified, one will be auto-generated.
        /// </param>
        /// <param name="environmentVariables">
        ///     Environment variables (if any) passed to the container.
        /// </param>
        /// <param name="binds">
        ///     Paths (if any) to bind-mount into the container.
        /// </param>
        /// <param name="ports">
        ///     Ports (if any) to expose from the container.
        /// </param>
        /// <param name="attachStdOut">
        ///     Attach STDOUT to the container?
        /// </param>
        /// <param name="attachStdErr">
        ///     Attach STDERR to the container?
        /// </param>
        /// <param name="attachStdIn">
        ///     Attach STDIN to the container?
        /// </param>
        /// <param name="tty">
        ///     Enable a TTY for the container?
        /// </param>
        /// <param name="logType">
        ///     The type of logging to use for the container.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public CreateContainer(string image, string name = null, ImmutableDictionary<string, string> environmentVariables = null, ImmutableDictionary<string, string> binds = null, ImmutableDictionary<string, string> ports = null, bool attachStdOut = false, bool attachStdErr = false, bool attachStdIn = false, bool tty = false, string logType = "json-file", string correlationId = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(image))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(image)}.", nameof(image));

            if (String.IsNullOrWhiteSpace(logType))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(logType)}.", nameof(logType));

            Image = image;
            Name = name;
            EnvironmentVariables = environmentVariables ?? ImmutableDictionary<string, string>.Empty;
            Binds = binds ?? ImmutableDictionary<string, string>.Empty;
            Ports = ports ?? ImmutableDictionary<string, string>.Empty;
            AttachStdOut = attachStdOut;
            AttachStdErr = attachStdErr;
            AttachStdIn = attachStdIn;
            TTY = tty;
            LogType = logType;
        }

        /// <summary>
        ///     The Id, name, or tag of the image from which the container will be created.
        /// </summary>
        public string Image { get; }

        /// <summary>
        ///     An optional name for the container.
        /// </summary>
        /// <remarks>
        ///     If not specified, one will be auto-generated.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        ///     Environment variables (if any) passed to the container.
        /// </summary>
        /// <remarks>
        ///     Format is ["Name"] = "Value".
        /// </remarks>
        public ImmutableDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        ///     Paths (if any) to bind-mount into the container.
        /// </summary>
        /// <remarks>
        ///     Format is ["HostPath"] = "ContainerPath".
        /// </remarks>
        public ImmutableDictionary<string, string> Binds { get; }

        /// <summary>
        ///     Ports (if any) to expose from the container.
        /// </summary>
        /// <remarks>
        ///     Format is ["HostPort"] = "ContainerPort".
        /// </remarks>
        public ImmutableDictionary<string, string> Ports { get; }

        /// <summary>
        ///     Attach STDOUT to the container?
        /// </summary>
        public bool AttachStdOut { get; }

        /// <summary>
        ///     Attach STDERR to the container?
        /// </summary>
        public bool AttachStdErr { get; }

        /// <summary>
        ///     Attach STDIN to the container?
        /// </summary>
        public bool AttachStdIn { get; }

        /// <summary>
        ///     Enable a TTY for the container?
        /// </summary>
        public bool TTY { get; }

        /// <summary>
        ///     The type of logging to use for the container.
        /// </summary>
        public string LogType { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Create Container ({Image})";

    }
}
