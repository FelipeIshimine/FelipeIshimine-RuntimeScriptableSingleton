using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.AddressableAssets;

public class RuntimeScriptableSingletonInitializer : ScriptableObject
{
    public string addressableGroupName = "RuntimeScriptableSingleton";
    public string addressableLabel = "RuntimeScriptableSingleton";

    private static Action OnInitialization;
    public static RuntimeScriptableSingletonInitializer Instance { get; private set; }
    
    [SerializeField] private List<BaseRuntimeScriptableSingleton> loadedByResources = new List<BaseRuntimeScriptableSingleton>();
    [SerializeField] private List<AssetReference> loadedByAddressableAssets = new List<AssetReference>();
    
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
        Instance = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));
        
        var allRuntimeScriptableSingletons = new List<BaseRuntimeScriptableSingleton>(Instance.loadedByResources);

        if (Instance == null)
            throw new Exception($"{nameof(RuntimeScriptableSingletonInitializer)} not found in any Resources");
       
        var asyncOperation = Addressables.LoadAssetsAsync<BaseRuntimeScriptableSingleton>(Instance.addressableLabel, null);

        Debug.Log($"RSSI: Load RuntimeSingletons from AddressableAssets START");
        await asyncOperation.Task;
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in asyncOperation.Result)
        {
            Debug.Log($"RSSI: {baseRuntimeScriptableSingleton.name} added from AddressableAssets");
            allRuntimeScriptableSingletons.Add(baseRuntimeScriptableSingleton);
        }
        Debug.Log($"RSSI: Load RuntimeSingletons from AddressableAssets COMPLETE");
      
        if (!Debug.isDebugBuild)
            Debug.Log("RelEaSe VeRsiOn: DeBuG DiSaBlEd");

        Debug.unityLogger.logEnabled = Debug.isDebugBuild;

        Debug.Log("<COLOR=white>---RuntimeScriptableSingleton Initializer---</color>");
        allRuntimeScriptableSingletons.Sort(RuntimeScriptableSingletonSorter);
        allRuntimeScriptableSingletons.Reverse();

        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in allRuntimeScriptableSingletons)
            baseRuntimeScriptableSingleton.InitializeSingleton();
        
        
        OnInitialization?.Invoke();
        InitializationCompleted = true;
    }

    private static int RuntimeScriptableSingletonSorter(BaseRuntimeScriptableSingleton x, BaseRuntimeScriptableSingleton y) => x.InitializationPriority.CompareTo(y.InitializationPriority);

    public static void WhenInitializationIsDone(Action callback)
    {
        if(InitializationCompleted)
            callback?.Invoke();
        else
            OnInitialization += callback;
    }

    public void SetLoadedFromResources(List<BaseRuntimeScriptableSingleton> runtimeSingletons) => loadedByResources = runtimeSingletons;

    public void SetLoadedFromAddressableAssets(List<AssetReference> runtimeSingletons)
    {
        loadedByAddressableAssets = runtimeSingletons;
    }
}

