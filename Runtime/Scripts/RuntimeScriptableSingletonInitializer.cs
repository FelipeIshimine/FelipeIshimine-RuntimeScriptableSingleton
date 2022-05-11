using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuntimeScriptableSingletonInitializer : ScriptableObject
{
    public string addressableGroupName = "RuntimeScriptableSingleton";
    public string addressableLabel = "RuntimeScriptableSingleton";

    private static Action OnInitialization;
    public static RuntimeScriptableSingletonInitializer Instance { get; private set; }
    
    public List<BaseRuntimeScriptableSingleton> elements = new List<BaseRuntimeScriptableSingleton>();

    public static string DefaultFilePath => $"{DefaultFileFolder}/{DefaultFileName}";
    public const string DefaultFileFolder = "Assets/ScriptableObjects/Resources";
    public const string DefaultFileName = nameof(RuntimeScriptableSingletonInitializer);

    public static bool InitializationCompleted = false;
    public static bool InitializationStarted = false;

    public static void Clear() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static async void Initialize()
    {
        InitializationStarted = true;

        #region Resources Load

        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));


        if (runtimeScriptableSingletonInitializer == null)
        {
            throw new Exception($"{nameof(RuntimeScriptableSingletonInitializer)} not found in any Resources");
        }
    }
        OnInitialization?.Invoke();
        InitializationCompleted = true;


        runtimeScriptableSingletonInitializer.InitializeElements();

    public void InitializeElements()
    {
        if (Instance != null)
            throw new Exception($"{nameof(RuntimeScriptableSingletonInitializer)} already initialized");
        Instance = this;
        
        Debug.unityLogger.logEnabled = Debug.isDebugBuild;

        if (!Debug.isDebugBuild)
            Debug.Log("RelEaSe VeRsiOn: DeBuG DiSaBlEd");
        
        Debug.Log("<COLOR=white>---RuntimeScriptableSingleton Initializer---</color>");
        #if UNITY_EDITOR
        Debug.Log(AssetDatabase.GetAssetPath(this));
        #endif

        List<BaseRuntimeScriptableSingleton> sortedManagers = new List<BaseRuntimeScriptableSingleton>(elements);
        
        sortedManagers.Sort(RuntimeScriptableSingletonSorter);
        sortedManagers.Reverse();
        
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in sortedManagers)
            baseRuntimeScriptableSingleton.InitializeSingleton();
    }

    private static int RuntimeScriptableSingletonSorter(BaseRuntimeScriptableSingleton x, BaseRuntimeScriptableSingleton y) => x.InitializationPriority.CompareTo(y.InitializationPriority);


    public static void WhenInitializationIsDone(Action callback)
    {
        if(InitializationCompleted)
            callback?.Invoke();
        else
            OnInitialization += callback;
    }
   
}
