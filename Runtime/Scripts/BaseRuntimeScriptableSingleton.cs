using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class BaseRuntimeScriptableSingleton : ScriptableObject
{
    /// <summary>
    /// Objetos con mayor prioridad de inicializan primero
    /// </summary>
    public virtual int InitializationPriority => 0;

    [System.Serializable]
    public enum AssetMode
    {
        EditorOnly,
        Addressable,
        Resources,
        AddressableManual
    }

    public AssetMode loadMode = AssetMode.Addressable;
    
    public bool IsEditorOnly => loadMode == AssetMode.EditorOnly;
    public bool IncludeAsResource => loadMode == AssetMode.Resources;
    public bool IncludeAsAddressable => loadMode == AssetMode.Addressable;
    
    public abstract void InitializeSingleton();
    
    public static string DefaultFileFolder => "Assets/ScriptableObjects/Managers";
    
    
    
    /// <summary>
    /// Use throw new BuildFailedException(Message)
    /// </summary>
    public virtual (bool success, string errorMessage) PreBuildProcess() => (true, string.Empty);

}