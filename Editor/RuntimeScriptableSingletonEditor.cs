﻿#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RuntimeScriptableSingletonEditor
{
    static RuntimeScriptableSingletonEditor()
    {
        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer =
            Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));

        if (!runtimeScriptableSingletonInitializer)
        {
            string path = RuntimeScriptableSingletonInitializer.DefaultFileFolder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            runtimeScriptableSingletonInitializer = ScriptableObject.CreateInstance<RuntimeScriptableSingletonInitializer>();
            
            AssetDatabase.CreateAsset(runtimeScriptableSingletonInitializer, $"{path}/{RuntimeScriptableSingletonInitializer.DefaultFileName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        InstantiateMissing(runtimeScriptableSingletonInitializer.elements);
        ScanForAll(runtimeScriptableSingletonInitializer.elements);
    }
    
    private static void ScanForAll(List<BaseRuntimeScriptableSingleton> elements)
    {
        elements.RemoveAll(x => x == null);
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in FindAssetsByType<BaseRuntimeScriptableSingleton>())
        {
            if(!elements.Contains(baseRuntimeScriptableSingleton))
                elements.Add(baseRuntimeScriptableSingleton);
        }
    }
    
    public static void InstantiateMissing(List<BaseRuntimeScriptableSingleton> baseRuntimeScriptableSingletons)
    {
        HashSet<Type> existing = new HashSet<Type>(baseRuntimeScriptableSingletons.ConvertAll(x => x.GetType()));

        var types = GetAllSubclassTypes<BaseRuntimeScriptableSingleton>();
        foreach (Type item in types)
        {
            if(existing.Contains(item))
                continue;
            
            Object uObject = null;
            var objects = FindAssetsByType(item);
            objects.RemoveAll(x => x.GetType() != item);
            
            if (objects.Count == 1)
                uObject = objects[0];
            else if (objects.Count > 1)
            {
                StringBuilder stringBuilder = new StringBuilder($"More than 1 instances of {item.Name} found");
                foreach (Object obj in objects)
                    stringBuilder.Append($"\n {AssetDatabase.GetAssetPath(obj)} T:{obj.GetType()}");
                throw new Exception(stringBuilder.ToString());
            }
            
            if (uObject != null) continue;

            if (!AssetDatabase.IsValidFolder(BaseRuntimeScriptableSingleton.DefaultFileFolder))
            {
                string fullPath = Application.dataPath.Replace("/Assets", string.Empty);
                fullPath += $"/{BaseRuntimeScriptableSingleton.DefaultFileFolder}";
                
                Debug.Log($"Creating directory: {fullPath}");
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
            
            string currentPath = $"{BaseRuntimeScriptableSingleton.DefaultFileFolder}/{item.Name}.asset";
            uObject = AssetDatabase.LoadAssetAtPath(currentPath, item);
                
            if (uObject != null) continue;
            
            uObject = ScriptableObject.CreateInstance(item);
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
}
#endif