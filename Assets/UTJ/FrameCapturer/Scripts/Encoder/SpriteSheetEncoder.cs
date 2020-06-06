using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using UnityEditor;
using UnityEngine;
using UTJ.FrameCapturer;
using ColorSpace = ImageMagick.ColorSpace;

namespace UTJ.FrameCapturer
{
    public class MagickSpriteSheetGenerator
    {
        public static IMagickImage<byte> Compile(byte[][] images, int width, int height, string path)
        {
            MagickReadSettings mr = new MagickReadSettings();


            mr.ColorType = ColorType.TrueColorAlpha;
            mr.Width = width;
            mr.Height = height;

            mr.Format = MagickFormat.Rgba;
            mr.Depth = 16;
            mr.BackgroundColor = new MagickColor("transparent");
            mr.ColorSpace = ColorSpace.RGB;

            int i = 0;
            using (var collection = new MagickImageCollection())
            {
                foreach (var image in images)
                {
                    var magickImage = new MagickImage() {Depth = 16};
                    magickImage.Read(image, mr);
                    // magickImage.ReadPixels(image,
                    //     new PixelReadSettings(width, height, StorageType.Short, PixelMapping.RGBA));
                    // var profile = magickImage.GetColorProfile();
                    // Debug.Log(profile?.Name);
                    magickImage.Alpha(AlphaOption.Opaque);
                    //magickImage.Write(path + (i++).ToString());
                    collection.Add(magickImage);
                }


                var montageSettings = new MontageSettings();
                montageSettings.Geometry = new MagickGeometry();
                montageSettings.Geometry.Width = width;
                montageSettings.Geometry.Height = height;
                montageSettings.BackgroundColor = new MagickColor("transparent");
                // collection.Write(path);
                var montage = collection.Montage(montageSettings);

                montage.ColorSpace = ColorSpace.Log;
                //montage.Format = MagickFormat.Rgb;
                montage.Write(path);

                // collection.Mosaic().Write(path);
                return montage;
            }
        }

        public static IMagickImage<byte> Compile(List<string> paths, string path)
        {
            int i = 0;
            int width = 0;
            int height = 0;
            using (var collection = new MagickImageCollection())
            {
                foreach (var image in paths)
                {
                    var magickImage = new MagickImage(image);
                    collection.Add(magickImage);
                    width = magickImage.Width;
                    height = magickImage.Height;
                }


                var montageSettings = new MontageSettings();
                montageSettings.Geometry = new MagickGeometry();
                montageSettings.Geometry.Width = width;
                montageSettings.Geometry.Height = height;
                montageSettings.BackgroundColor = new MagickColor("transparent");
                // collection.Write(path);
                var montage = collection.Montage(montageSettings);

                montage.ColorSpace = ColorSpace.Log;
                //montage.Format = MagickFormat.Rgb;
                montage.Write(path);

                // collection.Mosaic().Write(path);
                return montage;
            }
        }
    }


    public class SpriteSheetEncoder : MovieEncoder
    {
        fcAPI.fcPngContext m_ctx;
        fcAPI.fcSpriteSheetConfig m_config;
        string m_outPath;
        int m_currentframe;


        public override void Release()
        {
            m_ctx.Release();
        }

        public override bool IsValid()
        {
            return m_ctx;
        }

        public override Type type
        {
            get { return Type.Png; }
        }

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

            m_config = (fcAPI.fcSpriteSheetConfig) config;
            var pngConfig = m_config.pngConfig;
            m_ctx = fcAPI.fcPngCreateContext(ref pngConfig);
            int pathIndex = outPath.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
            CaptureType = outPath.Substring(pathIndex);
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

        private List<string> paths = new List<string>();

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp = -1.0)
        {
            int bytesPerPixel = frame.Length / (m_config.frameSize * m_config.frameSize);
            if (m_ctx)
            {
                sheet[m_currentframe % m_config.numFramesInAnimation] = frame;
                string path = AnimationGenerator.GetFileName(m_config.modelName,
                    $"{m_config.animationName}_{CaptureType}_{m_currentframe % m_config.numFramesInAnimation}",
                    currentSheet, ".png");
                //int channels = System.Math.Min(m_config.pngConfig.channels, (int)format & 7);
                int channels = System.Math.Min(m_config.pngConfig.channels, (int) format & 7);
                fcAPI.fcPngExportPixels(m_ctx, path, frame, m_config.frameSize, m_config.frameSize, format, channels);
                paths.Add(path);
            }

            ++m_currentframe;

            if (m_currentframe % m_config.numFramesInAnimation == 0)
            {
                Debug.Log($"Apparent {bytesPerPixel} bytes per pixel");
                //  string path = $"{m_outPath}{m_config.modelName}_{m_config.animationName}_{CaptureType}_{currentSheet.ToString("0000")}.png";
                string path = AnimationGenerator.GetFileName(m_config.modelName,
                    $"{m_config.animationName}_{CaptureType}", currentSheet, ".png");
                int channels = System.Math.Min(m_config.pngConfig.channels, (int) format & 7);
                Debug.Log(
                    $"Saving sheet {currentSheet} {CaptureType} with {m_config.numFramesInAnimation} anim frames. FrameSize is {m_config.frameSize}. Channels : {channels}. Format : {format}");
                // var allBytes = Combine(sheet);
                var allBytes =
                    MagickSpriteSheetGenerator.Compile(paths, path);
                paths.ForEach(File.Delete);
                paths.Clear();
                // byte[] allByteArray = allBytes.ToByteArray();
                // if (allByteArray.Length != allBytes.Width * allBytes.Height * 8)
                // {
                //     Debug.LogError("All bytes array was wrong length");
                // }
                // allBytes.Write(path);

                // fcAPI.fcPngExportPixels(m_ctx, path, allBytes.ToByteArray(), allBytes.Width,
                //     allBytes.Height, format, channels);

                //allBytes.Dispose();
                currentSheet++;
                sheet = new byte[m_config.numFramesInAnimation][];

                if (m_currentframe + 1 == m_config.numFramesInAnimation * 8)
                {
                    TextureArrayGenerator.Create($"{m_config.modelName}_{m_config.animationName}_{CaptureType}",
                        AnimationGenerator.GetDirectory(m_config.modelName));
                }
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