using Unity.Entities;
using Cysharp.Threading.Tasks;
using Infrastructure; // For NetworkClient
using System;
// FIXME: tidyup after 8/29
#nullable enable

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class NetworkSyncSystem : SystemBase
    {
        private NetworkClient? _networkClient;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Resolve NetworkClient from ServiceLocator
            var services = new Infrastructure.ServiceLocator();
            if (services.TryResolve<NetworkClient>(out var client))
            {
                _networkClient = client;
            }
            else
            {
                UnityEngine.Debug.LogWarning("NetworkClient not found in ServiceLocator.");
            }
        }

        protected override void OnUpdate()
        {
            if (_networkClient == null || !_networkClient.IsConnected)
                return;

            // Gather local player entities and send their state to the server
            Entities
                .WithAll<PlayerStateComponent, PlayerInputComponent>()
                .ForEach((in PlayerStateComponent state, in PlayerInputComponent input) =>
                {
                    // TODO: Serialize state and input into a network message

                    // Example: Send serialized data (pseudocode)
                    // byte[] message = SerializePlayerState(state, input);
                    // _networkClient.SendAsync(message).Forget();
                }).WithoutBurst().Run();

            // TODO: Apply incoming network messages to entities
            // This requires a network message queue processed elsewhere or event-driven updates
        }
    }
}
