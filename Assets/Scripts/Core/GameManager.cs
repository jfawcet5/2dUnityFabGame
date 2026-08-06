using System.Collections;
using BeyProject.Overworld;
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
            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeOut();
            }

            bool loaded = false;

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
                }

                loaded = true;
            }

            SceneManager.sceneLoaded += OnRoomLoaded;
            SceneManager.LoadScene(sceneName);

            yield return new WaitUntil(() => loaded);

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
            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeOut();
            }

            bool loaded = false;

            void OnReturnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnReturnSceneLoaded;

                CurrentRoomSceneName = scene.name;

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = context.returnPosition;
                }

                LastReturnedToOverworldAt = Time.time;
                PendingBattle = null;
                loaded = true;
            }

            SceneManager.sceneLoaded += OnReturnSceneLoaded;
            SceneManager.LoadScene(context.returnSceneName);

            yield return new WaitUntil(() => loaded);

            if (SceneFadeUI.Instance != null)
            {
                yield return SceneFadeUI.Instance.FadeIn();
            }
        }
    }
}
