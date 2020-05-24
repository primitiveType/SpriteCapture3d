using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace UTJ.FrameCapturer
{
    public class GBufferAnimationRecorder : RecorderBase
    {
        private int NumFramesInAnimation = 8;
        public int PixelsPerMeter=200;
        public GameObject TurnTable;
        public Animator Animator;

        #region fields

        [SerializeField] MovieEncoderConfigs m_encoderConfigs = new MovieEncoderConfigs(MovieEncoder.Type.SpriteSheet);
        [SerializeField] FrameBufferConponents m_fbComponents = FrameBufferConponents.defaultValue;

        [SerializeField] Shader m_shCopy;
        Material m_matCopy;
        Mesh m_quad;
        CommandBuffer m_cbCopyFB;
        CommandBuffer m_cbCopyGB;
        CommandBuffer m_cbClearGB;
        CommandBuffer m_cbCopyVelocity;
        RenderTexture[] m_rtFB; //use as temp render textures?
        RenderTexture[] m_rtGB;
        List<BufferRecorder> m_recorders = new List<BufferRecorder>();
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
            base.Start();
            // float x = (100f - 100f / (Screen.width / frameSize)) / 100f;
            // float y = (100f - 100f / (Screen.height / frameSize)) / 100f;
            //
            // Camera.rect = new Rect(x, y, 1, 1);
            Debug.Log($"Camera is {Camera.pixelWidth}x{Camera.pixelHeight}");
            AnimationClip clip = Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
            float numFramesF = clip.frameRate * clip.length;
//            Debug.Log($"{numFramesF} frames calculated.");
            NumFramesInAnimation = Mathf.RoundToInt(numFramesF);
            // Rows = Mathf.RoundToInt(Mathf.Sqrt(NumFramesInAnimation));
            // Columns = Rows;
            //     Debug.Log($"using dimensions {Rows} x {Columns} .");
            StartCoroutine(InternalExportCoroutine());
        }

        private void SetupCameraResolution()
        {
            var frameSize = (int) (PixelsPerMeter * Camera.orthographicSize * 2);
            var config = m_encoderConfigs.spritesheetEncoderSettings;
            config.frameSize = frameSize;

            m_encoderConfigs.spritesheetEncoderSettings = config;
            
            Screen.SetResolution(frameSize, frameSize, FullScreenMode.Windowed);
            Camera.farClipPlane = 100000;
            Camera.allowDynamicResolution = false;
            Camera.targetTexture = new RenderTexture(frameSize, frameSize, 32);
            Camera.targetTexture.filterMode = FilterMode.Point;
        }

        protected override void Update()
        {
            m_captureControl = CaptureControl.Manual;
            // base.Update();
        }

        private IEnumerator InternalExportCoroutine()
        {
            float frameDelay = 0.0f;
            AnimationClip clip = null;
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

            yield return CalculateBounds();
            SetCameraSizeToBounds();
            SetupCameraResolution();
            BeginRecording();
            float elapsed = 0.0f;
            int captured = 0;
            for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
            {
                elapsed = 0;
                for (int capture = 0; capture < NumFramesInAnimation; capture++)
                {
                    captured++;
                    if (Animator == null)
                    {
                        //Camera.clearFlags = CameraClearFlags.SolidColor;
                    }
                    else
                    {
                        if (Animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
                        {
                            Animator.Play(clip.name, -1, elapsed);
                            Animator.speed = 0.0f;
                        }
                        else
                        {
                            Debug.LogError("The animator state must be the same name as the clip.");
                        }

                        yield return new WaitForEndOfFrame();
                    }

                    elapsed += frameDelay;
                    m_capture = true;
                    var captured1 = captured;
                    yield return new WaitUntil(() => !m_capture && captured1 == m_recordedFrames);
                }

                Debug.Log($"Finished sheet {perspective} . Captured {captured} so far.");
                TurnTable.transform.Rotate(new Vector3(0, 360f / NumRotationsToCapture, 0));
            }

        }

        private Bounds FourDBounds { get; set; }

        private IEnumerator CalculateBounds()
        {
            AnimationClip clip = null;
            float frameDelay = 0f;
            float elapsedFramesInTime = 0f;
            var tester = TurnTable.AddComponent<BoxCollider>();
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
                for (int capture = 0; capture < NumFramesInAnimation; capture++)
                {
                    if (Animator == null)
                    {
                        Camera.clearFlags = CameraClearFlags.SolidColor;
                    }
                    else
                    {
                        if (Animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
                        {
                            Animator.Play(clip.name, -1, elapsedFramesInTime);
                            Animator.speed = 0.0f;
                        }
                        else
                        {
                            Debug.LogError("The animator state must be the same name as the clip.");
                        }

                       // bounds.Encapsulate(TurnTable.GetComponent<Collider>().bounds);

                        foreach (var skm in skinnedMeshRenderers)
                        {
                            var skmBounds =
                                skm.bounds; //docs say this should include the bounds of every frame of animation, but appears to not be true.
                            Debug.Log($"{skmBounds.center} : {skmBounds.size}");
                            bounds.Encapsulate(skmBounds);
                            Debug.Log($"{bounds.center} : {bounds.size}");
                        }

                        tester.center = bounds.center;
                        tester.size = bounds.size;
                        elapsedFramesInTime += frameDelay;
                        yield return new WaitForSeconds(0.01f);
                    }
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
            Camera.orthographicSize = maxSize;
            Camera.transform.position = new Vector3(FourDBounds.center.x, FourDBounds.center.y, 1000);
            Camera.transform.LookAt(FourDBounds.center);
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
        void Reset()
        {
            m_shCopy = fcAPI.GetFrameBufferCopyShader();
        }
#endif // UNITY_EDITOR

        IEnumerator OnPostRender()
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