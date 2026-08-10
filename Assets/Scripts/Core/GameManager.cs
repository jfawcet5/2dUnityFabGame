using System.Collections;
using BeyProject.Overworld;
using BeyProject.Player;
using BeyProject.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.Core
{
    /// <summary>
    /// Persistent singleton that owns scene transitions: room-to-room travel (doors, main
    /// menu, save/load) and the battle round trip. Battle logic itself doesn't live here -
    /// this is just the trigger/return plumbing so the actual battle system stays undecided.
    /// Both transition paths fade out/in via SceneFadeUI (best-effort - skipped if unavailable).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string BattleSceneName = "Battle";

        [Tooltip("How long after returning from a battle before encounter zones can roll again. Prevents an instant re-encounter when the return position is inside a grass patch.")]
        public float PostBattleGraceSeconds = 1.5f;

        public static GameManager Instance { get; private set; }

        public BattleContext PendingBattle { get; private set; }

        public string CurrentRoomSceneName { get; private set; }

        public float LastReturnedToOverworldAt { get; private set; } = float.NegativeInfinity;

        public bool IsInPostBattleGrace => Time.time - LastReturnedToOverworldAt < PostBattleGraceSeconds;

        // Every room-to-room LoadScene destroys the current Player instance and the new scene
        // instantiates a completely fresh one - PlayerHealth/PlayerCombat reset to full in
        // their own Awake/Start with no idea a previous instance existed. Caching the outgoing
        // instance's resource state here (a persistent singleton, so it survives the load) and
        // pushing it into the new instance is what makes health/energy actually carry over,
        // instead of just position.
        private bool hasCachedPlayerCombatState;
        private float cachedHealth;
        private float cachedEnergy;
        private int cachedBurst;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// General-purpose room transition: door walk-throughs, "New Game," and save-file
        /// loads all funnel through this one method. Spawns the player at the RoomSpawnPoint
        /// matching spawnPointId, or at fallbackPosition if spawnPointId is null/not found.
        /// </summary>
        public void TravelToRoom(string sceneName, string spawnPointId, Vector2 fallbackPosition)
        {
            StartCoroutine(TravelToRoomRoutine(sceneName, spawnPointId, fallbackPosition));
        }

        private IEnumerator TravelToRoomRoutine(string sceneName, string spawnPointId, Vector2 fallbackPosition)
        {
            CachePlayerCombatState();

            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeOut();
            }

            bool loaded = false;
            GameObject loadedPlayer = null;

            void OnRoomLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnRoomLoaded;

                CurrentRoomSceneName = scene.name;

                Vector2 spawnPosition = fallbackPosition;
                if (!string.IsNullOrEmpty(spawnPointId))
                {
                    foreach (var spawnPoint in Object.FindObjectsOfType<RoomSpawnPoint>())
                    {
                        if (spawnPoint.spawnId == spawnPointId)
                        {
                            spawnPosition = spawnPoint.transform.position;
                            break;
                        }
                    }
                }

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = spawnPosition;
                    loadedPlayer = player;
                }

                loaded = true;
            }

            SceneManager.sceneLoaded += OnRoomLoaded;
            SceneManager.LoadScene(sceneName);

            yield return new WaitUntil(() => loaded);

            // One more frame so the new Player's own Start() (which sets fresh full
            // resources) has definitely already run before we override it - otherwise this
            // and Start() would race depending on exactly when Unity schedules Start().
            yield return null;
            RestorePlayerCombatState(loadedPlayer);

            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeIn();
            }
        }

        public void StartBattle(BattleContext context)
        {
            PendingBattle = context;
            SceneManager.LoadScene(BattleSceneName);
        }

        public void ReturnFromBattle()
        {
            BattleContext context = PendingBattle;

            if (context == null)
            {
                return;
            }

            StartCoroutine(ReturnFromBattleRoutine(context));
        }

        private IEnumerator ReturnFromBattleRoutine(BattleContext context)
        {
            CachePlayerCombatState();

            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeOut();
            }

            bool loaded = false;
            GameObject loadedPlayer = null;

            void OnReturnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnReturnSceneLoaded;

                CurrentRoomSceneName = scene.name;

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = context.returnPosition;
                    loadedPlayer = player;
                }

                LastReturnedToOverworldAt = Time.time;
                PendingBattle = null;
                loaded = true;
            }

            SceneManager.sceneLoaded += OnReturnSceneLoaded;
            SceneManager.LoadScene(context.returnSceneName);

            yield return new WaitUntil(() => loaded);

            yield return null;
            RestorePlayerCombatState(loadedPlayer);

            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeIn();
            }
        }

        /// <summary>Snapshots the outgoing Player's health/energy/burst, if one exists, before
        /// its scene unloads and destroys it.</summary>
        private void CachePlayerCombatState()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : null;

            if (health == null && combat == null)
            {
                hasCachedPlayerCombatState = false;
                return;
            }

            cachedHealth = health != null ? health.CurrentHealth : 0f;
            cachedEnergy = combat != null ? combat.CurrentEnergy : 0f;
            cachedBurst = combat != null ? combat.CurrentBurst : 0;
            hasCachedPlayerCombatState = true;
        }

        /// <summary>Pushes the snapshot from CachePlayerCombatState into the newly-loaded
        /// scene's Player instance, overriding the fresh-full values its own Start() set.</summary>
        private void RestorePlayerCombatState(GameObject player)
        {
            if (!hasCachedPlayerCombatState || player == null)
            {
                return;
            }

            player.GetComponent<PlayerHealth>()?.RestoreHealth(cachedHealth);
            player.GetComponent<PlayerCombat>()?.RestoreCombatState(cachedEnergy, cachedBurst);
        }
    }
}
