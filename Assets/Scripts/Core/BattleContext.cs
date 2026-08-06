using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Carries what's needed to enter a battle and return to the right spot
    /// in the overworld afterwards. Plain data only - no battle logic here.
    /// </summary>
    [System.Serializable]
    public class BattleContext
    {
        public string opponentId;
        public string opponentDisplayName;
        public Color opponentColor;

        public string returnSceneName;
        public Vector2 returnPosition;

        public BattleContext(string opponentId, string opponentDisplayName, Color opponentColor,
            string returnSceneName, Vector2 returnPosition)
        {
            this.opponentId = opponentId;
            this.opponentDisplayName = opponentDisplayName;
            this.opponentColor = opponentColor;
            this.returnSceneName = returnSceneName;
            this.returnPosition = returnPosition;
        }
    }
}
