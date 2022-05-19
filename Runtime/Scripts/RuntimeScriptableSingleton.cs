using System;
using Sirenix.OdinInspector;
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
            if (_instance) return _instance;
#if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                _instance = FindOrCreate();
                return _instance;
            }
#endif
            throw new Exception($"{DefaultFileName} not initialized.");
        }
    }

    private static string DefaultFileName =>  typeof(T).Name;
    private static string DefaultFilePath => $"{DefaultFileFolder}/{DefaultFileName}.asset";

    public T Myself => this as T;

    private void OnValidate()
    {
        if (name != DefaultFileName)
            name = DefaultFileName;
    }

    public override void InitializeSingleton()
    {
        if(IsEditorOnly)
            throw new Exception($"Initializing EDITOR ONLY RuntimeScriptableSingleton {this.GetType()}");
        
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



