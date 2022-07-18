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
                _instance = FindOrCreateInstanceAsset();
                return _instance;
            }
#endif
            throw new Exception($"{DefaultFileName} not initialized.");
        }
    }

    

    protected static string DefaultFileName =>  typeof(T).Name;
    protected static string DefaultFilePath => $"{DefaultFileFolder}/{DefaultFileName}.asset";

    public T Myself => this as T;

    private void OnValidate()
    {
        if (name != DefaultFileName)
            name = DefaultFileName;
    }

    public static bool Initialized => _instance != null;
    private static Action<T> _onInitialize;
    public static void WhenInitialize(Action<T> callback)
    {
        if (Initialized)
            callback?.Invoke(Instance);
        else
            _onInitialize += callback;
    }

    public override void InitializeSingleton()
    {
        if(IsEditorOnly)
            throw new Exception($"Initializing EDITOR ONLY RuntimeScriptableSingleton {this.GetType()}");
        
        if (_instance != null && _instance != this)
            throw new Exception($"Singleton error {this.GetType()}");
        
        _instance = this as T;
        Debug.Log($" <Color=white> |{InitializationPriority}|</color> <Color=green> {_instance}  </color> ");
        
        _onInitialize?.Invoke(_instance);
        _onInitialize = null;
    }


#if UNITY_EDITOR
    public static T FindOrCreateInstanceAsset()
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        _instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])); 
        if (!_instance)
        {
            _instance = CreateInstance<T>();
            System.IO.Directory.CreateDirectory(DefaultFilePath);
            AssetDatabase.CreateAsset(_instance, DefaultFilePath);
        }
        return _instance;
    }
#endif
}



