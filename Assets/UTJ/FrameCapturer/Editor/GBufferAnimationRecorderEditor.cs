using System;
using UnityEditor;
using UnityEngine;
using UTJ.FrameCapturer;

[CustomEditor(typeof(GBufferAnimationRecorder))]
public class GBufferAnimationRecorderEditor : RecorderBaseEditor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();

        var recorder = target as GBufferAnimationRecorder;
        var so = serializedObject;
        EditorGUILayout.PropertyField(so.FindProperty("animationDictionary"), true);
        EditorGUILayout.PropertyField(so.FindProperty("Camera"), true);

        CommonConfig();
        EditorGUILayout.PropertyField(so.FindProperty("PixelsPerMeter"), true);
        EditorGUILayout.PropertyField(so.FindProperty("EncapsulateAnimatedBounds"), true);
        EditorGUILayout.PropertyField(so.FindProperty("NumRotationsToCapture"), true);
        EditorGUILayout.PropertyField(so.FindProperty("dontReposition"), true);
        EditorGUILayout.PropertyField(so.FindProperty("GenerateArrays"), true);
        
        EditorGUILayout.PropertyField(so.FindProperty("m_targetFramerate"), true);
        EditorGUILayout.PropertyField(so.FindProperty("TesterBounds"), true);


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Capture Components");
        EditorGUI.indentLevel++;
        {
            EditorGUI.BeginChangeCheck();
            var fbc = recorder.fbComponents;

            fbc.frameBuffer = EditorGUILayout.Toggle("Frame Buffer", fbc.frameBuffer);
            if (fbc.frameBuffer)
            {
                EditorGUI.indentLevel++;
                fbc.fbColor = EditorGUILayout.Toggle("Color", fbc.fbColor);
                fbc.fbAlpha = EditorGUILayout.Toggle("Alpha", fbc.fbAlpha);
                EditorGUI.indentLevel--;
            }

            fbc.GBuffer = EditorGUILayout.Toggle("GBuffer", fbc.GBuffer);
            if (fbc.GBuffer)
            {
                EditorGUI.indentLevel++;
                fbc.gbAlbedo = EditorGUILayout.Toggle("Albedo", fbc.gbAlbedo);
                fbc.gbOcclusion = EditorGUILayout.Toggle("Occlusion", fbc.gbOcclusion);
                fbc.gbSpecular = EditorGUILayout.Toggle("Specular", fbc.gbSpecular);
                fbc.gbSmoothness = EditorGUILayout.Toggle("Smoothness", fbc.gbSmoothness);
                fbc.gbNormal = EditorGUILayout.Toggle("Normal", fbc.gbNormal);
                fbc.gbEmission = EditorGUILayout.Toggle("Emission", fbc.gbEmission);
                fbc.gbDepth = EditorGUILayout.Toggle("Depth", fbc.gbDepth);
                fbc.gbVelocity = EditorGUILayout.Toggle("Velocity", fbc.gbVelocity);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                recorder.fbComponents = fbc;
                EditorUtility.SetDirty(recorder);
            }
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        ResolutionControl();
        FramerateControl();

        EditorGUILayout.Space();

        RecordingControl();

        so.ApplyModifiedProperties();
    }
}