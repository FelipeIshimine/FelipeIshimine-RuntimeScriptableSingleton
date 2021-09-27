using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuntimeScriptableSingletonInitializer : ScriptableObject
{
    public static RuntimeScriptableSingletonInitializer Instance { get; private set; }
    
    public List<BaseRuntimeScriptableSingleton> elements = new List<BaseRuntimeScriptableSingleton>();

    public static string DefaultFilePath => $"{DefaultFileFolder}/{DefaultFileName}";
    public const string DefaultFileFolder = "Assets/ScriptableObjects/Resources";
    public const string DefaultFileName = nameof(RuntimeScriptableSingletonInitializer);

    public static void Clear() => Instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));

        if (runtimeScriptableSingletonInitializer == null)
        {
#if UNITY_EDITOR
            bool selectedValue = EditorUtility.DisplayDialog($"Error de {nameof(RuntimeScriptableSingletonInitializer)}", $"{nameof(RuntimeScriptableSingletonInitializer)} not found in Resources.\nThe play session will be stopped. \n Do you want to create the asset now? \n The asset will be created at:\n{DefaultFilePath}", "Yes", "No");

            if (selectedValue)
            {
                if(!Directory.Exists(DefaultFileFolder)) Directory.CreateDirectory(DefaultFileFolder);

                AssetDatabase.CreateAsset(CreateInstance<RuntimeScriptableSingletonInitializer>(), $"{DefaultFilePath}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));
            }
#else
            throw new Exception($"{nameof(RuntimeScriptableSingletonInitializer)} not found in any Resources");
#endif
        }
      
#if UNITY_EDITOR
        GetOrInstantiateAllInstances();
        runtimeScriptableSingletonInitializer.ScanForAll();
#endif
        
        runtimeScriptableSingletonInitializer.InitializeElements();
    }

#if UNITY_EDITOR
    private void ScanForAll()
    {
        elements.RemoveAll(x => x == null || !x.IncludeInBuild);
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in FindAssetsByType<BaseRuntimeScriptableSingleton>())
        {
            if (baseRuntimeScriptableSingleton.IncludeInBuild)
            {
                if(!elements.Contains(baseRuntimeScriptableSingleton))
                    elements.Add(baseRuntimeScriptableSingleton);
            }
        }
    }
    
    public static void GetOrInstantiateAllInstances()
    {
        var types = GetAllSubclassTypes<BaseRuntimeScriptableSingleton>();
        foreach (Type item in types)
        {
            Object uObject = null;
            
            var objects = FindAssetsByType(item);
            
            if (objects.Count == 1)
                uObject = objects[0];
            else if (objects.Count > 1)
            {
                StringBuilder stringBuilder = new StringBuilder($"More than 1 instances of {item.Name} found");
                foreach (Object obj in objects)
                    stringBuilder.Append($"\n {AssetDatabase.GetAssetPath(obj)}");
                throw new Exception(stringBuilder.ToString());
            }
            
            
            if (uObject != null) continue;
            
            string currentPath = $"{BaseRuntimeScriptableSingleton.DefaultFileFolder}/{item.Name}.asset";
            uObject = AssetDatabase.LoadAssetAtPath(currentPath, item);
                
            if (uObject != null) continue;
            
            uObject = CreateInstance(item);
            AssetDatabase.CreateAsset(uObject, $"{currentPath}");
        }
        AssetDatabase.SaveAssets();
    }
    
    public static IEnumerable<Type> GetAllSubclassTypes<T>() 
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where (type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
            select type;
    }
    
    public static List<Object> FindAssetsByType(Type type)
    {
        List<Object> assets = new List<Object>();
        string[] guids = AssetDatabase.FindAssets($"t:{type}");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] found = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int index = 0; index < found.Length; index++)
                if (found[index] is { } item && !assets.Contains(item))
                    assets.Add(item);
        }
        return assets;
    }
    public static List<T> FindAssetsByType<T>()
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] found = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int index = 0; index < found.Length; index++)
                if (found[index] is T item && !assets.Contains(item))
                    assets.Add(item);
        }
        return assets;
    }
#endif

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

    public static string PreBuildProcess()
    {
        StringBuilder errors = new StringBuilder();

        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in Instance.elements)
        {
            (bool success, string message) = baseRuntimeScriptableSingleton.PreBuildProcess();

            if (!success)
                errors.Append($"{message} \n");
        }
        return errors.ToString();
    }
}