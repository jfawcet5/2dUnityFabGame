using BeyProject.Core;
using BeyProject.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.Overworld
{
    /// <summary>
    /// A visible rival blader / wild bey standing in the overworld. Blocks movement like a
    /// solid NPC; the player has to walk up and press the interact key to start a battle
    /// (no more auto-triggering just by touching it).
    /// </summary>
    public class OverworldOpponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private BeyIdentity identity;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Optional back-and-forth patrol")]
        [SerializeField] private bool patrol;
        [SerializeField] private float patrolDistance = 1.5f;
        [SerializeField] private float patrolSpeed = 1f;

        private Vector3 patrolStart;
        private bool triggered;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Color color = identity != null ? identity.color : Color.red;
            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(color);
            }

            patrolStart = transform.position;
        }

        private void Update()
        {
            if (!patrol)
            {
                return;
            }

            float offset = Mathf.PingPong(Time.time * patrolSpeed, patrolDistance * 2f) - patrolDistance;
            transform.position = patrolStart + new Vector3(offset, 0f, 0f);
        }

        public void Interact(GameObject interactor)
        {
            if (triggered || GameManager.Instance == null)
            {
                return;
            }

            triggered = true;

            var context = new BattleContext(
                identity != null ? identity.id : "unknown_bey",
                identity != null ? identity.displayName : "Wild Bey",
                identity != null ? identity.color : Color.red,
                SceneManager.GetActiveScene().name,
                interactor.transform.position);

            GameManager.Instance.StartBattle(context);
        }
    }
}
