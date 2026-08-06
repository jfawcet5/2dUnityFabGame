using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// A named marker in a room scene. GameManager.TravelToRoom looks up the spawn point
    /// matching the requested id and places the player there.
    /// </summary>
    public class RoomSpawnPoint : MonoBehaviour
    {
        public string spawnId;
    }
}
