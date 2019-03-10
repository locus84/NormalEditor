using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ModifyNormalWindow : EditorWindow
{
    string m_MainTexName = "_MainTex";
    string m_NormalTexName = "_BumpMap";
    bool m_AutoCreateNormalMap = false;
    int m_AutoCreateSize = 256;
    GameObject m_SelectedObject = null;

    // Start is called before the first frame update
    [MenuItem("Assets/NormalEditor")]
    static void Init()
    {
        //Selection.activeGameObject
        var activeGo = Selection.activeGameObject;

        if (!CheckComponents(activeGo))
            return;

        var window = GetWindow<ModifyNormalWindow>();
        window.m_SelectedObject = Selection.activeGameObject;
        window.Show();
    }

    // Update is called once per frame
    void OnGUI()
    {
        GUILayout.Label("Normal map editing creation settings");
        EditorGUILayout.ObjectField("Selected Object", m_SelectedObject, typeof(GameObject), false);
        m_MainTexName = EditorGUILayout.TextField("MainTexture Name(in shader)", m_MainTexName);
        m_NormalTexName = EditorGUILayout.TextField("NormalTexture Name(in shader)", m_NormalTexName);
        m_AutoCreateNormalMap = EditorGUILayout.BeginToggleGroup("Auto Create Normal", m_AutoCreateNormalMap);
        m_AutoCreateSize = EditorGUILayout.IntField("Size", m_AutoCreateSize);
        EditorGUILayout.EndToggleGroup();

        if(GUILayout.Button("Create Normal Modifier Object", GUILayout.Height(100)))
        {
            CreateNormalModifier(m_SelectedObject, m_MainTexName, m_NormalTexName, m_AutoCreateNormalMap, m_AutoCreateSize);
        }
    }

    static void CreateNormalModifier(GameObject selected, string mainTexName, string normalTexName, bool autoNormalCreate, int autoNormalSize)
    {
        if (!CheckComponents(selected))
        {
            return;
        }

        var mat = selected.GetComponent<MeshRenderer>().sharedMaterial;
        var texNameList = new List<string>(mat.GetTexturePropertyNames());

        if(!texNameList.Contains(mainTexName) || !texNameList.Contains(normalTexName))
        {
            Debug.LogWarning("Main or Normal texture name is invalid");
            return;
        }

        var mainTex = mat.GetTexture(mainTexName);
        var normalTex = mat.GetTexture(normalTexName);

        if(mainTex == null)
        {
            Debug.LogWarning("Main texture is not set");
            return;
        }

        if(!autoNormalCreate && normalTex == null)
        {
            Debug.LogWarning("Normal texure is not set");
            return;
        }

        var mainTexCopy = new RenderTexture(mainTex.width, mainTex.height, 0);
        Graphics.CopyTexture(mainTex, 0, 0, mainTexCopy, 0, 0);

        RenderTexture normalTexCopy;
        var fillMaterial = new Material(Shader.Find("NormalEditor/FillShader"));
        var realtimeViewMat = new Material(Shader.Find("NormalEditor/RealtimeViewer"));

        if (normalTex == null)
        {
            normalTexCopy = new RenderTexture(autoNormalSize, autoNormalSize, 0, RenderTextureFormat.ARGB32);

            fillMaterial.SetColor("_FillColor", new Color(0.5f, 0.5f, 1f, 1f));
            Graphics.Blit(null, normalTexCopy, fillMaterial);
        }
        else
        {
            normalTexCopy = new RenderTexture(normalTex.width, normalTex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.CopyTexture(normalTex, 0, 0, normalTexCopy, 0, 0);
        }

        var normalTexResult = new RenderTexture(normalTexCopy);
        Graphics.CopyTexture(normalTexCopy, normalTexResult);

        var realtimeViewRT = new RenderTexture(normalTexCopy);
        Graphics.Blit(normalTexCopy, realtimeViewRT, realtimeViewMat);

        var rootGo = new GameObject("NormalModifier : " + selected.name);
        var modifier = rootGo.AddComponent<ModifyNormalMap>();

        var initiated = Instantiate(selected);
        initiated.transform.position = Vector3.zero;
        initiated.transform.rotation = selected.transform.rotation;
        initiated.transform.localScale = selected.transform.lossyScale;

        initiated.transform.parent = rootGo.transform;
        if(initiated.GetComponent<MeshCollider>() == null)
            initiated.AddComponent<MeshCollider>();
        initiated.GetComponent<MeshCollider>().sharedMesh = initiated.GetComponent<MeshFilter>().sharedMesh;

        modifier.OriginalNormal = normalTex;
        modifier.OriginalNormalRT = normalTexCopy;
        modifier.ResultNormalRT = normalTexResult;
        modifier.RealtimeViewRT = realtimeViewRT;

        modifier.OriginalAlbedo = mainTex;
        modifier.BrushPointRT = mainTexCopy;

        //create paint textures and clear it
        modifier.PaintRT = new RenderTexture(normalTexCopy.width, normalTexCopy.height, 0);
        modifier.TempPaintRT = new RenderTexture(normalTexCopy.width, normalTexCopy.height, 0);
        fillMaterial.SetColor("_FillColor", Color.clear);
        Graphics.Blit(null, modifier.PaintRT, fillMaterial);
        Graphics.Blit(null, modifier.TempPaintRT, fillMaterial);

        //discard shared material(as we don't want to modify it directly)
        //and create new one
        var tempMat = new Material(mat);
        tempMat.SetTexture(mainTexName, modifier.BrushPointRT);
        tempMat.SetTexture(normalTexName, modifier.RealtimeViewRT);
        initiated.GetComponent<MeshRenderer>().material = tempMat;

        //fix for normal map is not updated on standard shader
        tempMat.EnableKeyword("_NORMALMAP");

        //make some needed shaders for painting
        modifier.BrushPointMat = new Material(Shader.Find("NormalEditor/BrushShader"));
        modifier.BrushMat = new Material(Shader.Find("NormalEditor/PaintShader"));
        //assign fill material priviousely used
        modifier.FillMat = fillMaterial;
        modifier.RealtimeMat = realtimeViewMat;


        ////set up camera to read unwarpped normal
        var cam = rootGo.AddComponent<Camera>();
        cam.enabled = false;
        cam.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.5f, 0.5f, 1f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 0.5f;
        cam.nearClipPlane = -1;
        cam.farClipPlane = 1;
        cam.useOcclusionCulling = false;
        cam.allowMSAA = false;
        cam.allowHDR = false;
        cam.forceIntoRenderTexture = true;
        cam.targetTexture = modifier.ResultNormalRT;
        cam.cullingMask = 0;
        modifier.RenderCamera = cam;

        modifier.DrawMesh = initiated.GetComponent<MeshFilter>().sharedMesh;
        modifier.DrawMaterial = new Material(Shader.Find("NormalEditor/NormalWriter"));
        modifier.DrawMaterial.SetTexture("_OriginalNormal", modifier.OriginalNormalRT);
        modifier.DrawMaterial.SetTexture("_PaintingNormal", modifier.PaintRT);
    }

    static bool CheckComponents(GameObject selected)
    {
        if (selected.GetComponent<MeshRenderer>() == null || selected.GetComponent<MeshFilter>() == null)
        {
            Debug.LogWarning("Does not contains mesh renderor/filter");
            return false;
        }

        if (selected.GetComponent<MeshRenderer>().sharedMaterial == null || selected.GetComponent<MeshFilter>().sharedMesh == null)
        {
            Debug.LogWarning("Could not find mesh or material");
            return false;
        }

        return true;
    }
}
