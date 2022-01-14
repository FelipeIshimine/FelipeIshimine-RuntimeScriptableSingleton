using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Object = UnityEngine.Object;

public static class RuntimeScriptableSingletonEditorInitializer
{


    public static string PreBuildProcess()
    {
        GetOrInstantiateAllInstances();
        ScanForAll();
        
        
        StringBuilder errors = new StringBuilder();
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in FindAssetsByType<BaseRuntimeScriptableSingleton>())
        {
            (bool success, string message) = baseRuntimeScriptableSingleton.PreBuildProcess();

            if (!success)
                errors.Append($"{message} \n");
        }
        return errors.ToString();
    }
    

    
    public static void ScanForAll()
    {
        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));

        var elements = runtimeScriptableSingletonInitializer.elements;
        
        elements.RemoveAll(x => x == null || !x.IncludeAsResource);
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in FindAssetsByType<BaseRuntimeScriptableSingleton>())
        {
            switch (baseRuntimeScriptableSingleton.loadMode)
            {
                case BaseRuntimeScriptableSingleton.AssetMode.EditorOnly:
                    break;
                case BaseRuntimeScriptableSingleton.AssetMode.Addressable:
                    AddToAddressableAssets(
                        baseRuntimeScriptableSingleton, 
                        runtimeScriptableSingletonInitializer.addressableGroupName, 
                        runtimeScriptableSingletonInitializer.addressableLabel);
                    break;
                case BaseRuntimeScriptableSingleton.AssetMode.Resources:
                    if(!elements.Contains(baseRuntimeScriptableSingleton))
                        elements.Add(baseRuntimeScriptableSingleton);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        EditorUtility.SetDirty(runtimeScriptableSingletonInitializer);
        AssetDatabase.SaveAssets();
    }
    
    private static void GetOrInstantiateAllInstances()
    {
        var types = GetAllSubclassTypes<BaseRuntimeScriptableSingleton>();
        foreach (Type currentType in types)
        {
            if (currentType.IsAbstract) continue;
                
            Object uObject = null;
            
            
            var objects = FindAssetsByType(currentType);
            
            
            
            
            if (objects.Count == 1)
                uObject = objects[0];
            else if (objects.Count > 1)
            {
                StringBuilder stringBuilder = new StringBuilder($"More than 1 instances of {currentType.Name} found");
                foreach (Object obj in objects)
                    stringBuilder.Append($"\n {AssetDatabase.GetAssetPath(obj)}");
                throw new Exception(stringBuilder.ToString());
            }
            
            
            if (uObject != null) continue;
            
            string currentPath = $"{BaseRuntimeScriptableSingleton.DefaultFileFolder}/{currentType.Name}.asset";
            uObject = AssetDatabase.LoadAssetAtPath(currentPath, currentType);
                
            if (uObject != null) continue;
            
            uObject = ScriptableObject.CreateInstance(currentType);
            AssetDatabase.CreateAsset(uObject, $"{currentPath}");
        }
        AssetDatabase.SaveAssets();
    }
    
    private  static IEnumerable<Type> GetAllSubclassTypes<T>() 
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where (type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
            select type;
    }
    
    private  static List<Object> FindAssetsByType(Type type)
    {
        List<Object> assets = new List<Object>();
        string[] guids = AssetDatabase.FindAssets($"t:{type}");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] found = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int index = 0; index < found.Length; index++)
            {
                if (found[index].GetType() == type && found[index] is { } item && !assets.Contains(item))
                    assets.Add(item);
                
            }
        }
        return assets;
    }
    private  static List<T> FindAssetsByType<T>()
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
    
    public static void AddToAddressableAssets(UnityEngine.Object asset, string groupName, params string[] labels)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        var currentLabels = new List<string>(settings.GetLabels());
        foreach (string label in labels)
        {
            if(!currentLabels.Contains(label))
                AddressableAssetSettingsDefaultObject.Settings.AddLabel(label);
        }
        
        if (settings)
        {
            var group = settings.FindGroup(groupName);
            if (!group)
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
 
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
 
            var addressableAssetEntry = settings.CreateOrMoveEntry(guid, group, false, false);

            foreach (string label in labels)
                addressableAssetEntry.labels.Add(label);
            
            var entriesAdded = new List<AddressableAssetEntry> {addressableAssetEntry};
 
            group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
        }
    }
}