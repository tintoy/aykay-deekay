using System.Runtime.Serialization;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
    ///		Well-known event types used by the Docker API. 
    /// </summary>
	public enum DockerEventType
	{
		/// <summary>
		///		An unknown event type.
		/// </summary>
		Unknown = 0,

		/// <summary>
        ///     Attached container console.
        /// </summary>
		[EnumMember(Value = "attach")]
		Attach,

        /// <summary>
        ///     Commit container changes.
        /// </summary>
		[EnumMember(Value = "commit")]
		Commit,

        /// <summary>
        ///     Connected to a container.
        /// </summary>
		[EnumMember(Value = "connect")]
		Connect,

        /// <summary>
        ///     Copied a container.
        /// </summary>
		[EnumMember(Value = "copy")]
		Copy,

        /// <summary>
        ///     Created a container.
        /// </summary>
        [EnumMember(Value = "create")]
		Create,

        /// <summary>
        ///     Deleted a container.
        /// </summary>
        [EnumMember(Value = "delete")]
		Delete,

        /// <summary>
        ///     Destroyed a container.
        /// </summary>
        [EnumMember(Value = "destroy")]
		Destroy,

        /// <summary>
        ///     Detached container console.
        /// </summary>
        [EnumMember(Value = "detach")]
		Detach,

        /// <summary>
        ///     Container died.
        /// </summary>
        [EnumMember(Value = "die")]		
		Die,

        /// <summary>
        ///     Disconnected from a container.
        /// </summary>
        [EnumMember(Value = "disconnect")]
		Disconnect,

        /// <summary>
        ///     Created a container via "docker exec".
        /// </summary>
        [EnumMember(Value = "exec_create")]
		ExecCreate,

        /// <summary>
        ///     Detached console for a container via "docker exec".
        /// </summary>
        [EnumMember(Value = "exec_detach")]
		ExecDetach,

        /// <summary>
        ///     Started a container via "docker exec".
        /// </summary>
        [EnumMember(Value = "exec_start")]
		ExecStart,

        /// <summary>
        ///     Exported a container.
        /// </summary>
        [EnumMember(Value = "export")]
		Export,

        /// <summary>
        ///     Container health status.
        /// </summary>
        [EnumMember(Value = "health_status")]
		HealthStatus,

        /// <summary>
        ///     Imported a container.
        /// </summary>
        [EnumMember(Value = "import")]
		Import,

        /// <summary>
        ///     Killed a container.
        /// </summary>
        [EnumMember(Value = "kill")]		
		Kill,

        /// <summary>
        ///     Loaded a container.
        /// </summary>
        [EnumMember(Value = "load")]
		Load,

        /// <summary>
        ///     Mounted a volume into a container.
        /// </summary>
        [EnumMember(Value = "mount")]
		Mount,

        /// <summary>
        ///     Container ran out of memory.
        /// </summary>
        [EnumMember(Value = "oom")]
		OutOfMemory,

        /// <summary>
        ///     Paused a container.
        /// </summary>
        [EnumMember(Value = "pause")]
		Pause,

        /// <summary>
        ///     Pulled an image.
        /// </summary>
        [EnumMember(Value = "pull")]
		Pull,

        /// <summary>
        ///     Pushed an image.
        /// </summary>
        [EnumMember(Value = "push")]
		Push,

        /// <summary>
        ///     Reloaded a container.
        /// </summary>
        [EnumMember(Value = "reload")]
		Reload,

        /// <summary>
        ///     Renamed a container.
        /// </summary>
        [EnumMember(Value = "rename")]
		Rename,
		
        /// <summary>
        ///     Resized a container (or is it an image?).
        /// </summary>
		[EnumMember(Value = "resize")]
		Resize,
		
        /// <summary>
        ///     Restarted a container.
        /// </summary>
		[EnumMember(Value = "restart")]
		Restart,

        /// <summary>
        ///     Saved container state.
        /// </summary>
        [EnumMember(Value = "save")]
		Save,

        /// <summary>
        ///     Started a container.
        /// </summary>
        [EnumMember(Value = "start")]		
		Start,

        /// <summary>
        ///     Stopped a container.
        /// </summary>
        [EnumMember(Value = "stop")]
		Stop,

        /// <summary>
        ///     Tagged an image.
        /// </summary>
        [EnumMember(Value = "tag")]
		Tag,

        /// <summary>
        ///     Process info for a container.
        /// </summary>
        [EnumMember(Value = "top")]		
		Top,
		
        /// <summary>
        ///     Unmounted a volume from a container.
        /// </summary>
		[EnumMember(Value = "unmount")]
		Unmount,

		/// <summary>
        ///     Unpaused a container.
        /// </summary>
		[EnumMember(Value = "unpause")]
		Unpause,

        /// <summary>
        ///     Untagged an image.
        /// </summary>
        [EnumMember(Value = "untag")]
		Untag,
		
        /// <summary>
        ///     Updated a container (or is it an image?).
        /// </summary>
		[EnumMember(Value = "update")]
		Update
	}
}
