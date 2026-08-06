using BeyProject.Core;
using BeyProject.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Tall-grass-style trigger area. Movement is free-form (no discrete steps), so instead
    /// of rolling per-keypress, this accumulates distance traveled while the player is inside
    /// and rolls an encounter chance every fixed distance increment.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField] private BeyIdentity[] possibleOpponents;
        [Range(0f, 1f)]
        [SerializeField] private float encounterChancePerStep = 0.15f;
        [SerializeField] private float distancePerStep = 0.5f;

        private Vector3 lastPosition;
        private float distanceAccumulated;
        private bool playerInside;
        private Transform playerTransform;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            playerInside = true;
            playerTransform = other.transform;
            lastPosition = playerTransform.position;
            distanceAccumulated = 0f;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            playerInside = false;
            playerTransform = null;
        }

        private void Update()
        {
            if (!playerInside || playerTransform == null || GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.IsInPostBattleGrace)
            {
                // Just teleported back in (possibly still standing in the zone) - don't let
                // that count as movement, so no roll happens until grace ends and the player
                // actually walks somewhere.
                lastPosition = playerTransform.position;
                distanceAccumulated = 0f;
                return;
            }

            float moved = Vector3.Distance(playerTransform.position, lastPosition);
            lastPosition = playerTransform.position;
            distanceAccumulated += moved;

            while (distanceAccumulated >= distancePerStep)
            {
                distanceAccumulated -= distancePerStep;
                RollEncounter();
            }
        }

        private void RollEncounter()
        {
            if (Random.value > encounterChancePerStep)
            {
                return;
            }

            BeyIdentity opponent = PickOpponent();
            var context = new BattleContext(
                opponent != null ? opponent.id : "wild_bey",
                opponent != null ? opponent.displayName : "Wild Bey",
                opponent != null ? opponent.color : Color.green,
                SceneManager.GetActiveScene().name,
                playerTransform.position);

            playerInside = false;
            GameManager.Instance.StartBattle(context);
        }

        private BeyIdentity PickOpponent()
        {
            if (possibleOpponents == null || possibleOpponents.Length == 0)
            {
                return null;
            }

            return possibleOpponents[Random.Range(0, possibleOpponents.Length)];
        }
    }
}
