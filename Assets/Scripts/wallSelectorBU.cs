using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class wallSelectorbu : MonoBehaviour
{
    public TMP_Dropdown wallselector;
    public string folderPath = "Assets/wallMaterial";
    // ������Ҳ��� "Cube"���Ԅӽ��� 1m �yԇ�����w
    public bool autoCreateTestCube = true;

    // �Ƿ�����������s���Ԅ��a���N�D tiling
    public bool autoCompensateTilingByScale = true;

    // �����N�D��ƽ�棺false=XY�������ߣ���true=XZ�������L��
    public bool useXZ = false;

    // ���� tiling �c offset��Cube ֱ�����@�M�����t�ڴ˻��A�ϳ˿s���a��
    public Vector2 baseTiling = new Vector2(1f, 1f);
    public Vector2 baseOffset = Vector2.zero;

    private bool loaded;
    private GameObject loadedObject;

    void Start()
    {
        wallselector = GetComponent<TMP_Dropdown>();
        if (wallselector == null)
        {
            Debug.LogError("wallSelector: TMP_Dropdown component not found on this GameObject.");
            return;
        }

        wallselector.ClearOptions();

        Material[] WMaterial = Resources.LoadAll<Material>("wallMaterial");
        var WMaterialNames = new List<string>(WMaterial.Length);
        foreach (var WMN in WMaterial) WMaterialNames.Add(WMN.name);
        wallselector.AddOptions(WMaterialNames);
        wallselector.RefreshShownValue();

        wallselector.onValueChanged.AddListener(changeWallMaterial);
    }

    public void WMSetLoaded(bool isLoaded, GameObject Go)
    {
        loaded = isLoaded;
        if (wallselector != null) wallselector.interactable = isLoaded;
        if (loaded) loadedObject = Go;
    }

    public void changeWallMaterial(int index)
    {
        Debug.Log("go in changeWallMaterial");
        if (wallselector == null || index < 0 || index >= wallselector.options.Count) return;

        string selectedText = wallselector.options[index].text;
        Material materialWall = Resources.Load<Material>("wallMaterial/" + selectedText);

        if (!loaded || materialWall == null)
        {
            Debug.LogWarning("Material not found or object not loaded");
            return;
        }

        // ���x�����ܾ��� Shader ���Q
        // Built-in: "Standard"
        // URP: "Universal Render Pipeline/Lit"
        // HDRP: "HDRP/Lit"
        string targetShaderName = "Standard";
        Shader targetShader = Shader.Find(targetShaderName);
        if (targetShader == null)
        {
            Debug.LogError("Target shader not found: " + targetShaderName + ". If you use URP/HDRP, change targetShaderName accordingly.");
            return;
        }

        if (materialWall.shader != targetShader) materialWall.shader = targetShader;

        // ���� URP ��Դ���|���� _BaseMap �ЈD�� _MainTex �� null���}�u�� _MainTex
        if (materialWall.HasProperty("_BaseMap") && materialWall.HasProperty("_MainTex"))
        {
            Texture baseTex = materialWall.GetTexture("_BaseMap");
            Texture mainTex = materialWall.GetTexture("_MainTex");
            if (baseTex != null && mainTex != baseTex) materialWall.SetTexture("_MainTex", baseTex);
        }

        // ���ò��|�Y�a�ϵ��N�D�s���cƫ��
        if (materialWall.HasProperty("_MainTex"))
        {
            materialWall.SetTextureScale("_MainTex", Vector2.one);
            materialWall.SetTextureOffset("_MainTex", Vector2.zero);
        }
        if (materialWall.HasProperty("_BaseMap"))
        {
            materialWall.SetTextureScale("_BaseMap", Vector2.one);
            materialWall.SetTextureOffset("_BaseMap", Vector2.zero);
        }

        Debug.Log("loaded and found Material_wall");

        GameObject spawned = loadedObject;
        if (spawned == null)
        {
            Debug.LogError("Cannot find Spawned_instance");
            return;
        }

        Debug.Log("Found Spawned_instance: " + loadedObject.name);

        MeshRenderer firstWallRenderer = null;

        // ���õ����� "Wall" �����
        foreach (Transform child in spawned.transform)
        {
            if (!child.name.Contains("Wall"))
            {
                Debug.Log("Found " + child.name);
                continue;
            }

            MeshRenderer rend = child.GetComponent<MeshRenderer>();
            if (rend == null)
            {
                Debug.LogError("the child " + child.name + " doesnt have MeshRenderer");
                continue;
            }

            // ��Q���� submesh ���|��
            Material[] mats = rend.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = materialWall;
            rend.sharedMaterials = mats;

            // Ӌ��Ҫ���õ� tiling
            Vector2 tilingToApply = baseTiling;
            if (autoCompensateTilingByScale)
            {
                Vector3 s = rend.transform.lossyScale;
                float scaleX = Mathf.Max(0.0001f, Mathf.Abs(s.x));
                float scaleY_or_Z = useXZ ? Mathf.Max(0.0001f, Mathf.Abs(s.z)) : Mathf.Max(0.0001f, Mathf.Abs(s.y));
                tilingToApply = new Vector2(baseTiling.x * scaleX, baseTiling.y * scaleY_or_Z);
            }

            // ��� MPB �K�O�� tiling/offset
            ClearRendererOverrides(rend);
            SetTilingPerRenderer(rend, tilingToApply, baseOffset);

            // �p�ٹ���׃�򣨃H�yԇ��
            rend.lightmapIndex = -1;
            rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Debug.Log("changed " + child.name + " Material!!");

            if (firstWallRenderer == null) firstWallRenderer = rend;
        }

        // ȡ�û��Ԅӽ����yԇ Cube
        MeshRenderer cubeRend = EnsureTestCube(out GameObject cubeGO);
        if (cubeRend != null)
        {
            // ����ͬһ���|
            Material[] matsC = cubeRend.sharedMaterials;
            if (matsC == null || matsC.Length == 0) matsC = new Material[1];
            for (int i = 0; i < matsC.Length; i++) matsC[i] = materialWall;
            cubeRend.sharedMaterials = matsC;

            // Cube ���� 1m ���ʣ������s���a��
            ClearRendererOverrides(cubeRend);
            SetTilingPerRenderer(cubeRend, baseTiling, baseOffset);

            cubeRend.lightmapIndex = -1;
            cubeRend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        // ݔ���������I
        LogRenderer("WALL", firstWallRenderer);
        LogRenderer("CUBE", cubeRend);

        if (firstWallRenderer != null)
        {
            for (int i = 0; i < firstWallRenderer.sharedMaterials.Length; i++)
            {
                Material m = firstWallRenderer.sharedMaterials[i];
                string mname = (m != null) ? m.name : "null";
                Debug.Log("WALL mat slot " + i + ": " + mname);
            }
        }
        if (cubeRend != null)
        {
            for (int i = 0; i < cubeRend.sharedMaterials.Length; i++)
            {
                Material m = cubeRend.sharedMaterials[i];
                string mname = (m != null) ? m.name : "null";
                Debug.Log("CUBE mat slot " + i + ": " + mname);
            }
        }
    }

    // ���ɻ�ȡ�Üyԇ Cube��1m��������
    private MeshRenderer EnsureTestCube(out GameObject cubeGO)
    {
        cubeGO = GameObject.Find("Cube");
        if (cubeGO == null && autoCreateTestCube)
        {
            cubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeGO.name = "Cube";
            // �[������ҕҰ��λ��
            cubeGO.transform.position = new Vector3(2f, 0.5f, 2f);
            cubeGO.transform.localScale = Vector3.one;
        }
        MeshRenderer r = null;
        if (cubeGO != null) r = cubeGO.GetComponent<MeshRenderer>();
        return r;
    }

    // �� MPB �O��ÿ�� Renderer ���N�D�s�ţ�Built-in: _MainTex_ST��URP: _BaseMap_ST��
    void SetTilingPerRenderer(MeshRenderer rend, Vector2 tiling, Vector2 offset)
    {
        if (rend == null) return;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);

        Material mat = rend.sharedMaterial;
        if (mat != null)
        {
            if (mat.HasProperty("_MainTex"))
                mpb.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
            if (mat.HasProperty("_BaseMap"))
                mpb.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        }

        rend.SetPropertyBlock(mpb);
    }

    // ��� MPB
    void ClearRendererOverrides(MeshRenderer r)
    {
        if (!r) return;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.Clear();
        r.SetPropertyBlock(mpb);
    }

    // ݔ�� Renderer �c���|�YӍ
    void LogRenderer(string label, MeshRenderer r)
    {
        if (!r)
        {
            Debug.Log(label + ": no renderer");
            return;
        }

        string info = label +
                      ": name=" + r.name +
                      " lightmapIndex=" + r.lightmapIndex +
                      " probeUsage=" + r.reflectionProbeUsage +
                      " materials=" + r.sharedMaterials.Length;
        Debug.Log(info);

        Material[] mats = r.sharedMaterials;
        if (mats != null)
        {
            for (int i = 0; i < mats.Length; i++) LogMat(label + "[mat " + i + "]", mats[i]);
        }
    }

    void LogMat(string label, Material m)
    {
        if (m == null)
        {
            Debug.Log(label + ": null material");
            return;
        }

        Texture texMain = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        Texture texBase = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;

        Vector2 tilingMain = Vector2.one;
        Vector2 offsetMain = Vector2.zero;
        Vector2 tilingBase = Vector2.one;
        Vector2 offsetBase = Vector2.zero;

        if (m.HasProperty("_MainTex"))
        {
            tilingMain = m.GetTextureScale("_MainTex");
            offsetMain = m.GetTextureOffset("_MainTex");
        }
        if (m.HasProperty("_BaseMap"))
        {
            tilingBase = m.GetTextureScale("_BaseMap");
            offsetBase = m.GetTextureOffset("_BaseMap");
        }

        string shaderName = (m.shader != null) ? m.shader.name : "null";
        string keywordsJoined = (m.shaderKeywords != null && m.shaderKeywords.Length > 0)
            ? string.Join(" ", m.shaderKeywords)
            : "";

        Debug.Log(label + ": shader=" + shaderName + " keywords=[" + keywordsJoined + "]");
        Debug.Log(label + ": _MainTex=" + ((texMain != null) ? texMain.name : "null") + " tiling=" + tilingMain + " offset=" + offsetMain);
        Debug.Log(label + ": _BaseMap=" + ((texBase != null) ? texBase.name : "null") + " tiling=" + tilingBase + " offset=" + offsetBase);

        if (m.HasProperty("_BumpMap"))
        {
            Texture bump = m.GetTexture("_BumpMap");
            Debug.Log(label + ": _BumpMap=" + ((bump != null) ? bump.name : "null"));
        }
        if (m.HasProperty("_MetallicGlossMap"))
        {
            Texture mgm = m.GetTexture("_MetallicGlossMap");
            Debug.Log(label + ": _MetallicGlossMap=" + ((mgm != null) ? mgm.name : "null"));
        }
        if (m.HasProperty("_OcclusionMap"))
        {
            Texture occ = m.GetTexture("_OcclusionMap");
            Debug.Log(label + ": _OcclusionMap=" + ((occ != null) ? occ.name : "null"));
        }
    }
}