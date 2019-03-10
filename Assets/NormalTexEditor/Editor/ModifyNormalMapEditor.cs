using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ModifyNormalMap))]
public class ModifyNormalMapEditor : Editor
{
    public enum PaintMode { None, Paint, Direction }

    public PaintMode CurrentMode = PaintMode.None;
    RaycastHit m_LastHitInfo;
    Vector3 m_LastRetrivedNormalDirection;
    bool m_LastHitSuccess = false;
    bool m_EditMode = false;
    Tool m_LastTool = Tool.Move;

    //float radius, float hardness, float strength
    public static int BrushRadius = 10;
    public static float BrushHardness = 0.5f;
    public static float BrushStrength = 1f;
    public static Vector3 EularBrushDirection = Vector3.zero;

    public void OnEnable()
    {
        return;
        //var mn = target as ModifyNormalMap;
        //var mr = mn.GetComponent<MeshRenderer>();
        //var mf = mn.GetComponent<MeshFilter>();
        //if(mr == null || mf == null || mf.sharedMesh == null || mr.sharedMaterial == null)
        //{
        //    Debug.Log("CouldNot found proper mesh or renderer");
        //    return;
        //}

        //goTo = new GameObject("[Temp]Painter");
        //goTo.hideFlags = HideFlags.DontSave; //| HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        //var mfTo = goTo.AddComponent<MeshFilter>();
        //var mrTo = goTo.AddComponent<MeshRenderer>();
        ////mr.bounds = new Bounds(new Vector3(0.5f, 0.5f, 0), Vector3.one);

        //mfTo.sharedMesh = mf.sharedMesh;
        //mrTo.sharedMaterial = new Material(Shader.Find("Unlit/NormalWriter"));
        ////mrTo.enabled = false;


        //var cam = goTo.AddComponent<Camera>();
        //cam.clearFlags = CameraClearFlags.SolidColor;
        //cam.backgroundColor = new Color32(128, 128, 255, 0);
        //cam.orthographic = true;
        //cam.orthographicSize = 0.5f;
        //cam.nearClipPlane = -1;
        //cam.farClipPlane = 1;
        //cam.useOcclusionCulling = false;
        //cam.allowMSAA = false;
        //cam.allowHDR = false;
        //cam.forceIntoRenderTexture = true;
        //cam.depth = -10;

        //Texture2D normalTex = null;
        //foreach(var id in mr.sharedMaterial.GetTexturePropertyNameIDs())
        //{
        //    var tex = mr.sharedMaterial.GetTexture(id);
        //    if (!EditorUtility.IsPersistent(tex)) continue;
        //    var texImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        //    if (texImporter == null || texImporter.textureType != TextureImporterType.NormalMap) continue;
        //    normalTex = tex as Texture2D;
        //    break;
        //}

        //if (normalTex == null)
        //{
        //    Debug.Log("CouldNot found normal map");
        //    return;
        //}

        //PaintingTexture = new Texture2D(normalTex.width, normalTex.height);
        //mrTo.sharedMaterial.SetTexture("_OriginalNormal", normalTex);
        //mrTo.sharedMaterial.SetTexture("_PaintingNormal", PaintingTexture);

    }

    public void OnDisable()
    {
        //if (rend) DestroyImmediate(rend.gameObject);
        //if (PaintingTexture) DestroyImmediate(PaintingTexture);
        CurrentMode = PaintMode.None;
    }


    public void OnSceneGUI()
    {
        //draw default guis
        DrawDefaultControls();
        var typedTarget = target as ModifyNormalMap;

        if (m_EditMode)
        {
            Event e = Event.current;
            //switch paint mode
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (e.shift)
                {
                    if(m_LastHitSuccess)
                    {
                        ApplyCurrentHitNormalToBrushDirection(m_LastRetrivedNormalDirection, null);
                    }
                }
                else if (e.control)
                    CurrentMode = PaintMode.Direction;
                else
                {
                    CurrentMode = PaintMode.Paint;
                    var go = typedTarget.GetComponentInChildren<MeshRenderer>();
                    var normal = go.transform.InverseTransformDirection((Quaternion.Euler(EularBrushDirection) * Vector3.forward));
                    normal = (normal + Vector3.one) * 0.5f;
                    typedTarget.DrawMaterial.SetColor("_WorldNormal", new Color(normal.x, normal.y, normal.z, 1));

                    Undo.RegisterCompleteObjectUndo(typedTarget.ResultNormalRT, "Paint");
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if(CurrentMode == PaintMode.Paint)
                {
                    //copy currently modified to result
                    Graphics.CopyTexture(typedTarget.ResultNormalRT, typedTarget.OriginalNormalRT);
                    //clear painting
                    typedTarget.FillMat.SetColor("_FillColor", Color.clear);
                    Graphics.Blit(null, typedTarget.PaintRT, typedTarget.FillMat);
                    //render
                    RenderToNormalTex();
                    //record - currently does not work
                    //Undo.RecordObjects(new Object[] { typedTarget.ResultNormalRT, typedTarget.OriginalNormalRT }, "Paint");
                }
                CurrentMode = PaintMode.None;
            }

            //update hit when it's not direction mode
            if(CurrentMode != PaintMode.Direction)
            {
                Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));
                m_LastHitSuccess = Physics.Raycast(r, out m_LastHitInfo);
            }

            //we draw arrows first of all (possible invisible bug when leading graphics.blit)
            if (e.type == EventType.Repaint && m_LastHitSuccess)
            {
                m_LastRetrivedNormalDirection = GetWorldNormal(m_LastHitInfo, typedTarget.ResultNormalRT);
                DrawArrowHandle(m_LastHitInfo.point, Quaternion.Euler(EularBrushDirection), Color.cyan);
                DrawArrowHandle(m_LastHitInfo.point, Quaternion.LookRotation(m_LastRetrivedNormalDirection), Color.yellow, 0.5f);
            }

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                if(CurrentMode == PaintMode.Direction)
                {
                    RotateDirection(ref EularBrushDirection, e.delta);
                }

                if(m_LastHitSuccess && CurrentMode == PaintMode.Paint)
                {
                    //do paint
                    var radius01 = BrushRadius * typedTarget.OriginalAlbedo.texelSize.x;
                    SetPaintValue(typedTarget.BrushMat, GetTexCoord(m_LastHitInfo), radius01, BrushHardness, BrushStrength);
                    DrawToTexture(typedTarget.PaintRT, typedTarget.TempPaintRT, typedTarget.BrushMat);
                    RenderToNormalTex();
                }
            }

            //if eventtype != repaint, blit captures some wierd color
            if (e.type == EventType.Repaint && CurrentMode != PaintMode.Direction)
            {
                if(m_LastHitSuccess)
                {
                    var radius01 = BrushRadius * typedTarget.OriginalAlbedo.texelSize.x;
                    SetPaintValue(typedTarget.BrushPointMat, GetTexCoord(m_LastHitInfo), radius01, BrushHardness, BrushStrength);
                    Graphics.Blit(typedTarget.OriginalAlbedo, typedTarget.BrushPointRT, typedTarget.BrushPointMat);
                }
                else
                {
                    Graphics.CopyTexture(typedTarget.OriginalAlbedo, 0, 0, typedTarget.BrushPointRT, 0, 0);
                }

                SceneView.RepaintAll();
            }
        }
    }

    public void RenderToNormalTex()
    {
        var typedTarget = target as ModifyNormalMap;
        typedTarget.RenderCamera.Render();
        Graphics.Blit(typedTarget.ResultNormalRT, typedTarget.RealtimeViewRT, typedTarget.RealtimeMat);
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    /// <summary>
    /// set paint material values
    /// </summary>
    public static void SetPaintValue(Material paintMat, Vector2 texcoord, float radius, float hardness, float strength)
    {
        //_XCoord("XCoord", Float) = 0.5
        //_YCoord("YCoord", Float) = 0.5
        //_Radius("Radius", Float) = 0.5
        //_Hardness("Hardness", Float) = 0.5
        //_Strength("Strength", Float) = 0.5

        paintMat.SetFloat("_XCoord", Mathf.Clamp01(texcoord.x));
        paintMat.SetFloat("_YCoord", Mathf.Clamp01(texcoord.y));
        paintMat.SetFloat("_Radius", Mathf.Clamp01(radius));
        paintMat.SetFloat("_Hardness", Mathf.Clamp01(hardness));
        paintMat.SetFloat("_Strength", Mathf.Clamp01(strength));
    }


    /// <summary>
    /// draw paintmat additively into dest rendertexture.
    /// </summary>
    public static void DrawToTexture(RenderTexture destRT, RenderTexture tempRT, Material paintMat)
    {
        //copy destRT to temp 
        //Graphics.CopyTexture(destRT, tempRT);
        Graphics.Blit(tempRT, destRT, paintMat);
    }

    public static void DrawArrowHandle(Vector3 position, Quaternion direction, Color color, float size = 1)
    {
        if (Event.current.type != EventType.Repaint)
            return;
        var colorCache = Handles.color;
        Handles.color = color;
        Handles.ArrowHandleCap(0, position, direction, HandleUtility.GetHandleSize(position) * size, EventType.Repaint);
        Handles.color = colorCache;
    }


    static Texture2D s_TempTex;
    /// <summary>
    /// Get current hit's world normal from normal map and vertex normal info
    /// </summary>
    public static Vector3 GetWorldNormal(RaycastHit hitInfo, RenderTexture normalMap)
    {
        var MC = hitInfo.collider as MeshCollider;
        var M = MC.sharedMesh;
        var normals = M.normals;
        var triangles = M.triangles;
        var indices = new int[] { triangles[hitInfo.triangleIndex * 3 + 0], triangles[hitInfo.triangleIndex * 3 + 1], triangles[hitInfo.triangleIndex * 3 + 2] };
        var tangents = M.tangents;
        var uvs = M.uv;

        var baryCoord = hitInfo.barycentricCoordinate;
        var localNormal = (baryCoord[0] * normals[indices[0]] + baryCoord[1] * normals[indices[1]] + baryCoord[2] * normals[indices[2]]).normalized;
        var localUV = (baryCoord[0] * uvs[indices[0]] + baryCoord[1] * uvs[indices[1]] + baryCoord[2] * uvs[indices[2]]);
        var localTangentTemp = (baryCoord[0] * tangents[indices[0]] + baryCoord[1] * tangents[indices[1]] + baryCoord[2] * tangents[indices[2]]);
        var localTangent = (new Vector3(localTangentTemp.x, localTangentTemp.y, localTangentTemp.z)).normalized;
        var localBinormal = Vector3.Cross(localNormal, localTangent) * localTangentTemp.w;

        Matrix4x4 tanToObjRotation = new Matrix4x4(localTangent, localBinormal, localNormal, new Vector4(0, 0, 0, 1));

        var prevRT = RenderTexture.active;
        RenderTexture.active = normalMap;
        if(s_TempTex == null) s_TempTex = new Texture2D(normalMap.width, normalMap.height);

        var rect = new Rect(0, 0, normalMap.width, normalMap.height);
        s_TempTex.ReadPixels(rect, 0, 0);
        s_TempTex.Apply();
        Color packedNormal = s_TempTex.GetPixel((int)(normalMap.width * localUV.x), (int)(normalMap.height * localUV.y));
        RenderTexture.active = prevRT;
        
        //var packedNormal = normalMap != null? normalMap.GetPixel((int)(normalMap.texelSize.x * localUV.x), (int)(normalMap.texelSize.y * localUV.y)) : new Color(0.5f, 0.5f, 1f);
        var tangentNormal = new Vector3(packedNormal.r * 2 - 1, packedNormal.g * 2 - 1, packedNormal.b * 2 - 1);
        return MC.transform.TransformDirection(tanToObjRotation * tangentNormal);
    }


    public static Vector2 GetTexCoord(RaycastHit hitInfo)
    {
        var MC = hitInfo.collider as MeshCollider;
        var M = MC.sharedMesh;
        var triangles = M.triangles;
        var indices = new int[] { triangles[hitInfo.triangleIndex * 3 + 0], triangles[hitInfo.triangleIndex * 3 + 1], triangles[hitInfo.triangleIndex * 3 + 2] };
        var uvs = M.uv;
        var baryCoord = hitInfo.barycentricCoordinate;
        var localUV = (baryCoord[0] * uvs[indices[0]] + baryCoord[1] * uvs[indices[1]] + baryCoord[2] * uvs[indices[2]]);
        return localUV;
    }

    public static void RotateDirection(ref Vector3 eularDirection, Vector2 delta)
    {
        eularDirection.y = (eularDirection.y - delta.x + 540) % 360 - 180;
        eularDirection.x = Mathf.Clamp((eularDirection.x + delta.y), -90, 90);
    }

    void DrawDefaultControls()
    {
        var typedTarget = target as ModifyNormalMap;
        if(m_EditMode)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Handles.BeginGUI();
            GUILayout.BeginVertical(GUILayout.Width(200), GUILayout.Height(200));
            GUILayout.Label("Brush Radius(pixel) : " + BrushRadius.ToString("0"));
            BrushRadius = (int)EditorGUILayout.Slider(BrushRadius, 1, 100, GUILayout.Width(200));
            GUILayout.Label("Brush Hardness : " + BrushHardness.ToString("0.00"));
            BrushHardness = EditorGUILayout.Slider(BrushHardness, 0f, 1f, GUILayout.Width(200));
            GUILayout.Label("Brush Strength : " + BrushStrength.ToString("0.00"));
            BrushStrength = EditorGUILayout.Slider(BrushStrength, 0f, 1f, GUILayout.Width(200));
            GUILayout.Space(10);
            GUILayout.Label("Brush Rotation Eular X : " + EularBrushDirection.x.ToString("0.0"));
            EularBrushDirection.x = EditorGUILayout.Slider(EularBrushDirection.x, -90, 90, GUILayout.Width(200));
            GUILayout.Label("Brush Rotation Eular Y : " + EularBrushDirection.y.ToString("0.0"));
            EularBrushDirection.y = EditorGUILayout.Slider(EularBrushDirection.y, -180, 180, GUILayout.Width(200));
            GUILayout.Space(10);
            GUILayout.Label("Contrl + Drag on surface : Rotate Brush");
            GUILayout.Label("Shift + Click on surface : Substract Normal Rotation");


            if (GUILayout.Button("ClearEdited", GUILayout.Width(100), GUILayout.Height(100)))
            {
                if (typedTarget.OriginalNormal == null)
                {
                    typedTarget.FillMat.SetColor("_FillColor", new Color(0.5f, 0.5f, 1f, 1f));
                    Graphics.Blit(null, typedTarget.OriginalNormalRT, typedTarget.FillMat);
                }
                else
                {
                    Graphics.CopyTexture(typedTarget.OriginalNormal, 0, 0, typedTarget.OriginalNormalRT, 0, 0);
                }
                RenderToNormalTex();
            }

            if (GUILayout.Button("End Edit Mode", GUILayout.Width(100), GUILayout.Height(100)))
            {
                m_EditMode = !m_EditMode;
                Tools.current = m_LastTool;
            }

            GUILayout.EndVertical();
            Handles.EndGUI();
        }
        else
        {
            Handles.BeginGUI();
            if (GUILayout.Button("Start Edit Mode", GUILayout.Width(100), GUILayout.Height(100)))
            {
                m_EditMode = !m_EditMode;
                m_LastTool = Tools.current;
                Tools.current = Tool.None;
            }
            Handles.EndGUI();
        }
    }

    public static void ApplyCurrentHitNormalToBrushDirection(Vector3 direction, Texture2D normalMap)
    {
        var eular = Quaternion.LookRotation(direction).eulerAngles;
        eular.x = eular.x % 360;
        if (eular.x > 90) eular.x -= 360;
        eular.y = (eular.y + 540) % 360 - 180;
        EularBrushDirection = eular;
    }

    public static void DrawMeshToRenderTexture(RenderTexture destRT)
    {
        Graphics.SetRenderTarget(destRT);

        Graphics.SetRenderTarget(null);
    }
}
