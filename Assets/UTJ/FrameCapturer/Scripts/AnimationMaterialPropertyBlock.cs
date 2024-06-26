﻿using System;
using UnityEngine;

[Serializable]
public class AnimationMaterialPropertyBlock 
{
    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private string animationName;
    [SerializeField] private int numFrames;
    [SerializeField] private Texture normalMap;
    [SerializeField] private Texture diffuseMap;
    [SerializeField] private Texture alphaMap;
    [SerializeField] private float normalizedGroundPosition;
    private static readonly int RowsProperty = Shader.PropertyToID("Rows");
    private static readonly int ColumnsProperty = Shader.PropertyToID("Columns");
    private static readonly int NumFramesProperty = Shader.PropertyToID("NumFrames");
    private static readonly int GroundPositionProperty = Shader.PropertyToID("GroundPosition");
    private static readonly int TexturesProperty = Shader.PropertyToID("Textures");
    private static readonly int AlphaProperty = Shader.PropertyToID("Alpha");
    private static readonly int NormalsProperty = Shader.PropertyToID("Normals");

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

    public int NumFrames
    {
        get => numFrames;
        set => numFrames = value;
    }

    public float NormalizedGroundPosition
    {
        get => normalizedGroundPosition;
        set => normalizedGroundPosition = value;
    }

    public Texture NormalMap
    {
        get => normalMap;
        set => normalMap = value;
    }

    public Texture DiffuseMap
    {
        get => diffuseMap;
        set => diffuseMap = value;
    }

    public Texture AlphaMap
    {
        get => alphaMap;
        set => alphaMap = value;
    }

    public MaterialPropertyBlock GetMaterialPropertyBlock()
    {
        var block = new MaterialPropertyBlock();
        block.SetInt(RowsProperty, Rows);
        block.SetInt(ColumnsProperty, Columns);
        block.SetInt(NumFramesProperty, NumFrames);
        block.SetFloat(GroundPositionProperty, NormalizedGroundPosition);
        block.SetTexture(NormalsProperty, NormalMap);
        block.SetTexture(AlphaProperty, AlphaMap);
        block.SetTexture(TexturesProperty, DiffuseMap);
        return block;
    }
}