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

    
    public EditButton editButton;
    private bool interactable;


    void Start()
    {

        var go = GameObject.Find("select_ITEM");
        if (go == null) { Debug.LogError("[DropdownPopulator] 'select_ITEM' not found"); return; }
        ItemList = go.GetComponent<TMP_Dropdown>();
        if (ItemList == null) { Debug.LogError("[DropdownPopulator] TMP_Dropdown missing on 'select_ITEM'"); return; }
       

        // Clear existing options
        ItemList.options.Clear();

        var prefabs = Resources.LoadAll<GameObject>("item");
        var prefabNames = new List<string>(prefabs.Length);
        foreach (var p in prefabs) prefabNames.Add(p.name);
        ItemList.AddOptions(prefabNames);
        ItemList.RefreshShownValue();

        Debug.Log($"[DropdownPopulator] Loaded {prefabNames.Count} options");


    }
    
    // Update is called once per frame
    void Update()
    {


        ItemList.interactable = editButton.editMode;
        if (ItemList.options == null || ItemList.options.Count == 0) return;
        int idx = ItemList.value;
        if (idx < 0 || idx >= ItemList.options.Count) return;

        selectedText = ItemList.options[idx].text ?? "";
        
        if (selectedText.Contains("ch")){
            itemScale = 0.208f;
        }
        else if (selectedText.Contains("Desk")){
            itemScale = 0.15f;
        }
        else
        {
            itemScale = 0.2f;
        }


    }
}
