using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Story/Dialogue", fileName = "NewDialogue")]
public class DialogueSO : ScriptableObject
{
    [Header("Dialogue Info")]
    public string dialogueID;

    [Header("Dialogue Lines")]
    public List<DialogueLine> lines = new List<DialogueLine>();
}
