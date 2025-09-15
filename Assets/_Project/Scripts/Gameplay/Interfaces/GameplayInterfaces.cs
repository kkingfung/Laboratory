using UnityEngine;

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Interface for objects that can be interacted with by players
    /// </summary>
    public interface IInteractable
    {
        void Interact(Laboratory.Subsystems.Player.PlayerController player);
        string GetInteractionPrompt();
        bool CanInteract(Laboratory.Subsystems.Player.PlayerController player);
    }
}
