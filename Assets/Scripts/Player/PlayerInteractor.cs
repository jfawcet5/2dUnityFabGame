using BeyProject.Overworld;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Player
{
    /// <summary>
    /// Lets the player trigger nearby IInteractable objects (NPCs, rival bladers, etc.)
    /// with a keypress instead of just walking into them.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        // 0.9 was flaky against a locked door's solid collider (playerRadius 0.4 + door
        // half-extent ~0.5 sits right at the edge) - 1.0 gives reliable reach.
        [SerializeField] private float interactRadius = 1.0f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private void Update()
        {
            if (UIInputLock.IsBlocked || !Input.GetKeyDown(interactKey))
            {
                return;
            }

            IInteractable nearest = FindNearestInteractable();
            nearest?.Interact(gameObject);
        }

        private IInteractable FindNearestInteractable()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject)
                {
                    continue;
                }

                var interactable = hit.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            return nearest;
        }
    }
}
