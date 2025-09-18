using UnityEngine;
using Laboratory.Subsystems.Player;

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Interface for objects that can be interacted with by players
    /// </summary>
    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetInteractionPrompt();
        bool CanInteract(PlayerController player);
    }
}
