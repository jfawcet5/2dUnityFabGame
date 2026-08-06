using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Anything the player can walk up to and press the interact key on -
    /// NPCs/rival bladers to start a battle, item pickups if they need a prompt later, etc.
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}
