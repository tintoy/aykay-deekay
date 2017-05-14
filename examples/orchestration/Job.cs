using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace AKDK.Examples.Orchestration
{
    /// <summary>
    ///     Represents information about a job.
    /// </summary>
    public sealed class Job
    {
        /// <summary>
        ///     Create a new <see cref="Job"/>.
        /// </summary>
        /// <param name="id">
        ///     The job Id.
        /// </param>
        /// <param name="targetUrl">
        ///     The target URL for the job.
        /// </param>
        public Job(int id, Uri targetUrl)
            : this(id, JobStatus.Created, targetUrl)
        {
        }

        /// <summary>
        ///     Create a new <see cref="Job"/>.
        /// </summary>
        /// <param name="id">
        ///     The job Id.
        /// </param>
        /// <param name="status">
        ///     A <see cref="JobStatus"/> value representing the job status.
        /// </param>
        /// <param name="targetUrl">
        ///     The target URL for the job.
        /// </param>
        public Job(int id, JobStatus status, Uri targetUrl)
            : this(id, status, targetUrl, content: null, messages: ImmutableList<string>.Empty)
        {
        }

        /// <summary>
        ///     Create a new <see cref="Job"/>.
        /// </summary>
        /// <param name="id">
        ///     The job Id.
        /// </param>
        /// <param name="status">
        ///     A <see cref="JobStatus"/> value representing the job status.
        /// </param>
        /// <param name="targetUrl">
        ///     The target URL for the job.
        /// </param>
        /// <param name="content">
        ///     The content (if any) fetched from the target URL.
        /// </param>
        /// <param name="messages">
        ///     Messages (if any) associated with the job.
        /// </param>
        internal Job(int id, JobStatus status, Uri targetUrl, string content, ImmutableList<string> messages)
        {
            if (id < 1)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Job Id cannot be less than 1.");
            
            if (targetUrl == null)
                throw new ArgumentNullException(nameof(targetUrl));

            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            Id = id;
            TargetUrl = targetUrl;
            Messages = messages;
        }

        /// <summary>
		///     The job Id.
		/// </summary>
        public int Id { get; }

        /// <summary>
        ///     The job status.
        /// </summary>
        public JobStatus Status { get; }

        /// <summary>
		///     The job's target URL.
		/// </summary>
        public Uri TargetUrl { get; }

        /// <summary>
        ///     The content (if any) fetched from the target URL.
        /// </summary>
        public string Content { get; }

        /// <summary>
        ///     Mesages (if any) associated with the job.
        /// </summary>
		public ImmutableList<string> Messages { get; }

        /// <summary>
        ///     Create a copy of the <see cref="Job"/>, but with the specified status (and, optionally, messages appended).
        /// </summary>
        /// <param name="status">
        ///     A <see cref="JobStatus"/> value representing the job's new status.
        /// </param>
        /// <param name="messages">
        ///     The messages (if any) to append.
        /// </param>
        /// <returns>
        ///     The new <see cref="Job"/>.
        /// </returns>
        public Job WithStatus(JobStatus status, params string[] messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (status == Status && messages.Length == 0)
                return this;

            return new Job(Id, Status, TargetUrl, Content,
                messages: Messages.AddRange(messages)
            );
        }

        /// <summary>
        ///     Create a copy of the <see cref="Job"/>, but with the specified content.
        /// </summary>
        /// <param name="content">
        ///     The content (if any) fetched from the target URL.
        /// </param>
        /// <returns>
        ///     The new job.
        /// </returns>
        public Job WithContent(string content)
        {
            if (String.Equals(Content, content))
                return this;

            return new Job(Id, Status, TargetUrl, content, Messages);
        }

        /// <summary>
        ///     Create a copy of the <see cref="Job"/>, but with the specified messages appended.
        /// </summary>
        /// <param name="messages">
        ///     The messages to append.
        /// </param>
        /// <returns>
        ///     The new <see cref="Job"/>.
        /// </returns>
        public Job WithMessages(params string[] messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (messages.Length == 0)
                return this;

            return new Job(Id, Status, TargetUrl, Content,
                messages: Messages.AddRange(messages)
            );
        }
    }

    /// <summary>
	///     Represents the status of a job.
	/// </summary>
    public enum JobStatus
	{
        /// <summary>
        ///     Job status is unknown.
        /// </summary>
        /// <remarks>
        ///     Used to detect uninitialised values; do not use directly.
        /// </remarks>
        Unknown = 0,

        /// <summary>
        ///     Job has been created.
        /// </summary>
        [EnumMember(Value = "created")]
        Created = 1,

        /// <summary>
        ///     Job is ready to execute.
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending = 2,

        /// <summary>
        ///     Job is executing.
        /// </summary>
        [EnumMember(Value = "active")]
        Active = 3,

        /// <summary>
        ///     Job completed successfully.
        /// </summary>
        [EnumMember(Value = "completed")]
        Completed = 4,

        /// <summary>
        ///     Job execution failed.
        /// </summary>
        [EnumMember(Value = "failed")]
        Failed = 5
	}
}
