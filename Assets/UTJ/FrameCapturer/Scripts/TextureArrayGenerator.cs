using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TextureArrayGenerator
{
    public static string outputPath => "Assets/SpriteOutputs";

    public static Texture2DArray Create(string namePrefix, string path)
    {
        Debug.Log($"Creating texture array at path {path}");
        DirectoryInfo di = new DirectoryInfo(path);
        List<string> filepaths = new List<string>();
        List<Texture2D> textures = new List<Texture2D>();
        di.Create();
        bool linear = namePrefix.Contains("FrameBuffer");
        foreach (var file in di.EnumerateFiles())
        {
            if (file.Name.StartsWith(namePrefix))
            {
                filepaths.Add(file.FullName);
                
                Texture2D newTex = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
                newTex.LoadImage(File.ReadAllBytes(file.FullName));
                textures.Add(newTex);
            }
        }

        var array= Create(textures, outputPath, namePrefix, linear);

        textures.ForEach(Object.Destroy);

        return array;
    }

    private static Texture2DArray Create(List<Texture2D> textures, string path, string animationName, bool linear)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (!Directory.Exists(di.FullName))
        {
            di.Create();
            // Directory.CreateDirectory(path);
        }


        Debug.Log($"Creating texture2d array for animation {animationName} at path {path} with {textures.Count} textures.");
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
            return null;
        }

        //string path = "Assets/Texture2d/test.asset";
        // Create Texture2DArray
        Texture2DArray texture2DArray = new
            Texture2DArray(textures[0].width,
                textures[0].height, textures.Count,
                TextureFormat.RGBA32, false, linear);
        // Apply settings
        texture2DArray.filterMode = FilterMode.Point;
        texture2DArray.wrapMode = TextureWrapMode.Repeat;
        Debug.Log($"Texture array dimensions {texture2DArray.width}x{texture2DArray.height}");

        // Loop through ordinary textures and copy pixels to the
        // Texture2DArray
        for (int i = 0; i < textures.Count; i++)
        {
            texture2DArray.SetPixels(textures[i].GetPixels(0),
                i, 0);
        }

        texture2DArray.wrapMode = TextureWrapMode.Clamp;
        texture2DArray.filterMode = FilterMode.Point;
        // Apply our changes
        texture2DArray.Apply();
        if (path.Length != 0)
        {
            AssetDatabase.CreateAsset(texture2DArray, Path.Combine(path, $"{animationName}_Array.Asset"));
        }

        return texture2DArray;
    }
}