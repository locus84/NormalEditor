using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ModifyNormalMap : MonoBehaviour
{
    public Texture OriginalAlbedo;
    public RenderTexture BrushPointRT;

    public Texture OriginalNormal;
    public RenderTexture OriginalNormalRT;
    public RenderTexture ResultNormalRT;
    public RenderTexture RealtimeViewRT;

    public RenderTexture PaintRT;
    public RenderTexture TempPaintRT;

    public Material BrushPointMat;
    public Material BrushMat;
    public Material FillMat;
    public Material RealtimeMat;
    public Camera RenderCamera;

    public Mesh DrawMesh;
    public Material DrawMaterial;

    public void OnPostRender()
    {
        // set first shader pass of the material
        DrawMaterial.SetPass(0);
        // draw mesh at the origin
        var moveAmount = 1 * ResultNormalRT.texelSize.x;

        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * 2 * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * 2 * -moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * 2 * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * 2 * -moveAmount, transform.rotation);

        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * moveAmount + transform.up * -moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * -moveAmount + transform.up * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * moveAmount + transform.right * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * -moveAmount + transform.right * -moveAmount, transform.rotation);

        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.right * -moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position + transform.up * -moveAmount, transform.rotation);
        Graphics.DrawMeshNow(DrawMesh, transform.position, transform.rotation);
    }
}
