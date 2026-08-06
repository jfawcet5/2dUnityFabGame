using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Bottom-screen dialogue box, part of the persistent UI canvas. Shows one line at a
    /// time; E-press or the Continue button advances. Blocks player movement/interaction
    /// via UIInputLock while open, and runs a completion callback when the sequence ends -
    /// that's how NPCs/interactables sequence "show dialogue, then give item" safely.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text lineText;
        [SerializeField] private Button continueButton;

        private string[] lines;
        private int lineIndex;
        private Action onComplete;

        private void Awake()
        {
            Instance = this;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Advance);
            }
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.E))
            {
                Advance();
            }
        }

        public void Show(string speaker, string[] dialogueLines, Action completeCallback)
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                completeCallback?.Invoke();
                return;
            }

            lines = dialogueLines;
            lineIndex = 0;
            onComplete = completeCallback;

            if (speakerText != null)
            {
                speakerText.text = speaker;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            UIInputLock.TryAcquire(this);
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (lineText != null)
            {
                lineText.text = lines[lineIndex];
            }
        }

        private void Advance()
        {
            lineIndex++;
            if (lineIndex >= lines.Length)
            {
                Close();
                return;
            }

            ShowCurrentLine();
        }

        private void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            UIInputLock.Release(this);

            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
