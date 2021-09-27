using UnityEngine;

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
    /// <summary>
    /// Use throw new BuildFailedException(Message)
    /// </summary>
    public virtual (bool success, string errorMessage) PreBuildProcess() => (true, string.Empty);

}