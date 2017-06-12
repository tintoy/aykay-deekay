﻿using AKDK.Messages;
using System;
using System.Collections.Immutable;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that manages the execution of a specific job.
    /// </summary>
    public partial class JobWorker
    {
        /// <summary>
        ///     Request to a <see cref="JobWorker"/> for execution of a job.
        /// </summary>
        public class ExecuteJob
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="ExecuteJob"/> request.
            /// </summary>
            /// <param name="job">
            ///     The job to execute.
            /// </param>
            /// <param name="targetUrl">
            ///     The target URL to fetch.
            /// </param>
            /// <param name="jobStateDirectory">
            ///     The directory used to hold the job's state.
            /// </param>
            /// <param name="correlationId">
            ///     An optional message-correlation Id.
            /// </param>
            public ExecuteJob(Job job, DirectoryInfo jobStateDirectory, string correlationId = null)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
                JobStateDirectory = jobStateDirectory;
            }

            /// <summary>
            ///     The job to execute.
            /// </summary>
            public Job Job { get; }

            /// <summary>
            ///     The target URL to fetch.
            /// </summary>
            public Uri TargetUrl { get; }

            /// <summary>
            ///     The directory used to hold the job's state.
            /// </summary>
            public DirectoryInfo JobStateDirectory { get; }
        }

        /// <summary>
        ///     Response from a <see cref="JobWorker"/> indicating that a job is being executed.
        /// </summary>
        public class JobExecuting
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="JobExecuting"/> response.
            /// </summary>
            /// <param name="jobId">
            ///     The job that is executing.
            /// </param>
            /// <param name="jobStateDirectory">
            ///     The directory used to hold the job's state.
            /// </param>
            public JobExecuting(Job job, DirectoryInfo jobStateDirectory, string correlationId = null)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                if (jobStateDirectory == null)
                    throw new ArgumentNullException(nameof(jobStateDirectory));

                JobStateDirectory = jobStateDirectory;
            }

            /// <summary>
            ///     The job that is executing.
            /// </summary>
            public Job Job { get; }

            /// <summary>
            ///     The directory used to hold the job's state.
            /// </summary>
            public DirectoryInfo JobStateDirectory { get; }
        }

        /// <summary>
        ///     Response from a <see cref="JobWorker"/> indicating that a job has been executed.
        /// </summary>
        public class JobExecuted
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="JobExecuted"/> response.
            /// </summary>
            /// <param name="jobId">
            ///     The job that was executed.
            /// </param>
            /// <param name="jobStateDirectory">
            ///     The directory used to hold the job's state.
            /// </param>
            public JobExecuted(Job job, DirectoryInfo jobStateDirectory, string correlationId = null)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                if (jobStateDirectory == null)
                    throw new ArgumentNullException(nameof(jobStateDirectory));
                
                JobStateDirectory = jobStateDirectory;
            }

            /// <summary>
            ///     The job that was executed.
            /// </summary>
            public Job Job { get; }

            /// <summary>
            ///     The target URL to fetch.
            /// </summary>
            public Uri TargetUrl { get; }

            /// <summary>
            ///     The directory used to hold the job's state.
            /// </summary>
            public DirectoryInfo JobStateDirectory { get; }
        }

        /// <summary>
        ///     Message indicating that the timeout period expired before a job process was launched.
        /// </summary>
        internal class ProcessLaunchTimeout
        {
            /// <summary>
            ///     The singleton instance of the <see cref="ProcessLaunchTimeout"/> message.
            /// </summary>
            public static readonly ProcessLaunchTimeout Instance = new ProcessLaunchTimeout();

            /// <summary>
            ///     Create a new <see cref="ProcessLaunchTimeout"/> message.
            /// </summary>
            ProcessLaunchTimeout()
            {
            }
        }

        /// <summary>
        ///     Message indicating that the timeout period expired before a container's content was harvested.
        /// </summary>
        internal class ContainerHarvestTimeout
        {
            /// <summary>
            ///     The singleton instance of the <see cref="ContainerHarvestTimeout"/> message.
            /// </summary>
            public static readonly ContainerHarvestTimeout Instance = new ContainerHarvestTimeout();

            /// <summary>
            ///     Create a new <see cref="ContainerHarvestTimeout"/> message.
            /// </summary>
            ContainerHarvestTimeout()
            {
            }
        }
    }
}