using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image portraitImage;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Database")]
    public List<DialogueSO> allDialogues;
    private Dictionary<string, DialogueSO> dialogueLookup = new Dictionary<string, DialogueSO>();

    private InputSystem_Actions inputActions;
    private bool isPlaying;
    private bool advanceRequested;
    private Coroutine currentDialogueCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        BuildDialogueLookup();

        dialoguePanel.SetActive(false);

        // Initialize input
        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.started += OnMoveStarted;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void BuildDialogueLookup()
    {
        dialogueLookup.Clear();
        foreach (var d in allDialogues)
        {
            if (d == null)
            {
                Debug.LogWarning("[DialogueSystem] Null DialogueSO found in list!");
                continue;
            }

            if (string.IsNullOrEmpty(d.dialogueID))
            {
                Debug.LogWarning("[DialogueSystem] DialogueSO with empty ID detected!");
                continue;
            }

            if (dialogueLookup.ContainsKey(d.dialogueID))
            {
                Debug.LogWarning($"[DialogueSystem] Duplicate DialogueID found: {d.dialogueID}");
                continue;
            }

            dialogueLookup.Add(d.dialogueID, d);
        }
    }

    private void OnMoveStarted(InputAction.CallbackContext ctx)
    {
        if (isPlaying)
            advanceRequested = true;
    }

    public void StartDialogue(string dialogueID)
    {
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);

        currentDialogueCoroutine = StartCoroutine(PlayDialogue(dialogueID));
    }

    // In DialogueSystem
    public IEnumerator StartDialogueCoroutine(string dialogueID)
    {
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);

        currentDialogueCoroutine = StartCoroutine(PlayDialogue(dialogueID));
        yield return currentDialogueCoroutine;
    }

    private IEnumerator PlayDialogue(string dialogueID)
    {
        if (!dialogueLookup.TryGetValue(dialogueID, out DialogueSO dialogue) || dialogue == null)
        {
            Debug.LogWarning($"[DialogueSystem] No dialogue found for ID: {dialogueID}");
            yield break;
        }

        if (dialogue.lines == null || dialogue.lines.Count == 0)
        {
            Debug.LogWarning($"[DialogueSystem] Dialogue {dialogueID} has no lines!");
            yield break;
        }

        isPlaying = true;
        dialoguePanel.SetActive(true);

        foreach (var line in dialogue.lines)
        {
            if (line == null)
            {
                Debug.LogWarning($"[DialogueSystem] Null line in dialogue {dialogueID}");
                continue;
            }

            portraitImage.sprite = line.ImageSprite != null ? line.ImageSprite : null;
            speakerText.text = !string.IsNullOrEmpty(line.speakerName) ? line.speakerName : "???";

            string textToShow = !string.IsNullOrEmpty(line.text) ? line.text : "";
            yield return StartCoroutine(TypeText(textToShow));

            // Wait for player move input
            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
        }

        dialoguePanel.SetActive(false);
        isPlaying = false;
        currentDialogueCoroutine = null;
    }

    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f);
        }
    }
}

[System.Serializable]
public class DialogueLine
{
    public Sprite ImageSprite;
    public string speakerName;
    [TextArea(2, 5)] public string text;
}
