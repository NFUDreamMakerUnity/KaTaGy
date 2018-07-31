﻿using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewEditor : Editor {

    public override void OnInspectorGUI()
    {
        
        FieldOfView fow = (FieldOfView)target;
        GUILayout.Label("MatchType:");
        fow.firstPriority = (GameManager.NowTarget)EditorGUILayout.EnumPopup(fow.firstPriority);
        GUILayout.FlexibleSpace();
        base.OnInspectorGUI();
    }

    void OnSceneGUI()
    {
        FieldOfView fow = (FieldOfView)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);

        if (fow.currentTarget != null)
            Handles.DrawLine(fow.transform.position, fow.currentTarget.transform.position);
    }
}
