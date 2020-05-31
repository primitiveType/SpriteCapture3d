using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
[Serializable]
public class AnimationMaterialDictionary : ScriptableObject
{
    [SerializeField] private List<AnimationMaterialPropertyBlock> PropertyBlocksByModelAnimation;

    public void AddPropertyBlock(string modelName, string animationName, int rows, int columns)
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
            Rows = rows
        };
        var oldItem = PropertyBlocksByModelAnimation.FirstOrDefault(item => item.AnimationName == key);
        if (oldItem != null)
        {
            PropertyBlocksByModelAnimation.Remove(oldItem);
        }

        PropertyBlocksByModelAnimation.Add(block);
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
}