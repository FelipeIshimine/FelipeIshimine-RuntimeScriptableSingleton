﻿using System;
using UnityEngine;
using Object = System.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VersionControl;
#endif

/// <summary>
/// Singleton que sea auto instancia e inicializa dentro de la carpeta Resources
/// </summary>
/// <typeparam name="T">Referencia circular a la propia clase de la que se quiere hacer Singleton</typeparam>
public abstract class RuntimeScriptableSingleton<T> : BaseRuntimeScriptableSingleton where T : RuntimeScriptableSingleton<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (!_instance)
            {
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    _instance = FindOrCreate();
                    return _instance;
                }
                #endif
                throw new Exception($"{DefaultFileName} not initialized.");
            }
            return _instance;
        }
    }

    public static string DefaultFileName =>  typeof(T).Name;
    public static string DefaultFilePath => $"{DefaultFileFolder}/{DefaultFileName}.asset";

    public T Myself => this as T;

    private void OnValidate()
    {
        if (name != DefaultFileName)
            name = DefaultFileName;
    }

    public override void InitializeSingleton()
    {
        if(!IncludeInBuild)
            throw new Exception($"Initializing EDITOR ONLY RuntimScriptableSingleton {this.GetType()}");
        
        if (_instance != null && _instance != this)
            throw new Exception($"Singleton error {this.GetType()}");
        
        _instance = this as T;
        Debug.Log($" <Color=white> |{InitializationPriority}|</color> <Color=green> {_instance}  </color> ");
    }

#if UNITY_EDITOR
    public static T FindOrCreate()
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        _instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])); 
        if (_instance == null)
        {
            _instance = CreateInstance<T>();
            System.IO.Directory.CreateDirectory(DefaultFilePath);
            AssetDatabase.CreateAsset(_instance, DefaultFilePath);
        }
        return _instance;
    }
#endif
}


public abstract class BaseRuntimeScriptableSingleton : ScriptableObject
{
    /// <summary>
    /// Objetos con mayor prioridad de inicializan primero
    /// </summary>
    public virtual int InitializationPriority => 0;

    [SerializeField] private bool includeInBuild = true;
    public bool IncludeInBuild => includeInBuild;
    
    public abstract void InitializeSingleton();
    
    public static string DefaultFileFolder => "Assets/ScriptableObjects/Managers";

}

