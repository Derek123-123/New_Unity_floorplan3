using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LayerSetup
{
    static LayerSetup()
    {
        CreateLayer("CubeLayer");
        CreateLayer("FloorLayer");
    }

    public static void CreateLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        bool layerExists = false;
        int emptySlot = -1;

        // Check existing layers
        for (int i = 8; i < layers.arraySize; i++) // Start from 8 to skip built-in layers
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == layerName)
            {
                layerExists = true;
                break;
            }
            if (string.IsNullOrEmpty(layerProp.stringValue) && emptySlot == -1)
            {
                emptySlot = i;
            }
        }

        if (!layerExists && emptySlot != -1)
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(emptySlot);
            layerProp.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Created layer: {layerName} at index {emptySlot}");
        }
        else if (layerExists)
        {
            Debug.Log($"Layer {layerName} already exists");
        }
        else
        {
            Debug.LogError($"No empty layer slots available to create {layerName}. Delete an unused layer in Tags and Layers settings.");
        }
    }
}