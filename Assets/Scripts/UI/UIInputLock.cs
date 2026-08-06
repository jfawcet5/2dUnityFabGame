namespace BeyProject.UI
{
    /// <summary>
    /// Ownership-based modal gate checked by PlayerController2D/PlayerInteractor so movement
    /// and world interaction pause while a menu or dialogue box is open. Only one owner can
    /// hold it at a time, and only that same owner can release it - so one modal closing can
    /// never accidentally clear a lock a different modal is still holding.
    /// </summary>
    public static class UIInputLock
    {
        private static object owner;

        public static bool IsBlocked => owner != null;

        /// <summary>Acquires the lock for requester. Fails if a different owner already holds it.</summary>
        public static bool TryAcquire(object requester)
        {
            if (owner != null && owner != requester)
            {
                return false;
            }

            owner = requester;
            return true;
        }

        /// <summary>No-op unless requester is the current owner.</summary>
        public static void Release(object requester)
        {
            if (owner == requester)
            {
                owner = null;
            }
        }
    }
}
