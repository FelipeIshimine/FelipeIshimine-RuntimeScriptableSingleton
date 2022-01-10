using System;
using System.Collections.Generic;
using System.IO;
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
#if UNITY_EDITOR
            bool selectedValue = EditorUtility.DisplayDialog(
                $"Error de {nameof(RuntimeScriptableSingletonInitializer)}",
                $"{nameof(RuntimeScriptableSingletonInitializer)} not found in Resources.\nThe play session will be stopped. \n Do you want to create the asset now? \n The asset will be created at:\n{DefaultFilePath}",
                "Yes", "No");

            if (selectedValue)
            {
                if (!Directory.Exists(DefaultFileFolder)) Directory.CreateDirectory(DefaultFileFolder);

                AssetDatabase.CreateAsset(CreateInstance<RuntimeScriptableSingletonInitializer>(),
                    $"{DefaultFilePath}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));
            }
#else
            throw new Exception($"{nameof(RuntimeScriptableSingletonInitializer)} not found in any Resources");
#endif
        }

        #endregion


        if (runtimeScriptableSingletonInitializer)
            runtimeScriptableSingletonInitializer = Instantiate(runtimeScriptableSingletonInitializer); //Creamos una copia temporal para que los cambios no se apliquen al asset en el editor

        #region Addressables Load

        var asyncOperation =
            Addressables.LoadAssetsAsync<BaseRuntimeScriptableSingleton>(
                runtimeScriptableSingletonInitializer.addressableLabel, null);

        await asyncOperation.Task;

        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in asyncOperation.Result)
        {
            Debug.Log($"RSSI: {baseRuntimeScriptableSingleton.name} added from AddressableAssets");
            runtimeScriptableSingletonInitializer.elements.Add(baseRuntimeScriptableSingleton);
        }

        #endregion

        runtimeScriptableSingletonInitializer.InitializeElements();

        InitializationCompleted = true;

    }


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

   
}