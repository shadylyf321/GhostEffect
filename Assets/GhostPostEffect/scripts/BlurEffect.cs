
using UnityEngine;
using System.Collections;

//编辑状态下也运行
[ExecuteInEditMode]
public class BlurEffect : MonoBehaviour
{
    public Material Material;
    //模糊半径
    public float BlurRadius = 1.0f;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (Material)
        {

            //blur 
            Material.SetFloat("_BlurRadius", BlurRadius);
            Graphics.Blit(source, destination, Material);

        }
    }
}
