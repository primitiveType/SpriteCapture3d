using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UTJ.FrameCapturer
{
    public class SpriteSheetEncoder : MovieEncoder
    {
        fcAPI.fcPngContext m_ctx;
        fcAPI.fcSpriteSheetConfig m_config;
        string m_outPath;
        int m_currentframe;


        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.Png; } }
        // public int Rows { get; set; }
        // public int Columns { get; set; }
        private byte[][] sheet;

        private string CaptureType { get; set; }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcPngIsSupported())
            {
                Debug.LogError("Png encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcSpriteSheetConfig)config;
            var pngConfig = m_config.pngConfig;
            m_ctx = fcAPI.fcPngCreateContext(ref pngConfig);
            int pathIndex = outPath.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
            CaptureType = outPath.Substring( pathIndex);
            m_outPath = Path.Combine(outPath.Substring(0, pathIndex), m_config.modelName + "/");
            Directory.CreateDirectory(m_outPath);
            m_currentframe = 0;
            sheet = new byte[m_config.numFramesInAnimation][];

            //GetAnimationInfo();
        }

        void GetAnimationInfo()
        {
            // AnimationClip clip = m_config.animator.GetCurrentAnimatorClipInfo(0)[0].clip;
            // float numFramesF = clip.frameRate * clip.length;
            // NumFramesInAnimation = Mathf.RoundToInt(numFramesF);
            // Debug.Log($"{NumFramesInAnimation} frames calculated.");
            // Rows = Mathf.CeilToInt(Mathf.Sqrt(NumFramesInAnimation));
            // Columns = Rows;
        }

        private int currentSheet;

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp = -1.0)
        {
            int bytesPerPixel = frame.Length / (m_config.frameSize * m_config.frameSize);
            if (m_ctx)
            {
                 sheet[m_currentframe % m_config.numFramesInAnimation] = frame;
                 
                 //int channels = System.Math.Min(m_config.pngConfig.channels, (int)format & 7);
                // fcAPI.fcPngExportPixels(m_ctx, path, frame , m_config.frameSize , m_config.frameSize , format, channels);

            }
            ++m_currentframe;
            
            if (m_currentframe % m_config.numFramesInAnimation == 0)
            {
                Debug.Log($"Apparent {bytesPerPixel} bytes per pixel");                 
                string path = $"{m_outPath}{m_config.modelName}_{m_config.animationName}_{CaptureType}_{currentSheet.ToString("0000")}.png";

                int channels = System.Math.Min(m_config.pngConfig.channels, (int)format & 7);
                Debug.Log($"Saving sheet {currentSheet} with {m_config.numFramesInAnimation} anim frames. FrameSize is {m_config.frameSize}. Channels : {channels}. Format {format}");
                var allBytes = Combine(sheet);
                fcAPI.fcPngExportPixels(m_ctx, path, allBytes , m_config.frameSize, m_config.frameSize * m_config.numFramesInAnimation, format, channels);
                currentSheet++;
                sheet = new byte[m_config.numFramesInAnimation][];
            }
        }
        
        private static byte[] Combine(byte[][] arrays)
        {
            byte[] bytes = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            Debug.Log($"Combining {arrays.Length} arrays. Total length {bytes.Length}");
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, bytes, offset, array.Length);
                offset += array.Length;
            }

            return bytes;
        }
     
        
        public override void AddAudioSamples(float[] samples)
        {
            // not supported
        }

    }
}