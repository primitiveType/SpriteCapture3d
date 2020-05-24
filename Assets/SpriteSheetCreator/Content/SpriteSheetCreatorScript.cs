/*
Sprite Sheet Creator
(c) 2016 Digital Ruby, LLC
Created by Jeff Johnson
http://www.digitalruby.com
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// Used to generate Texture Array asset
// Menu button is available in GameObject > Create Texture Array
// See CHANGEME in the file
using UnityEngine;
using UTJ.FrameCapturer;

// After this, you will have a Texture Array asset which you can assign to the shader's Tex attribute!
namespace DigitalRuby.SpriteSheetCreator
{
#if UNITY_EDITOR

    [ExecuteInEditMode]
    public class SpriteSheetCreatorScript : MonoBehaviour
    {
        [Header("Sprite Sheet Properties")] [Tooltip("Info. Auto-generated from properties.")]
        public string Info;

        [Tooltip("The width of each frame in the spritesheet in pixels")]
        public int FrameWidth = 64;

        public bool UseInfoFromAnimationClip;

        public int NumRotationsToCapture = 8;

        [Tooltip("The height of each frame the spritesheet in pixels")]
        public int FrameHeight = 64;

        [Range(2, 64)] [Tooltip("The number of rows in the spritesheet")]
        public int Rows = 5;

        [Range(2, 64)] [Tooltip("The number of columns in the spritesheet")]
        public int Columns = 5;

        [Tooltip(
            "Background color for the sprite sheet. Use transparent unless your sprite will use additive blend mode, in which case use black. Other colors are available for edge cases.")]
        public SpriteSheetBackgroundColor BackgroundColor = SpriteSheetBackgroundColor.Transparent;

        private readonly Color[] backgroundColors = new Color[]
            {Color.clear, Color.black, Color.white, new Color(1.0f, 0.0f, 1.0f, 1.0f), Color.cyan};

        [Header("Animation Properties")]
        [Tooltip(
            "The animation to use. If null, AnimationDuration is used and frames are spaced evenly using AnimationDuration / (Rows * Columns).")]
        public Animator Animator;

        [Tooltip(
            "If Animation is null, this is the total seconds to capture animation, with frames being evenly spaced using AnimationDuration / (Rows * Columns).")]
        public float AnimationDuration = 1.0f;

        [Header("Export")] [Tooltip("Root object for content that will be exported")]
        public GameObject ExportRoot;

        [Tooltip(
            "Default is SpriteSheet.png. The full path and file name to save the saved sprite sheet to. Leave this blank unless you have a specific use case for a different path.")]
        public string SaveFileName;

        [Tooltip("The label to notify that the export is working and then completed")]
        public Text ExportCompleteLabel;

        [Header("Preview")] [Tooltip("Particle system for preview once export is done")]
        public ParticleSystem PreviewParticleSystem;

        [Range(1, 60)] [Tooltip("Preview FPS")]
        public int PreviewFPS = 24;

        [Header("Scene")] [Tooltip("Overlay for aspect ratio")]
        public RectTransform AspectRatioOverlay;

        [Tooltip("Camera to use. Defaults to main camera.")]
        public Camera Camera;

        private bool exporting;
        private RenderTexture[] renderTextures;
        private RenderTexture[] renderTextureNormals;

        private Rect CenterSizeInScreen()
        {
            float widthScale = (float) Screen.width / (float) FrameWidth;
            float heightScale = (float) Screen.height / (float) FrameHeight;
            float ratio = Mathf.Min(widthScale, heightScale);
            float newWidth = FrameWidth * ratio;
            float newHeight = FrameHeight * ratio;
            float x = ((float) Screen.width - newWidth) * 0.5f;
            float y = ((float) Screen.height - newHeight) * 0.5f;

            return new Rect(x, y, newWidth, newHeight);
        }

        private void UpdateCamera()
        {
            if (Camera == null)
            {
                Camera = Camera.main;
                if (Camera == null)
                {
                    Camera = Camera.current;
                }
            }
        }

        private void UpdateInfo()
        {
            Info = "Dimensions: " + Width + "x" + Height;
        }

        private GBufferRecorder normalCapture;
        private void Start()
        {
            GetGBufferRecorder();
            UpdateCamera();
            Camera.depthTextureMode = DepthTextureMode.Depth;
            UpdateInfo();
        }

        private void GetGBufferRecorder()
        {
            normalCapture = GetComponentInChildren<GBufferRecorder>();
            
        }

        private void Update()
        {
            UpdateCamera();
            UpdateInfo();
            Rect rect = CenterSizeInScreen();
            AspectRatioOverlay.sizeDelta = new Vector2(rect.width, rect.height);
            if (!Application.isPlaying)
            {
                UpdatePreviewParticleSystem();
            }
        }

        private void ExportFrame(int row, int column, int index, bool captureDepth)
        {
            if (captureDepth)
            {
                //normalCapture.BeginRecording();
            }
            else
            {
                float x = ((float) column * (float) FrameWidth) / (float) Width;
                float y = ((float) row * (float) FrameHeight) / (float) Height;
                float w = (float) FrameWidth / (float) Width;
                float h = (float) FrameHeight / (float) Height;

                CameraClearFlags clearFlags = Camera.clearFlags;
                Camera.clearFlags = CameraClearFlags.Depth;
                Camera.targetTexture = renderTextures[index];
                Rect existingViewportRect = Camera.rect;
                Camera.rect = new Rect(x, 1.0f - y - h, w, h);

                
                Camera.Render();
                Camera.rect = existingViewportRect;
                Camera.targetTexture = null;
                Camera.clearFlags = clearFlags;
            }

            // Camera.depthTextureMode = DepthTextureMode.None;
        }

        private void UpdatePreviewParticleSystem()
        {
            if (PreviewParticleSystem == null)
            {
                return;
            }

            var m = PreviewParticleSystem.main;
            m.startLifetime = 100.0f * (float) (Rows * Columns) / (float) PreviewFPS;
            var anim = PreviewParticleSystem.textureSheetAnimation;
            anim.numTilesX = Columns;
            anim.numTilesY = Rows;
            anim.cycleCount = 100;
            anim.enabled = true;
        }

        private void FinishExport()
        {
            ordinaryTextures = new Texture2D[NumRotationsToCapture];
            normalTextures = new Texture2D[NumRotationsToCapture];
            var saveFileName = (SaveFileName ?? string.Empty).Trim();

            ExportArray(ordinaryTextures, renderTextures, saveFileName);
            ExportArray(normalTextures, renderTextureNormals, $"{saveFileName}-Normals");
            ExportCompleteLabel.text = "Done.";
        }

        private void ExportArray(Texture [] dest, RenderTexture[] source, string saveFileName)
        {
            for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
            {
                var name = saveFileName;
                RenderTexture.active = source[perspective];
                Texture2D spriteSheetTexture = new Texture2D(Width, Height, TextureFormat.ARGB32, false, false);
                spriteSheetTexture.filterMode = FilterMode.Point;
                spriteSheetTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                RenderTexture.active = null;
                if (string.IsNullOrEmpty(name))
                {
                    string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                    scenePath = System.IO.Path.GetDirectoryName(scenePath);
                    name = System.IO.Path.Combine(scenePath, $"SpriteSheet");
                }

                name = $"{saveFileName}-{perspective}.png";

                dest[perspective] = spriteSheetTexture;
                byte[] textureBytes = spriteSheetTexture.EncodeToPNG();
                Debug.Log($"Saving {name}");
                System.IO.File.WriteAllBytes(name, textureBytes);
                exporting = false;
                dest[perspective] = null;
                UnityEditor.AssetDatabase.Refresh();
                if (PreviewParticleSystem != null)
                {
                    UpdatePreviewParticleSystem();
                    PreviewParticleSystem.gameObject.SetActive(true);
                    PreviewParticleSystem.Stop();
                    PreviewParticleSystem.Play();
                    ExportRoot.SetActive(false);
                }

            }

        }
        public Texture2D[] ordinaryTextures;
        public Texture2D[] normalTextures;
        public GameObject objectToAddTextureTo;
        
        private IEnumerator InternalExportCoroutine()
        {
            float frameDelay = 0.0f;
            AnimationClip clip = null;
            if (Animator == null)
            {
                frameDelay = AnimationDuration / (float) (Rows + Columns);
            }
            else
            {
                try
                {
                    clip = Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
                    frameDelay = 1.0f / (float) (Rows * Columns);
                }
                catch
                {
                    Debug.LogError("Unable to get Animator clip. Maybe you should set the Animator to null?");
                    yield break;
                }
            }

            float elapsed = 0.0f;

            for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int column = 0; column < Columns; column++)
                    {
                        if (Animator == null)
                        {
                            yield return new WaitForSeconds(frameDelay);
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

                            yield return new WaitForSeconds(0.001f);
                        }

                        elapsed += frameDelay;
                        ExportFrame(row, column, perspective, false);
                        ExportFrame(row, column, perspective, true);
                    }
                }
                
                Camera.transform.RotateAround(Animator.transform.position, Vector3.up, 360f / NumRotationsToCapture);
            }

            FinishExport();
        }

        public void ExportTapped()
        {
            if (exporting)
            {
                return;
            }

            ExportRoot.SetActive(true);
            PreviewParticleSystem.Stop();
            PreviewParticleSystem.gameObject.SetActive(false);
            exporting = true;
            ExportCompleteLabel.text = "Processing...";

            if (UseInfoFromAnimationClip)
            {
                AnimationClip clip = Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
                float numFramesF = clip.frameRate * clip.length;
                Debug.Log($"{numFramesF} frames calculated.");
                int numFrames = Mathf.RoundToInt(numFramesF);
                Rows = Mathf.RoundToInt(Mathf.Sqrt(numFrames));
                Columns = Rows;
                Debug.Log($"using dimensions {Rows} x {Columns} .");
            }

            renderTextures = new RenderTexture[NumRotationsToCapture];
            renderTextureNormals = new RenderTexture[NumRotationsToCapture];
            for (int perspective = 0; perspective < NumRotationsToCapture; perspective++)
            {
                renderTextures[perspective] = GetRenderTexture();
                renderTextureNormals[perspective] = GetRenderTexture();
            }

         
            StartCoroutine(InternalExportCoroutine());
        }

        private RenderTexture GetRenderTexture()
        {
            var renderTexture = new RenderTexture(Width, Height, 16, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            renderTexture.useMipMap = false;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.anisoLevel = 0;
            renderTexture.antiAliasing = 1;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, backgroundColors[(int) BackgroundColor], 1.0f);
            RenderTexture.active = null;
            return renderTexture;
        }


        public int Width
        {
            get { return (FrameWidth * Columns); }
        }

        public int Height
        {
            get { return (FrameHeight * Rows); }
        }
    }

#endif
}