using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Serializable]
public enum IntervalType
{
    Time,
    Dist,
}

[RequireComponent(typeof(Camera))]
public class GhostCam : MonoBehaviour
{
    public IntervalType Type = IntervalType.Time;
    public float intervalTime = 0.13f;
    public float intervalDis = 3f;

    public Camera GhostShootCam;
    public GameObject Target = null;
    public float Intensity = 1.0f;
    public Color RimColor = new Color(0, 181 / 255f, 1, 1);

    Camera renderCamera;
    Shader GhoastShader = null;
    Shader XayRimShader = null;
    Material ghoastMaterial = null;
    Material xayRimMaterial = null;
    Material blurMaterial = null;
    RenderTexture[] textureG = new RenderTexture[2];
    RenderTexture sourceTex;
    RenderTexture curTargetTex;
    RenderTexture curTex;
    Transform skeleton;
    bool isSupport = true;
    bool isSupportRim = true;
    bool isSupportBlur = true;
    Coroutine showCor;
    Coroutine stopCor;
    Material[] materialG = new Material[2];

    Vector2 lastShootPos;
    public bool stop = false;
    float fadeout = 0.5f;

    void Awake()
    {
        renderCamera = this.GetComponent<Camera>();
        GhoastShader = Shader.Find("Custom/GhostEffect");
        if (GhoastShader != null && GhoastShader.isSupported)
            ghoastMaterial = new Material(GhoastShader);
        else
            isSupport = false;

        XayRimShader = Shader.Find("Custom/XrayRim");
        if (XayRimShader != null && XayRimShader.isSupported)
        {
            xayRimMaterial = new Material(XayRimShader);
            xayRimMaterial.SetColor("_RimColor", RimColor);
            xayRimMaterial.SetFloat("Intensity", Mathf.Clamp(Intensity, 0, 2));
        }
        else
            isSupportRim = false;

        var blurShader = Shader.Find("Custom/Blur");
        if (blurShader != null && blurShader.isSupported)
        {
            blurMaterial = new Material(blurShader);
            blurMaterial.SetFloat("_BlurRadius", 1f);
        }
        else
            isSupportBlur = false; 

        textureG[0] = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        textureG[1] = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        Graphics.SetRenderTarget(textureG[0]);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(textureG[1]);
        GL.Clear(true, true, Color.clear);
    }

    void Start()
    {
        showCor = StartCoroutine(ShowGhoastEffect());
    }

    private void Update()
    {
        if (stop)
        {
            fadeout -= Time.deltaTime;
            if (fadeout <= 0)
            {
                this.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        stop = false;
        fadeout = 0.5f;
        Graphics.SetRenderTarget(textureG[0]);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(textureG[1]);
        GL.Clear(true, true, Color.clear);
    }

    void GetTargetTextrue()
    {
        if (Target == null)
            return;
        skeleton = Target.transform;
        int ghostLayer = LayerMask.NameToLayer("ghostEffect");
        if (isSupportRim)
        {
            Renderer _originalRender = skeleton.GetComponent<Renderer>();
            Material _xayRimMaterial = GameObject.Instantiate(xayRimMaterial) as Material;
            materialG[0] = _originalRender.sharedMaterial;
            materialG[1] = _xayRimMaterial;
        }
        skeleton.gameObject.layer = ghostLayer;
        GhostShootCam.cullingMask = 1 << ghostLayer;
        curTargetTex = RenderTexture.GetTemporary(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        GhostShootCam.targetTexture = curTargetTex;
    }

    void CreateXayRimTexture()
    {
        if (!isSupportRim)
            return;
        if (skeleton)
        {
            Renderer rd = skeleton.GetComponent<Renderer>();
            rd.sharedMaterial = materialG[1];
            GhostShootCam.Render();
            rd.sharedMaterial = materialG[0];
        }
    }

    IEnumerator ShowGhoastEffect()
    {
        Debug.Log("**************** ShowGhostEffect");
        this.enabled = true;
        GhostShootCam.gameObject.SetActive(true);
        GetTargetTextrue();
        int i = 0;
        while (true)
        {
            CreateXayRimTexture();
            int a = i % 2;
            int b = ++i % 2;
            curTex = textureG[a];
            Graphics.SetRenderTarget(curTex);

            GL.Clear(true, true, Color.clear);

            ghoastMaterial.SetTexture("_Tex1", textureG[b]);
            ghoastMaterial.SetTexture("_MainTex", curTargetTex);
            ghoastMaterial.SetFloat("_fadeout", fadeout * 2);
            GL.PushMatrix();
            GL.LoadOrtho();
            ghoastMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0.0f, 0); GL.Vertex3(0.0f, 0.0f, 0.1f);
            GL.TexCoord2(1.0f, 0); GL.Vertex3(1.0f, 0.0f, 0.1f);
            GL.TexCoord2(1.0f, 1); GL.Vertex3(1.0f, 1.0f, 0.1f);
            GL.TexCoord2(0.0f, 1); GL.Vertex3(0.0f, 1.0f, 0.1f);
            GL.End();
            GL.PopMatrix();
            if (Type == IntervalType.Time)
                yield return new WaitForSeconds(intervalTime);
            else
            {
                yield return new YieldCondition(() => {
                    return Vector2.Distance(
                        renderCamera.WorldToScreenPoint(Target.transform.position), 
                        lastShootPos) > intervalDis;
                    });
                lastShootPos = renderCamera.WorldToScreenPoint(
                    Target.transform.position);
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        sourceTex = source;
        if (!isSupport)
        {
            Graphics.Blit(source, dest);
            return;
        }
        ghoastMaterial.SetTexture("_Tex0", sourceTex);
        ghoastMaterial.SetTexture("_Tex1", curTex);
        Graphics.Blit(null, null as RenderTexture, ghoastMaterial, 1);
    }

     /*
    public void GhostPlay(float time)
    {
        if (isSupport)
        {
            if (showCor == null)
            {
                showCor = EngineFramework.Instance.StartCoroutine(ShowGhoastEffect(time));
            }
        }

        if (stopCor != null)
            EngineFramework.Instance.StopCoroutine(stopCor);
    }
    */

        /*
    public void GhostStop(float time)
    {
        stopCor = EngineFramework.Instance.StartCoroutine(Stop(time));
    }
    */

       /*
    IEnumerator Stop(float time)
    {
        yield return new WaitForSeconds(time);
        if (showCor != null)
        {
            //Debug.Log("**************** StopGhostEffect");
            EngineFramework.Instance.StopCoroutine(showCor);
            showCor = null;
        }

        if (skeleton != null)
            skeleton.gameObject.layer = LayerMask.NameToLayer("Player");
        curTex = null;
        RenderTexture.ReleaseTemporary(curTargetTex);
        Graphics.SetRenderTarget(textureG[0]);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(textureG[1]);
        GL.Clear(true, true, Color.clear);

        this.enabled = false;
        GhostShootCam.gameObject.SetActive(false);
    }
    */
}
