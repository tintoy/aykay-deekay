using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace AKDK.Messages.DockerEvents.Converters
{
    /// <summary>
    ///     JSON converter for <see cref="DockerEvent"/>s.
    /// </summary>
	class DockerEventConverter
        : JsonCreationConverter<DockerEvent>
    {
        /// <summary>
        ///     Well-known image event types that have corresponding sub-types of <see cref="ImageEvent"/>.
        /// </summary>
        /// <remarks>
        ///     All other image events are deserialised as <see cref="ImageEvent"/>.
        /// </remarks>
        public static readonly ImmutableDictionary<DockerEventType, Type> ImageEventTypes = ImmutableDictionary<DockerEventType, Type>.Empty.AddRange(new Dictionary<DockerEventType, Type>
        {
            [DockerEventType.Pull] = typeof(ImagePulled),
            [DockerEventType.Push] = typeof(ImagePushed)
        });

        /// <summary>
        ///     Well-known container event types that have corresponding sub-types of <see cref="ContainerEvent"/>.
        /// </summary>
        /// <remarks>
        ///     All other container events are deserialised as <see cref="ContainerEvent"/>.
        /// </remarks>
        public static readonly ImmutableDictionary<DockerEventType, Type> ContainerEventTypes = ImmutableDictionary<DockerEventType, Type>.Empty.AddRange(new Dictionary<DockerEventType, Type>
        {
            [DockerEventType.Create] = typeof(ContainerCreated),
            [DockerEventType.Start] = typeof(ContainerStarted),
            [DockerEventType.Die] = typeof(ContainerDied),
            [DockerEventType.Destroy] = typeof(ContainerDestroyed),
        });

        /// <summary>
        ///     Well-known network event types that have corresponding sub-types of <see cref="NetworkEvent"/>.
        /// </summary>
        /// <remarks>
        ///     All other network events are deserialised as <see cref="NetworkEvent"/>.
        /// </remarks>
        public static readonly ImmutableDictionary<DockerEventType, Type> NetworkEventTypes = ImmutableDictionary<DockerEventType, Type>.Empty.AddRange(new Dictionary<DockerEventType, Type>
        {
            [DockerEventType.Connect] = typeof(NetworkConnected),
            [DockerEventType.Disconnect] = typeof(NetworkDisconnected)
        });

        /// <summary>
        ///     Well-known volume event types that have corresponding sub-types of <see cref="VolumeEvent"/>.
        /// </summary>
        /// <remarks>
        ///     All other volume events are deserialised as <see cref="VolumeEvent"/>.
        /// </remarks>
        public static readonly ImmutableDictionary<DockerEventType, Type> VolumeEventTypes = ImmutableDictionary<DockerEventType, Type>.Empty;

        /// <summary>
        ///     Create a <see cref="DockerEvent"/> to be populated from serialised data.
        /// </summary>
        /// <param name="json">
        ///     The JSON being deserialised.
        /// </param>
        /// <returns>
        ///     The new <see cref="DockerEvent"/>.
        /// </returns>
        protected override DockerEvent Create(JObject json)
        {
			string eventTargetValue = (string)json.GetValue("Type");
			DockerEventTarget eventTarget;
			if (!Enum.TryParse(eventTargetValue, true, out eventTarget))
				throw new InvalidOperationException($"Unsupported event target ('Type' == '{eventTargetValue}').");

			string eventTypeValue = (string)json.GetValue("Action");
			DockerEventType eventType;
			if (!Enum.TryParse(eventTypeValue, true, out eventType))
				throw new InvalidOperationException($"Unsupported event type ('Action' == '{eventTypeValue}').");

			DockerEvent dockerEvent;
			switch (eventTarget)
			{
				case DockerEventTarget.Container:
				{
					dockerEvent = CreateContainerEvent(eventType);

					break;
				}
				case DockerEventTarget.Image:
				{
					dockerEvent = CreateImageEvent(eventType);

					break;
				}
                case DockerEventTarget.Network:
                {
                    dockerEvent = CreateNetworkEvent(eventType);

                    break;
                }
                case DockerEventTarget.Volume:
                {
                    dockerEvent = CreateVolumeEvent(eventType);

                    break;
                }
                default:
				{
					throw new InvalidOperationException($"Unsupported event target ({eventTarget}).");
				}
			}

			if (dockerEvent == null)
				throw new InvalidOperationException($"Unsupported event type ({eventType}).");

            return dockerEvent;
        }

        /// <summary>
        ///     Create a <see cref="ContainerEvent"/> to be populated from serialised data.
        /// </summary>
        /// <param name="eventType">
        ///     The type of container event represented by the serialised data.
        /// </param>
        /// <returns>
        ///     The new <see cref="ContainerEvent"/>.
        /// </returns>
		ContainerEvent CreateContainerEvent(DockerEventType eventType)
		{
            if (ContainerEventTypes.TryGetValue(eventType, out Type type))
                return (ContainerEvent)Activator.CreateInstance(type);

            return (ContainerEvent)Activator.CreateInstance(typeof(ContainerEvent), eventType);
        }

        /// <summary>
        ///     Create a <see cref="ImageEvent"/> to be populated from serialised data.
        /// </summary>
        /// <param name="eventType">
        ///     The type of container event represented by the serialised data.
        /// </param>
        /// <returns>
        ///     The new <see cref="ImageEvent"/>.
        /// </returns>
		ImageEvent CreateImageEvent(DockerEventType eventType)
		{
            if (ImageEventTypes.TryGetValue(eventType, out Type type))
                return (ImageEvent)Activator.CreateInstance(type);

            return (ImageEvent)Activator.CreateInstance(typeof(ImageEvent), eventType);
        }

        /// <summary>
        ///     Create a <see cref="NetworkEvent"/> to be populated from serialised data.
        /// </summary>
        /// <param name="eventType">
        ///     The type of container event represented by the serialised data.
        /// </param>
        /// <returns>
        ///     The new <see cref="NetworkEvent"/>.
        /// </returns>
        NetworkEvent CreateNetworkEvent(DockerEventType eventType)
        {
            if (NetworkEventTypes.TryGetValue(eventType, out Type type))
                return (NetworkEvent)Activator.CreateInstance(type);

            return (NetworkEvent)Activator.CreateInstance(typeof(NetworkEvent), eventType);
        }

        /// <summary>
        ///     Create a <see cref="VolumeEvent"/> to be populated from serialised data.
        /// </summary>
        /// <param name="eventType">
        ///     The type of container event represented by the serialised data.
        /// </param>
        /// <returns>
        ///     The new <see cref="VolumeEvent"/>.
        /// </returns>
        VolumeEvent CreateVolumeEvent(DockerEventType eventType)
        {
            if (VolumeEventTypes.TryGetValue(eventType, out Type type))
                return (VolumeEvent)Activator.CreateInstance(type);

            return (VolumeEvent)Activator.CreateInstance(typeof(VolumeEvent), eventType);
        }
    }
}