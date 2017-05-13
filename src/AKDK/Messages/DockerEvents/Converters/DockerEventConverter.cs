using System;
using Newtonsoft.Json.Linq;

namespace AKDK.Messages.DockerEvents.Converters
{
	class DockerEventConverter
        : JsonCreationConverter<DockerEvent>
    {
        protected override DockerEvent Create(Type objectType, JObject json)
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
                default:
				{
					throw new InvalidOperationException($"Unsupported event target ({eventTarget}).");
				}
			}

			if (dockerEvent == null)
				throw new InvalidOperationException($"Unsupported event type ({eventType}).");

            return dockerEvent;
        }

		ContainerEvent CreateContainerEvent(DockerEventType eventType)
		{
			switch (eventType)
			{
				case DockerEventType.Create:
				{
					return new ContainerCreated();
				}
				default:
				{
					return new ContainerEvent(eventType);
				}
			}
		}

		ImageEvent CreateImageEvent(DockerEventType eventType)
		{
			switch (eventType)
			{
				case DockerEventType.Pull:
				{
					return new ImagePulled();
				}
				case DockerEventType.Push:
				{
					return new ImagePushed();
				}
				default:
				{
					return new ImageEvent(eventType);
				}
			}
		}

        NetworkEvent CreateNetworkEvent(DockerEventType eventType)
        {
            switch (eventType)
            {
                case DockerEventType.Connect:
                {
                    return new NetworkConnected();
                }
                case DockerEventType.Disconnect:
                {
                    return new NetworkDisconnected();
                }
                default:
                {
                    return new NetworkEvent(eventType);
                }
            }
        }
    }
}