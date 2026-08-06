using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Dropped into every scene (Main Menu, each room, Battle). Ensures the persistent
    /// systems prefab (GameManager/Inventory/SaveSystem/AudioManager/EventSystem/UI canvas)
    /// exists no matter which scene Play mode happens to start from - only the first one
    /// loaded actually instantiates it; DontDestroyOnLoad keeps it alive after that.
    /// </summary>
    public class PersistentSystemsLoader : MonoBehaviour
    {
        private const string PrefabResourcePath = "PersistentSystems";

        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                var prefab = Resources.Load<GameObject>(PrefabResourcePath);
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
                else
                {
                    Debug.LogError($"PersistentSystemsLoader: could not find Resources/{PrefabResourcePath}.prefab");
                }
            }
        }
    }
}
