using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class DropdownPopulator : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown ItemList; // Legacy UI Dropdown
    
    //private RectTransform DDTransform;
    public float itemScale;
    public string selectedText;

    private GameObject editButton;
    private EditButton editButton_Script;
    private bool interactable;


    void Start()
    {

        editButton = GameObject.Find("edit");
        if (editButton == null) { Debug.LogError("[DropdownPopulator] 'edit' not found"); return; }
        editButton_Script = editButton.GetComponent<EditButton>();
        if (editButton_Script == null) { Debug.LogError("[DropdownPopulator] EditButton not found on 'edit'"); return; }

        
        var go = GameObject.Find("select_ITEM");
        if (go == null) { Debug.LogError("[DropdownPopulator] 'select_ITEM' not found"); return; }
        ItemList = go.GetComponent<TMP_Dropdown>();
        if (ItemList == null) { Debug.LogError("[DropdownPopulator] TMP_Dropdown missing on 'select_ITEM'"); return; }
       

        // Clear existing options
        ItemList.options.Clear();

        var prefabs = Resources.LoadAll<GameObject>("");
        var prefabNames = new List<string>(prefabs.Length);
        foreach (var p in prefabs) prefabNames.Add(p.name);
        ItemList.AddOptions(prefabNames);
        ItemList.RefreshShownValue();

        Debug.Log($"[DropdownPopulator] Loaded {prefabNames.Count} options");


    }
    
    // Update is called once per frame
    void Update()
    {
        if (editButton_Script == null || ItemList == null) return;

        var dd = GetComponent<TMP_Dropdown>();
        if (dd == null) return;

        dd.interactable = editButton_Script.interactableDD;
        if (ItemList.options == null || ItemList.options.Count == 0) return;
        int idx = ItemList.value;
        if (idx < 0 || idx >= ItemList.options.Count) return;

        selectedText = ItemList.options[idx].text ?? "";
        
        if (selectedText.Contains("ch")){
            itemScale = 0.208f;
        }
        else if (selectedText.Contains("Desk")){
            itemScale = 0.20f;
        }


    }
}
