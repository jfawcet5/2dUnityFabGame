using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// Plain linear dialogue content - a speaker name and a sequence of lines shown one at
    /// a time. No branching/choices; the requirements explicitly don't need quests, so this
    /// is intentionally the simplest thing that supports "NPCs have dialogue."
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "Bey Project/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string speakerName = "???";
        [TextArea]
        public string[] lines = new string[0];
    }
}
