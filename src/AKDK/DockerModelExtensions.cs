using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AKDK
{
    /// <summary>
    ///     Extension methods for well-known Docker API models.
    /// </summary>
    public static class DockerModelExtensions
    {
        /// <summary>
        ///     Configure the container to use the "json-file" logging driver.
        /// </summary>
        /// <param name="parameters">
        ///     The container-creation parameters.
        /// </param>
        /// <returns>
        ///     The container-creation parameters (enables method-chaining).
        /// </returns>
        public static CreateContainerParameters UseJsonFileLogger(this CreateContainerParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            HostConfig hostConfig = parameters.HostConfig ?? (parameters.HostConfig = new HostConfig());
            LogConfig logConfig = hostConfig.EnsureLogConfig();
            logConfig.Type = "json-file";
            logConfig.Config.Clear();

            return parameters;
        }

        /// <summary>
        ///     Configure the container to use the "journald" logging driver.
        /// </summary>
        /// <param name="parameters">
        ///     The container-creation parameters.
        /// </param>
        /// <returns>
        ///     The container-creation parameters (enables method-chaining).
        /// </returns>
        public static CreateContainerParameters UseJournaldLogger(this CreateContainerParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            HostConfig hostConfig = parameters.HostConfig ?? (parameters.HostConfig = new HostConfig());
            LogConfig logConfig = hostConfig.EnsureLogConfig();
            logConfig.Type = "journald";
            logConfig.Config.Clear();

            return parameters;
        }

        static LogConfig EnsureLogConfig(this HostConfig hostConfig)
        {
            if (hostConfig == null)
                throw new ArgumentNullException(nameof(hostConfig));

            LogConfig logConfig = hostConfig.LogConfig ?? (hostConfig.LogConfig = new LogConfig());
            if (logConfig.Config == null)
                logConfig.Config = new Dictionary<string, string>();

            return logConfig;
        }
    }
}
