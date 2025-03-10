#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(HexGridTool))]
public class HexGridToolEditor : Editor
{
    private bool IsEraseModeActive = false;
    private bool MapHiddenInPlayMode = false;
    private EnumHexGridMode PreviousMode;

    public override void OnInspectorGUI()
    {
        HexGridTool HexGridTool = (HexGridTool)target;

        if (Application.isPlaying)
        {
            if (!MapHiddenInPlayMode)
            {
                HexGridTool.HexWalkable.HideHexWalkableMap();
                MapHiddenInPlayMode = true;
            }
            EditorGUILayout.HelpBox("HexGridToolEditor не доступен во время воспроизведения.", MessageType.Warning);
            return;
        }
        else
        {
            if (MapHiddenInPlayMode)
            {
                HexGridTool.HexWalkable.ShowHexWalkableMap();
                MapHiddenInPlayMode = false;
            }
        }

        if (PreviousMode == EnumHexGridMode.SetWalkable && HexGridTool.Mode != EnumHexGridMode.SetWalkable)
        {
            HexGridTool.HexWalkable.HideHexWalkableMap();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        PreviousMode = HexGridTool.Mode;

        HexGridTool.Mode = (EnumHexGridMode)EditorGUILayout.EnumPopup("Mode", HexGridTool.Mode);

        EditorGUILayout.Space();

        switch (HexGridTool.Mode)
        {
            case EnumHexGridMode.Generation:
                HexGridTool.GenerationType = (EnumHexGridGenerationType)EditorGUILayout.EnumPopup("Generation Type", HexGridTool.GenerationType);
                HexGridTool.HexPrefab = (Hex)EditorGUILayout.ObjectField("Hex Prefab", HexGridTool.HexPrefab, typeof(Hex), false);

                if (HexGridTool.HexPaintOptions.Count == 0)
                {
                    EditorGUILayout.HelpBox("Warning! If you do not add a paint value in Paint mode, it will not work correctly.", MessageType.Warning);
                }
                else
                {
                    ShowPaintPannel(HexGridTool);
                }

                switch (HexGridTool.GenerationType)
                {
                    case EnumHexGridGenerationType.Hexagonal:
                        HexGridTool.ArenaRadius = EditorGUILayout.IntField("Arena Radius", HexGridTool.ArenaRadius);
                        HexGridTool.SquareWidth = 0;
                        HexGridTool.SquareLenght = 0;
                        break;

                    case EnumHexGridGenerationType.Square:
                        HexGridTool.SquareWidth = EditorGUILayout.IntField("Square Width", HexGridTool.SquareWidth);
                        HexGridTool.SquareLenght = EditorGUILayout.IntField("Square Height", HexGridTool.SquareLenght);
                        break;

                    default: break;
                }

                HexGridTool.TargetHeight = EditorGUILayout.IntField("Target Height", Mathf.Clamp(HexGridTool.TargetHeight, 1, 50));

                if (GUILayout.Button("Generate Hex Grid"))
                {
                    HexGridTool.HexWalkable.Refresh();
                    HexGridTool.HexCreator.Refresh();

                    HexGridTool.HexWalkable.ShowHexWalkableMap();

                    EditorApplication.update += MonitorGenerationCompletion;

                    switch (HexGridTool.GenerationType)
                    {
                        case EnumHexGridGenerationType.Hexagonal:
                            HexGridTool.HexGridGenerator.GenerateHexagonalGridWithJobs(HexGridTool.ArenaRadius, HexGridTool.TargetHeight, HexGridTool.HexPrefab, HexGridTool.HexPaintOptions);
                            break;
                        case EnumHexGridGenerationType.Square:
                            HexGridTool.HexGridGenerator.GenerateSquareGridWithJobs(HexGridTool.SquareWidth, HexGridTool.SquareLenght, HexGridTool.TargetHeight, HexGridTool.HexPrefab, HexGridTool.HexPaintOptions);
                            break;
                        default: break;
                    }

                    EditorUtility.SetDirty(HexGridTool);
                }
                if (HexGridTool.HexGridGenerator.GenerationTime > 0)
                {
                    EditorGUILayout.HelpBox($"Generation completed in {HexGridTool.HexGridGenerator.GenerationTime:F2} seconds.", MessageType.Info);
                }

                if (GUILayout.Button("Clear Hex Grid"))
                {
                    HexGridTool.HexWalkable.HideHexWalkableMap();
                    HexGridTool.HexWalkable.Refresh();
                    HexGridTool.HexCreator.Refresh();
                    HexGridTool.HexGridGenerator.ClearHexGrid();
                    HexGridTool.HexGridGenerator.ResetProgress();

                    EditorUtility.SetDirty(HexGridTool);
                    Repaint();
                }
                break;

            case EnumHexGridMode.Paint:
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

            case EnumHexGridMode.Transform:
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

            case EnumHexGridMode.Creation:
                EditorGUILayout.LabelField("Creation Settings", EditorStyles.boldLabel);

                HexGridTool.HexPrefab = (Hex)EditorGUILayout.ObjectField("Hex Prefab", HexGridTool.HexPrefab, typeof(Hex), false);

                if (HexGridTool.HexPaintOptions.Count == 0)
                {
                    EditorGUILayout.HelpBox("Warning! If you do not add a paint value in Paint mode, it will not work correctly.", MessageType.Warning);
                } 
                else
                {
                    ShowPaintPannel(HexGridTool);
                }

                HexGridTool.TargetHeight = EditorGUILayout.IntField("Target Height", Mathf.Clamp(HexGridTool.TargetHeight, 1, 50));
                HexGridTool.TargetLenght = EditorGUILayout.IntField("Target Lenght", Mathf.Clamp(HexGridTool.TargetLenght, 1, 50));

                break;

            case EnumHexGridMode.SetWalkable:

                EditorGUILayout.LabelField("Set Walkable Settings", EditorStyles.boldLabel);

                HexGridTool.HexWalkable.ShowHexWalkableMap();

                Color walkableColor = new Color(0.2f, 0.7f, 0.2f);
                Color unwalkableColor = new Color(0.7f, 0.2f, 0.2f);

                Color buttonColor = HexGridTool.IsWalkable ? walkableColor : unwalkableColor;
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 14;
                buttonStyle.fixedHeight = 30;
                buttonStyle.normal.textColor = Color.white;

                GUI.backgroundColor = buttonColor;

                string buttonText = HexGridTool.IsWalkable ? "Walkable" : "Unwalkable";
                if (GUILayout.Button(buttonText, buttonStyle))
                {
                    HexGridTool.IsWalkable = !HexGridTool.IsWalkable;
                }

                GUI.backgroundColor = Color.white;
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

    private void ShowPaintPannel(HexGridTool HexGridTool)
    {
        EditorGUILayout.Space();

        Color OriginalColorGen = GUI.backgroundColor;
        GUI.backgroundColor = IsEraseModeActive ? Color.green : OriginalColorGen;

        GUI.backgroundColor = OriginalColorGen;

        for (int i = 0; i < HexGridTool.HexPaintOptions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            string ButtonText = HexGridTool.HexPaintOptions[i].HexPrefab != null ? "Select" : "None";
            GUI.backgroundColor = HexGridTool.HexPaintOptions[i].IsSelected ? Color.green : OriginalColorGen;

            if (GUILayout.Button(ButtonText, GUILayout.Width(80)))
            {
                IsEraseModeActive = false;
                HexGridTool.IsEraseMode = false;
                SelectOption(HexGridTool.HexPaintOptions, i);
            }

            GUI.backgroundColor = OriginalColorGen;

            GUILayout.FlexibleSpace();
            HexGridTool.HexPaintOptions[i].HexPrefab = (MeshRenderer)EditorGUILayout.ObjectField("", HexGridTool.HexPaintOptions[i].HexPrefab, typeof(MeshRenderer), false, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();        
    }

    private void MonitorGenerationCompletion()
    {
        HexGridTool HexGridTool = (HexGridTool)target;

        if (HexGridTool.HexGridGenerator.GenerationTime > 0)
        {
            EditorApplication.update -= MonitorGenerationCompletion;

            Repaint();
        }
    }
}
#endif