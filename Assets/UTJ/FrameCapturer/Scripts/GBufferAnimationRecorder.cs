﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.Serialization;


public static class AnimationGenerator
{
    public static string CreationPath = "./Capture/";
    private static string TemplateFileName => "Assets/Animation_template.txt";
    private static string TemplateText { get; set; }

    public static void CreateAnimation(string path, string modelName, string animationName, int numFrames, float duration)
    {
        if (TemplateText == null)
        {
            TemplateText = File.ReadAllText(TemplateFileName);
        }

        string fullAnimationName = $"{modelName}_{animationName}";

        string animationFile = TemplateText.Replace("$ANIMATION_NAME", fullAnimationName);
        animationFile = animationFile.Replace("$NUM_FRAMES_PLUS_ONE", (numFrames + 1).ToString());
        animationFile = animationFile.Replace("$ANIMATION_DURATION", (duration).ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, animationName + ".asset"), animationFile);
    }

    public static string GetFileName(string model, string animation, int perspective, string extension)
    {
        if (!extension.StartsWith("."))
        {
            extension = $".{extension}";
        }

        return Path.Combine(GetDirectory(model), $"{model}_{animation}_{perspective:0000}{extension}");
    }

    public static string GetDirectory(string model)
    {
        return Path.Combine(CreationPath, model);
    }
}

public static class TextureArrayGenerator
{
    public static string outputPath => "Assets/SpriteOutputs";
    public static void Create(string namePrefix, string path)
    {
        Debug.Log($"Creating texture array at path {path}");
        DirectoryInfo di = new DirectoryInfo(path);
        List<string> filepaths = new List<string>();
        List<Texture2D> textures = new List<Texture2D>();
        foreach (var file in di.EnumerateFiles())
        {
            if (file.Name.StartsWith(namePrefix))
            {
                filepaths.Add(file.FullName);
                Texture2D newTex = new Texture2D(2,2);
                newTex.LoadImage(File.ReadAllBytes(file.FullName));
                textures.Add(newTex);
            }
        }
        
        Create(textures, outputPath, namePrefix);
    }
    public static void Create(List<Texture2D> textures, string path, string animationName)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (!Directory.Exists(di.FullName))
        {
            di.Create();
            // Directory.CreateDirectory(path);
        }
        

        Debug.Log("Creating texture2d array");
        // List<Texture2D> textures = new List<Texture2D>();
        // foreach (Object o in Selection.objects)
        // {
        //     if (o.GetType() == typeof(Texture2D))
        //     {
        //         textures.Add((Texture2D) o);
        //     }
        // }

        if (textures.Count == 0)
        {
            Debug.Log("No textures in selection.");
            return;
        }

        //string path = "Assets/Texture2d/test.asset";
        // Create Texture2DArray
        Texture2DArray texture2DArray = new
            Texture2DArray(textures[0].width,
                textures[0].height, textures.Count,
                TextureFormat.RGBA32, true, false);
        // Apply settings
        texture2DArray.filterMode = FilterMode.Bilinear;
        texture2DArray.wrapMode = TextureWrapMode.Repeat;
        // Loop through ordinary textures and copy pixels to the
        // Texture2DArray
        for (int i = 0; i < textures.Count; i++)
        {
            texture2DArray.SetPixels(textures[i].GetPixels(0),
                i, 0);
        }

        // Apply our changes
        texture2DArray.Apply();
        if (path.Length != 0)
        {
            AssetDatabase.CreateAsset(texture2DArray, Path.Combine(path, $"{animationName}_Array.Asset"));
        }
    }
}

namespace UTJ.FrameCapturer
{
    public class GBufferAnimationRecorder : RecorderBase
    {
        [SerializeField] public AnimationMaterialDictionary animationDictionary;

        private int NumFramesInAnimation = 8;
        public int PixelsPerMeter = 200;
        public GameObject TurnTable;
        public Animator Animator;

        #region fields

        [SerializeField]
        private MovieEncoderConfigs m_encoderConfigs = new MovieEncoderConfigs(MovieEncoder.Type.SpriteSheet);

        [SerializeField] private FrameBufferConponents m_fbComponents = FrameBufferConponents.defaultValue;

        [SerializeField] private Shader m_shCopy;
        private Material m_matCopy;
        private Mesh m_quad;
        private CommandBuffer m_cbCopyFB;
        private CommandBuffer m_cbCopyGB;
        private CommandBuffer m_cbClearGB;
        private CommandBuffer m_cbCopyVelocity;
        private RenderTexture[] m_rtFB; //use as temp render textures?
        private RenderTexture[] m_rtGB;
        private List<BufferRecorder> m_recorders = new List<BufferRecorder>();
        protected bool m_capture = false;

        [SerializeField] private int NumRotationsToCapture = 8;

        [SerializeField] private Camera Camera;

        #endregion

        #region properties

        public FrameBufferConponents fbComponents
        {
            get { return m_fbComponents; }
            set { m_fbComponents = value; }
        }

        public MovieEncoderConfigs encoderConfigs
        {
            get { return m_encoderConfigs; }
        }

        #endregion

        protected override void Start()
        {
            TextureArrayGenerator.Create("CHIMERA_LEGACY_walk_RM_Alpha", "E:/Projects/SpriteCapture - Copy/Capture/CHIMERA_LEGACY");
            
             base.Start();
            TurnTable.transform.localScale = Vector3.one;
            StartCoroutine(InternalExportCoroutine());
        }

        private int FrameSize { get; set; }

        private void SetupCameraResolution()
        {
            FrameSize = (int) (PixelsPerMeter * Camera.orthographicSize * 2);

            Screen.SetResolution(FrameSize, FrameSize, FullScreenMode.Windowed);
            Camera.farClipPlane = 100000;
            Camera.allowDynamicResolution = false;
            Camera.targetTexture = new RenderTexture(FrameSize, FrameSize, 32);
            Camera.targetTexture.filterMode = FilterMode.Point;
        }

        private int SetupEncoderConfig()
        {
            var config = m_encoderConfigs.spritesheetEncoderSettings;
            config.frameSize = FrameSize;
            config.numFramesInAnimation = NumFramesInAnimation;
            config.animationName = currentClipName;
            config.modelName = currentModelName;
            m_encoderConfigs.spritesheetEncoderSettings = config;
            return FrameSize;
        }

        protected override void Update()
        {
            m_captureControl = CaptureControl.Manual;
            // base.Update();
        }

        private string currentClipName { get; set; }
        private string currentModelName { get; set; }

        private IEnumerator InternalExportCoroutine()
        {
            foreach (Transform child in TurnTable.transform)
            {
                child.transform.position = Vector3.zero;
                child.transform.localScale = Vector3.one;
                child.transform.rotation = Quaternion.identity;
                child.gameObject.SetActive(false);
            }

            foreach (Transform child in TurnTable.transform)
            {
                child.gameObject.SetActive(true);
                Animator = child.GetComponentInChildren<Animator>();
                if (Animator == null)
                {
                    child.gameObject.SetActive(false);
                    continue; //capture without animation?
                }

                currentModelName = Animator.gameObject.name;
                int clipCount = Animator.runtimeAnimatorController.animationClips.Length;
                Debug.Log($"Clip Count : {clipCount}");
                for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                {
                    Debug.Log($"Capturing animation {clipIndex}");
                    float frameDelay = 0.0f;
                    AnimationClip clip = Animator.runtimeAnimatorController.animationClips[clipIndex];

                    float numFramesF = clip.frameRate * clip.length;
                    NumFramesInAnimation = Mathf.RoundToInt(numFramesF);
                    currentClipName = clip.name;
                    AnimationGenerator.CreateAnimation($"Assets/Animations/{currentModelName}",
                        currentModelName, currentClipName, NumFramesInAnimation, clip.length);
                    animationDictionary.AddPropertyBlock(currentModelName, currentClipName ,NumFramesInAnimation, 1);

                    // var block = AnimationMaterialPropertyBlock.CreateInstance();
                    // var block = ScriptableObject.CreateInstance<AnimationMaterialPropertyBlock>();
                    // block.AnimationName = "test;";
                    // block.name = "tester";
                    yield return CalculateBounds(clipIndex);
                    SetCameraSizeToBounds();
                    SetupCameraResolution();
                    SetupEncoderConfig();
                    BeginRecording();
                    int captured = 0;
                    for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
                    {
                        foreach (var frame in EnumerateClipFrames(clipIndex))
                        {
                            yield return new WaitForEndOfFrame();
                            captured++;
                            m_capture = true;
                            var captured1 = captured;
                            // Debug.Log("Waiting for capture");
                            yield return new WaitUntil(() => !m_capture && captured1 == m_recordedFrames);
                        }

                        TurnTable.transform.Rotate(new Vector3(0, 360f / NumRotationsToCapture, 0));
                    }

                    Debug.Log($"Finished with animation {currentClipName}");
                    EndRecording();
                }

                Debug.Log($"Finished with model {currentModelName}");
                child.gameObject.SetActive(false);
            }

            Debug.Log("All done");
        }

        private IEnumerable EnumerateClipFrames(int clipIndex)
        {
            float frameDelay = 0.0f;
            AnimationClip clip = Animator.runtimeAnimatorController.animationClips[clipIndex];
            frameDelay = clip.length / (float) (NumFramesInAnimation);

            for (int capture = 0; capture < NumFramesInAnimation; capture++)
            {
                Animator.speed = 0.0f;
                Animator.Play(currentClipName, -1, frameDelay * capture);
                yield return null;
            }
        }

        private Bounds FourDBounds { get; set; }

        private IEnumerator CalculateBounds(int clipIndex)
        {
            AnimationClip clip = null;
            float frameDelay = 0f;
            float elapsedFramesInTime = 0f;
            var tester = TurnTable.GetComponent<BoxCollider>();
            if (!tester)
            {
                tester = TurnTable.AddComponent<BoxCollider>();
            }

            tester.size = Vector3.zero;
            List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            foreach (var child in TurnTable.GetComponentsInChildren<MeshRenderer>())
            {
                if (!child.gameObject.GetComponent<MeshCollider>())
                {
                    child.gameObject.AddComponent<MeshCollider>();
                }
            }

            foreach (var child in TurnTable.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderers.Add(child);
                child.updateWhenOffscreen = true; //for some reason, causes the bounding box to update every frame.
                Debug.Log("Found a skm");
            }

            Bounds bounds = new Bounds();
            bounds.Encapsulate(TurnTable.GetComponent<Collider>().bounds);

            try
            {
                clip = Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
                frameDelay = clip.length / (float) (NumFramesInAnimation);
            }
            catch
            {
                Debug.LogError("Unable to get Animator clip. Maybe you should set the Animator to null?");
                yield break;
            }

            for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
            {
                foreach (var frame in EnumerateClipFrames(clipIndex))
                {
                    foreach (var skm in skinnedMeshRenderers)
                    {
                        yield return new WaitForEndOfFrame();
                        //docs say this should include the bounds of every frame of animation, but appears to not be true.
                        var skmBounds = skm.bounds;
                        bounds.Encapsulate(skmBounds);
                    }

                    tester.center = bounds.center;
                    tester.size = bounds.size;
                    elapsedFramesInTime += frameDelay;
                }

                TurnTable.transform.Rotate(new Vector3(0, 360f / NumRotationsToCapture, 0));
            }


            FourDBounds = bounds;
            Debug.Log($"Bounds size: {FourDBounds.size.ToString()}");
        }

        private void SetCameraSizeToBounds()
        {
            var maxSizeX = FourDBounds.extents.x;
            var maxSizeY = FourDBounds.extents.y;
            var maxSizeZ = FourDBounds.extents.z;
            var maxSize = Mathf.Max(Mathf.Max(maxSizeX, maxSizeY), maxSizeZ);
            Camera.orthographic = true;
            Camera.orthographicSize = maxSize;
            Camera.transform.position = new Vector3(FourDBounds.center.x, FourDBounds.center.y, 1000);
            Camera.transform.LookAt(FourDBounds.center);
            var orthoMatrix = Camera.projectionMatrix;
            Camera.orthographic = false; //has to be false for normal maps.
            Camera.projectionMatrix = orthoMatrix; //trick it to use orthographic
        }


        public override bool BeginRecording()
        {
//if (m_recording) { return false; }
            if (m_shCopy == null)
            {
                Debug.LogError("GBufferRecorder: copy shader is missing!");
                return false;
            }

            m_outputDir.CreateDirectory();
            if (m_quad == null) m_quad = fcAPI.CreateFullscreenQuad();
            if (m_matCopy == null) m_matCopy = new Material(m_shCopy);

            var cam = GetComponent<Camera>();
            if (cam.targetTexture != null)
            {
                m_matCopy.EnableKeyword("OFFSCREEN");
            }

            else
            {
                m_matCopy.DisableKeyword("OFFSCREEN");
            }

            int captureWidth = cam.pixelWidth;
            int captureHeight = cam.pixelHeight;

            GetCaptureResolution(ref captureWidth, ref captureHeight);
            if (m_encoderConfigs.format == MovieEncoder.Type.MP4 ||
                m_encoderConfigs.format == MovieEncoder.Type.WebM)
            {
                captureWidth = (captureWidth + 1) & ~1;
                captureHeight = (captureHeight + 1) & ~1;
            }

            if (m_fbComponents.frameBuffer)
            {
                m_rtFB = new RenderTexture[2];
                for (int i = 0; i < m_rtFB.Length; ++i)
                {
                    m_rtFB[i] = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGBHalf);
                    m_rtFB[i].filterMode = FilterMode.Point;
                    m_rtFB[i].Create();
                }

                int tid = Shader.PropertyToID("_TmpFrameBuffer");
                m_cbCopyFB = new CommandBuffer();
                m_cbCopyFB.name = "GBufferRecorder: Copy FrameBuffer";
                m_cbCopyFB.GetTemporaryRT(tid, -1, -1, 0, FilterMode.Point);
                m_cbCopyFB.Blit(BuiltinRenderTextureType.CurrentActive, tid);
                m_cbCopyFB.SetRenderTarget(new RenderTargetIdentifier[] {m_rtFB[0], m_rtFB[1]}, m_rtFB[0]);
                m_cbCopyFB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 0);
                m_cbCopyFB.ReleaseTemporaryRT(tid);
                cam.AddCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);
            }

            if (m_fbComponents.GBuffer)
            {
                m_rtGB = new RenderTexture[8];
                for (int i = 0; i < m_rtGB.Length; ++i)
                {
                    m_rtGB[i] = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGBHalf);
                    m_rtGB[i].filterMode = FilterMode.Point;
                    m_rtGB[i].Create();
                }

                // clear gbuffer (Unity doesn't clear emission buffer - it is not needed usually)
                m_cbClearGB = new CommandBuffer();
                m_cbClearGB.name = "GBufferRecorder: Cleanup GBuffer";
                if (cam.allowHDR)
                {
                    m_cbClearGB.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                }
                else
                {
                    m_cbClearGB.SetRenderTarget(BuiltinRenderTextureType.GBuffer3);
                }

                m_cbClearGB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 3);
                m_matCopy.SetColor("_ClearColor", cam.backgroundColor);

                // copy gbuffer
                m_cbCopyGB = new CommandBuffer();
                m_cbCopyGB.name = "GBufferRecorder: Copy GBuffer";
                m_cbCopyGB.SetRenderTarget(new RenderTargetIdentifier[]
                {
                    m_rtGB[0], m_rtGB[1], m_rtGB[2], m_rtGB[3], m_rtGB[4], m_rtGB[5], m_rtGB[6]
                }, m_rtGB[0]);
                m_cbCopyGB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 2);
                cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, m_cbClearGB);
                cam.AddCommandBuffer(CameraEvent.BeforeLighting, m_cbCopyGB);

                if (m_fbComponents.gbVelocity)
                {
                    m_cbCopyVelocity = new CommandBuffer();
                    m_cbCopyVelocity.name = "GBufferRecorder: Copy Velocity";
                    m_cbCopyVelocity.SetRenderTarget(m_rtGB[7]);
                    m_cbCopyVelocity.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 4);
                    cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_cbCopyVelocity);
                    cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                }
            }

            int framerate = m_targetFramerate;
            if (m_fbComponents.frameBuffer)
            {
                if (m_fbComponents.fbColor) m_recorders.Add(new BufferRecorder(m_rtFB[0], 4, "FrameBuffer", framerate));
                if (m_fbComponents.fbAlpha) m_recorders.Add(new BufferRecorder(m_rtFB[1], 1, "Alpha", framerate));
            }

            if (m_fbComponents.GBuffer)
            {
                if (m_fbComponents.gbAlbedo)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[0], 3, "Albedo", framerate));
                }

                if (m_fbComponents.gbOcclusion)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[1], 1, "Occlusion", framerate));
                }

                if (m_fbComponents.gbSpecular)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[2], 3, "Specular", framerate));
                }

                if (m_fbComponents.gbSmoothness)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[3], 1, "Smoothness", framerate));
                }

                if (m_fbComponents.gbNormal)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[4], 3, "Normal", framerate));
                }

                if (m_fbComponents.gbEmission)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[5], 3, "Emission", framerate));
                }

                if (m_fbComponents.gbDepth)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[6], 1, "Depth", framerate));
                }

                if (m_fbComponents.gbVelocity)
                {
                    m_recorders.Add(new BufferRecorder(m_rtGB[7], 2, "Velocity", framerate));
                }
            }

            foreach (var rec in m_recorders)
            {
                if (!rec.Initialize(m_encoderConfigs, m_outputDir))
                {
                    EndRecording();
                    return false;
                }
            }

            base.BeginRecording();
            Debug.Log("GBufferRecorder: BeginRecording()");
            return true;
        }

        public override void EndRecording()
        {
            foreach (var rec in m_recorders)
            {
                rec.Release();
            }

            m_recorders.Clear();

            var cam = GetComponent<Camera>();
            if (m_cbCopyFB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);
                m_cbCopyFB.Release();
                m_cbCopyFB = null;
            }

            if (m_cbClearGB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, m_cbClearGB);
                m_cbClearGB.Release();
                m_cbClearGB = null;
            }

            if (m_cbCopyGB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeLighting, m_cbCopyGB);
                m_cbCopyGB.Release();
                m_cbCopyGB = null;
            }

            if (m_cbCopyVelocity != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_cbCopyVelocity);
                m_cbCopyVelocity.Release();
                m_cbCopyVelocity = null;
            }

            if (m_rtFB != null)
            {
                foreach (var rt in m_rtFB)
                {
                    rt.Release();
                }

                m_rtFB = null;
            }

            if (m_rtGB != null)
            {
                foreach (var rt in m_rtGB)
                {
                    rt.Release();
                }

                m_rtGB = null;
            }

            if (m_recording)
            {
                Debug.Log("GBufferRecorder: EndRecording()");
            }

            base.EndRecording();
        }


        #region impl

#if UNITY_EDITOR
        private void Reset()
        {
            m_shCopy = fcAPI.GetFrameBufferCopyShader();
        }
#endif // UNITY_EDITOR

        private IEnumerator OnPostRender()
        {
            if (m_recording && m_capture)
            {
                yield return new WaitForEndOfFrame();
                //Camera.clearFlags = CameraClearFlags.Depth;

                //double timestamp = Time.unscaledTime - m_initialTime;
                double timestamp = 0;
                if (m_recordedFrames > 0)
                {
                    timestamp = 1.0 / m_targetFramerate * m_recordedFrames;
                }

                foreach (var rec in m_recorders)
                {
                    rec.Update(timestamp);
                }

                ++m_recordedFrames;
                m_capture = false;
            }

            m_frame++;
        }

        #endregion
    }
}