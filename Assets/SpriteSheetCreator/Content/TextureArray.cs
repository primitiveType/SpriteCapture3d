using UnityEditor;
using UnityEngine;

public class TextureArray : MonoBehaviour {

    [MenuItem("GameObject/Create Texture Array")]
    static void Create()
    {
        // CHANGEME: Filepath must be under "Resources" and named appropriately. Extension is ignored.
        // {0:000} means zero padding of 3 digits, i.e. 001, 002, 003 ... 010, 011, 012, ...
        string filePattern = "Smoke/smoke_{0:000}"; 
        
        // CHANGEME: Number of textures you want to add in the array
        int slices = 24;
        
        // CHANGEME: TextureFormat.RGB24 is good for PNG files with no alpha channels. Use TextureFormat.RGB32 with alpha.
        // See Texture2DArray in unity scripting API.
        Texture2DArray textureArray = new Texture2DArray(256, 256, slices, TextureFormat.RGB24, false);
        
        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 1; i <= slices; i++)
        {
            string filename = string.Format(filePattern, i);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            textureArray.SetPixels(tex.GetPixels(0), i, 0);
        }
        textureArray.Apply();

        // CHANGEME: Path where you want to save the texture array. It must end in .asset extension for Unity to recognise it.
        string path = "Assets/Tasmania/Resources/SmokeTextureArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
    }
}