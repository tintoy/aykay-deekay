using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor used to persist information about active jobs.
    /// </summary>
    public partial class JobStore
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default name for instances of the <see cref="JobStoreEvents"/> actor.
        /// </summary>
        public static readonly string ActorName = "job-store";

        /// <summary>
        ///     Serialiser settings for persisting job store data.
        /// </summary>
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter()
            }
        };

        /// <summary>
        ///     The file used to persist job store data.
        /// </summary>
        readonly FileInfo       _storeFile;

        /// <summary>
        ///     The serialiser used to persist store data.
        /// </summary>
        readonly JsonSerializer _serializer = JsonSerializer.Create(SerializerSettings);

        /// <summary>
        ///     The current job store data.
        /// </summary>
        JobStoreData            _data;

        /// <summary>
        ///     A reference to the actor that manages the job-store event bus.
        /// </summary>
        IActorRef               _jobStoreEvents;

        /// <summary>
        ///     Create a new <see cref="JobStore"/> actor.
        /// </summary>
        /// <param name="storeFile">
        ///     The name of the file used to persist job store data.
        /// </param>
        public JobStore(string storeFile)
        {
            if (String.IsNullOrWhiteSpace(storeFile))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(storeFile)}.", nameof(storeFile));

            _storeFile = new FileInfo(storeFile);
        }

        /// <summary>
        ///     Called when the actor is ready to handle messages.
        /// </summary>
        void Ready()
        {
            Receive<CreateJob>(createJob =>
            {
                JobData jobData = new JobData
                {
                    Id = _data.NextJobId++,
                    Status = JobStatus.Created,
                    TargetUrl = createJob.TargetUrl
                };
                _data.Jobs.Add(jobData.Id, jobData);

                Persist();

                _jobStoreEvents.Tell(new JobStoreEvents.JobCreated(
                    correlationId: createJob.CorrelationId,
                    job: jobData.ToJob()
                ));
            });
            Receive<UpdateJob>(updateJob =>
            {
                JobData jobData;
                if (!_data.Jobs.TryGetValue(updateJob.JobId, out jobData))
                {
                    Log.Warning("Received request to update non-existent job '{0}' from '{1}'.",
                        updateJob.JobId,
                        Sender
                    );

                    Sender.Tell(new OperationFailure(updateJob.CorrelationId,
                        operationName: $"Update Job {updateJob.JobId}",
                        reason: new Exception($"Job {updateJob.JobId} not found.") // TODO: Custom exception type.
                    ));

                    return;
                }

                bool statusChange = updateJob.Status != jobData.Status;
                if (!statusChange && updateJob.AppendMessages.Count == 0)
                    return; // Nothing to do.

                jobData.Status = updateJob.Status;
                jobData.Messages.AddRange(updateJob.AppendMessages);

                Persist();

                if (statusChange)
                {
                    switch (jobData.Status)
                    {
                        case JobStatus.Pending:
                        {
                            // TODO: Define JobStoreEvents.JobPending message.

                            break;
                        }
                        case JobStatus.Active:
                        {
                            _jobStoreEvents.Tell(new JobStoreEvents.JobStarted(updateJob.CorrelationId,
                                job: jobData.ToJob()
                            ));

                            break;
                        }
                        case JobStatus.Completed:
                        {
                            _jobStoreEvents.Tell(new JobStoreEvents.JobCompleted(updateJob.CorrelationId,
                                job: jobData.ToJob()
                            ));

                            break;
                        }
                        case JobStatus.Failed:
                        {
                            _jobStoreEvents.Tell(new JobStoreEvents.JobFailed(updateJob.CorrelationId,
                                job: jobData.ToJob()
                            ));

                            break;
                        }
                    }
                }
            });
            Forward<EventBusActor.Subscribe>(_jobStoreEvents);
            Forward<EventBusActor.Unsubscribe>(_jobStoreEvents);
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            InitializeStore();
            _jobStoreEvents = Context.ActorOf(
                JobStoreEvents.Create(),
                name: "events"
            );
            Context.Watch(_jobStoreEvents);

            Become(Ready);
        }

        /// <summary>
        ///     Initialise the job store.
        /// </summary>
        void InitializeStore()
        {
            if (_storeFile.Exists)
            {
                using (StreamReader storeReader = _storeFile.OpenText())
                using (JsonTextReader jsonReader = new JsonTextReader(storeReader))
                {
                    _data = _serializer.Deserialize<JobStoreData>(jsonReader);
                }
            }
            else
            {
                _data = new JobStoreData
                {
                    NextJobId = 1
                };
                Persist();
            }
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create a new <see cref="JobStore"/> actor.
        /// </summary>
        /// <param name="storeFile">
        ///     The name of the file used to persist job store data.
        /// </param>
        public static Props Create(string storeFile)
        {
            if (String.IsNullOrWhiteSpace(storeFile))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(storeFile)}.", nameof(storeFile));

            return Props.Create(
                () => new JobStore(storeFile)
            );
        }

        /// <summary>
        ///     Write job store data to the store file.
        /// </summary>
        void Persist()
        {
            if (!_storeFile.Directory.Exists)
                _storeFile.Directory.Create();

            using (StreamWriter storeWriter = _storeFile.CreateText())
            using (JsonTextWriter jsonWriter = new JsonTextWriter(storeWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;

                _serializer.Serialize(jsonWriter, _data);
            }
        }

        /// <summary>
        ///     Persistence contract for job data.
        /// </summary>
        class JobData
        {
            /// <summary>
            ///     The job Id.
            /// </summary>
            [JsonProperty("id")]
            public int Id { get; set; }

            /// <summary>
            ///     The job status.
            /// </summary>
            [JsonProperty("status")]
            public JobStatus Status { get; set; }

            /// <summary>
            ///     The URL to fetch.
            /// </summary>
            [JsonProperty("targetUrl")]
            public Uri TargetUrl { get; set; }

            /// <summary>
            ///     The content (if any) fetched from the target URL.
            /// </summary>
            [JsonProperty("content", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string Content { get; set; }

            /// <summary>
            ///     Messages (if any) associated with the job.
            /// </summary>
            [JsonProperty("messages", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
            public List<string> Messages { get; } = new List<string>();

            /// <summary>
            ///     Convert the <see cref="JobData"/> to a <see cref="Job"/>.
            /// </summary>
            /// <returns>
            ///     The new <see cref="Job"/>.
            /// </returns>
            public Job ToJob()
            {
                return new Job(Id, Status, TargetUrl, Content,
                    messages: ImmutableList.CreateRange(Messages)
                );
            }

            /// <summary>
            ///     Create a <see cref="JobData"/> from the specified <see cref="Job"/>.
            /// </summary>
            /// <param name="job">
            ///     The <see cref="Job"/>.
            /// </param>
            /// <returns>
            ///     The new <see cref="JobData"/>.
            /// </returns>
            public static JobData FromJob(Job job)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                var jobData = new JobData
                {
                    Id = job.Id,
                    TargetUrl = job.TargetUrl,
                    Content = job.Content
                };

                jobData.Messages.AddRange(job.Messages);

                return jobData;
            }
        }

        /// <summary>
        ///     Persistence contract for job data.
        /// </summary>
        class JobStoreData
        {
            /// <summary>
            ///     The next available job Id.
            /// </summary>
            [JsonProperty("nextJobId")]
            public int NextJobId { get; set; }

            /// <summary>
            ///     All jobs known to the job store, keyed by job Id.
            /// </summary>
            [JsonProperty("jobs", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
            public Dictionary<int, JobData> Jobs { get; } = new Dictionary<int, JobData>();
        }
    }
}
