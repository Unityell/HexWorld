#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(HexGridTool))]
public class HexGridToolEditor : Editor
{
    private bool IsEraseModeActive = false;

    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("HexGridToolEditor не доступен во время воспроизведения.", MessageType.Warning);
            return;
        }

        HexGridTool HexGridTool = (HexGridTool)target;

        Undo.RecordObject(HexGridTool, "Modify HexGridTool");

        HexGridTool.Mode = (EnumHexGridMode)EditorGUILayout.EnumPopup("Mode", HexGridTool.Mode);

        EditorGUILayout.Space();

        switch (HexGridTool.Mode)
        {
            case EnumHexGridMode.Generation :

                HexGridTool.GenerationType = (EnumHexGridGenerationType)EditorGUILayout.EnumPopup("Generation Type", HexGridTool.GenerationType);

                HexGridTool.Ground = (Hex)EditorGUILayout.ObjectField("Ground Prefab", HexGridTool.Ground, typeof(Hex), false);

                switch (HexGridTool.GenerationType)
                {
                    case EnumHexGridGenerationType.Hexagonal:
                        HexGridTool.ArenaRadius = EditorGUILayout.IntField("Arena Radius", HexGridTool.ArenaRadius);
                        HexGridTool.SquareWidth = 0;
                        HexGridTool.SquareHeight = 0;
                        break;

                    case EnumHexGridGenerationType.Square:
                        HexGridTool.SquareWidth = EditorGUILayout.IntField("Square Width", HexGridTool.SquareWidth);
                        HexGridTool.SquareHeight = EditorGUILayout.IntField("Square Height", HexGridTool.SquareHeight);
                        break;

                    default: break;
                }

                HexGridTool.TargetHeight = EditorGUILayout.IntField("Target Height", Mathf.Clamp(HexGridTool.TargetHeight, 1, 50));

                if (GUILayout.Button("Generate Hex Grid"))
                {
                    switch (HexGridTool.GenerationType)
                    {
                        case EnumHexGridGenerationType.Hexagonal :
                            HexGridTool.HexGridGenerator.GenerateHexagonalGrid(HexGridTool.ArenaRadius, HexGridTool.TargetHeight, HexGridTool.Ground);
                            break;
                        case EnumHexGridGenerationType.Square :
                            HexGridTool.HexGridGenerator.GenerateHexagonalGridByDimensions(HexGridTool.SquareHeight, HexGridTool.SquareWidth, HexGridTool.TargetHeight, HexGridTool.Ground);
                            break;
                        default: break;
                    }
                    EditorUtility.SetDirty(HexGridTool);
                }

                if (GUILayout.Button("Clear Hex Grid"))
                {
                    HexGridTool.HexGridGenerator.ClearHexGrid();
                    EditorUtility.SetDirty(HexGridTool);
                }
                break;
            case EnumHexGridMode.Paint :

                EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel);

                if (HexGridTool.HexPaintOptions == null) HexGridTool.HexPaintOptions = new List<HexPaintOption>();

                EditorGUILayout.Space();

                Color OriginalColor = GUI.backgroundColor;
                GUI.backgroundColor = IsEraseModeActive ? Color.green : OriginalColor;

                if (GUILayout.Button("Erase Mode"))
                {
                    IsEraseModeActive = !IsEraseModeActive;
                    HexGridTool.IsEraseMode = IsEraseModeActive;
                    DeselectAllOptions(HexGridTool.HexPaintOptions);
                }

                GUI.backgroundColor = OriginalColor;

                for (int i = 0; i < HexGridTool.HexPaintOptions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    string ButtonText = HexGridTool.HexPaintOptions[i].HexPrefab != null ? "Select" : "None";
                    GUI.backgroundColor = HexGridTool.HexPaintOptions[i].IsSelected ? Color.green : OriginalColor;

                    if (GUILayout.Button(ButtonText, GUILayout.Width(80)))
                    {
                        IsEraseModeActive = false;
                        HexGridTool.IsEraseMode = false;
                        SelectOption(HexGridTool.HexPaintOptions, i);
                    }

                    GUI.backgroundColor = OriginalColor;

                    GUILayout.FlexibleSpace();
                    HexGridTool.HexPaintOptions[i].HexPrefab = (MeshRenderer)EditorGUILayout.ObjectField("", HexGridTool.HexPaintOptions[i].HexPrefab, typeof(MeshRenderer), false, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        HexGridTool.HexPaintOptions.RemoveAt(i);
                        EditorUtility.SetDirty(HexGridTool);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Hex Paint Option"))
                {
                    HexGridTool.HexPaintOptions.Add(new HexPaintOption());
                    EditorUtility.SetDirty(HexGridTool);
                }            
                break;
            case EnumHexGridMode.Transform :

                EditorGUILayout.LabelField("Transform Settings", EditorStyles.boldLabel);

                HexGridTool.TransformTool = (EnumTransformTool)EditorGUILayout.EnumPopup("Transform Tool", HexGridTool.TransformTool);

                if (HexGridTool.TransformTool == EnumTransformTool.SetHeight)
                {
                    HexGridTool.TargetHeight = EditorGUILayout.IntField("Target Height", Mathf.Clamp(HexGridTool.TargetHeight, 1, 50));
                }      
                if (HexGridTool.TransformTool == EnumTransformTool.SetLenght)
                {
                    HexGridTool.TargetLenght = EditorGUILayout.IntField("Target Lenght", Mathf.Clamp(HexGridTool.TargetLenght, 1, 50));
                }         
                break;
            case EnumHexGridMode.Creation :

                EditorGUILayout.LabelField("Creation Settings", EditorStyles.boldLabel);

                HexGridTool.Ground = (Hex)EditorGUILayout.ObjectField("Ground Prefab", HexGridTool.Ground, typeof(Hex), false);
                HexGridTool.TargetHeight = EditorGUILayout.IntField("Target Height", Mathf.Clamp(HexGridTool.TargetHeight, 1, 50));
                HexGridTool.TargetLenght = EditorGUILayout.IntField("Target Lenght", Mathf.Clamp(HexGridTool.TargetLenght, 1, 50));

                break;
            default: break;
        }

        if (GUI.changed) EditorUtility.SetDirty(HexGridTool);
    }

    private void SelectOption(List<HexPaintOption> Options, int SelectedIndex)
    {
        for (int i = 0; i < Options.Count; i++)
        {
            Options[i].IsSelected = i == SelectedIndex;
        }
    }

    private void DeselectAllOptions(List<HexPaintOption> Options)
    {
        foreach (var Option in Options)
        {
            Option.IsSelected = false;
        }
    }
}
#endif