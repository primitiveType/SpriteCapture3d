using System;
using UnityEngine;

[Serializable]
public class AnimationMaterialPropertyBlock 
{
    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private string animationName;
    private static readonly int RowsProperty = Shader.PropertyToID("Rows");
    private static readonly int Columns1 = Shader.PropertyToID("Columns");

    public int Rows
    {
        get => rows;
        set => rows = value;
    }

    public int Columns
    {
        get => columns;
        set => columns = value;
    }

    public string AnimationName
    {
        get => animationName;
        set => animationName = value;
    }

    public MaterialPropertyBlock GetMaterialPropertyBlock()
    {
        var block = new MaterialPropertyBlock();
        block.SetInt(RowsProperty, Rows);
        block.SetInt(Columns1, Columns);
        //TODO:set textures

        return block;
    }
}