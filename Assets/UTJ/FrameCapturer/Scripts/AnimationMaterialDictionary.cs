using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;

[CreateAssetMenu]
[Serializable]
public class AnimationMaterialDictionary : ScriptableObject
{
    [SerializeField] private List<AnimationMaterialPropertyBlock> PropertyBlocksByModelAnimation;

    public void AddPropertyBlock(Texture diffuse, Texture alpha, Texture normals, string modelName, string animationName,  int columns, int rows, int numFrames)
    {
        if (PropertyBlocksByModelAnimation == null)
        {
            PropertyBlocksByModelAnimation = new List<AnimationMaterialPropertyBlock>();
        }

        string key = $"{modelName}_{animationName}";
        var block = new AnimationMaterialPropertyBlock
        {
            AnimationName = key,
            Columns = columns,
            Rows = rows, 
            NumFrames = numFrames,
            DiffuseMap = diffuse,
            AlphaMap =  alpha,
            NormalMap = normals
        };
        var oldItem = PropertyBlocksByModelAnimation.FirstOrDefault(item => item.AnimationName == key);
        if (oldItem != null)
        {
            PropertyBlocksByModelAnimation.Remove(oldItem);
        }

        PropertyBlocksByModelAnimation.Add(block);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public void DebugMe()
    {
        foreach (var anim in PropertyBlocksByModelAnimation)
        {
            Debug.Log(anim.AnimationName);
        }
    }

    public MaterialPropertyBlock GetPropertyBlock(string modelName, string animationName)
    {
        return PropertyBlocksByModelAnimation.First(item=>item.AnimationName == $"{modelName}_{animationName}").GetMaterialPropertyBlock();
    }
    public MaterialPropertyBlock GetPropertyBlock(string key)
    {
        return PropertyBlocksByModelAnimation.First(item=>item.AnimationName == key).GetMaterialPropertyBlock();
    }
}
