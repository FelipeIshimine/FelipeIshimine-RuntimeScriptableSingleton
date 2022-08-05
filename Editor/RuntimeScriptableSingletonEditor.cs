using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

            runtimeScriptableSingletonInitializer =
                ScriptableObject.CreateInstance<RuntimeScriptableSingletonInitializer>();

            AssetDatabase.CreateAsset(runtimeScriptableSingletonInitializer,
                $"{path}/{RuntimeScriptableSingletonInitializer.DefaultFileName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var runtimeSingletons = FindAllRuntimeScriptableSingleton();
        InstantiateMissing(runtimeSingletons);
        FilterSingletons(runtimeSingletons, runtimeScriptableSingletonInitializer);
        EditorUtility.SetDirty(runtimeScriptableSingletonInitializer);
    }

    private static void FilterSingletons(List<BaseRuntimeScriptableSingleton> runtimeSingletons,
        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer)
    {
        var forResources = runtimeSingletons.FindAll(x => x.IncludeAsResource);
        runtimeScriptableSingletonInitializer.SetLoadedFromResources(forResources);

        var forAddressableAsset = runtimeSingletons.FindAll(x => x.IncludeAsAddressable);

        List<AssetReference> assetReferences = new List<AssetReference>();

        var group = AddressableAssetSettingsDefaultObject.Settings.groups.Find(x =>
            x.Name == runtimeScriptableSingletonInitializer.addressableGroupName);

        if (group)
        {
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(group.entries);
            foreach (AddressableAssetEntry addressableAssetEntry in entries)
                group.RemoveAssetEntry(addressableAssetEntry);
        }

        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in forAddressableAsset)
        {
            AddressablesUtility.AddToAddressableAssets(
                baseRuntimeScriptableSingleton,
                runtimeScriptableSingletonInitializer.addressableGroupName,
                runtimeScriptableSingletonInitializer.addressableLabel);
            assetReferences.Add(new AssetReference(AssetDatabase
                .GUIDFromAssetPath(AssetDatabase.GetAssetPath(baseRuntimeScriptableSingleton)).ToString()));
        }

        runtimeScriptableSingletonInitializer.SetLoadedFromAddressableAssets(assetReferences);
    }

    public static string PreBuildProcess()
    {
        var allSingletons = FindAllRuntimeScriptableSingleton();
        RuntimeScriptableSingletonInitializer runtimeScriptableSingletonInitializer = Resources.Load<RuntimeScriptableSingletonInitializer>(nameof(RuntimeScriptableSingletonInitializer));
        
        StringBuilder errors = new StringBuilder();
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in allSingletons)
        {
            (bool success, string message) = baseRuntimeScriptableSingleton.PreBuildProcess();

            if (!success)
                errors.Append($"{message} \n");
        }

        InstantiateMissing(allSingletons);
        FilterSingletons(allSingletons, runtimeScriptableSingletonInitializer);
        EditorUtility.SetDirty(runtimeScriptableSingletonInitializer);
        
        return errors.ToString();
    }


    private static List<BaseRuntimeScriptableSingleton> FindAllRuntimeScriptableSingleton() =>
        FindAssetsByType<BaseRuntimeScriptableSingleton>();

    private static void ScanForAll(List<BaseRuntimeScriptableSingleton> elements)
    {
        elements.RemoveAll(x => x == null);
        foreach (BaseRuntimeScriptableSingleton baseRuntimeScriptableSingleton in
                 FindAssetsByType<BaseRuntimeScriptableSingleton>())
        {
            if (!elements.Contains(baseRuntimeScriptableSingleton))
                elements.Add(baseRuntimeScriptableSingleton);
        }
    }

    public static void InstantiateMissing(List<BaseRuntimeScriptableSingleton> baseRuntimeScriptableSingletons)
    {
        HashSet<Type> existing = new HashSet<Type>(baseRuntimeScriptableSingletons.ConvertAll(x => x.GetType()));

        var types = GetAllSubclassTypes<BaseRuntimeScriptableSingleton>();
        foreach (Type item in types)
        {
            if (existing.Contains(item))
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


            if (uObject != null)
                continue;

            var values =  FindAssetsByType(item);

            if (values.Count > 0)
            {
                string path = AssetDatabase.GetAssetPath(values[0]);
                Debug.Log($"${item.Name} Asset found with wrong name. Renaming it. From:{path} To:{AssetDatabase.RenameAsset(path, item.Name)}");
                continue;
            }

            if (EditorUtility.DisplayDialog($"Asset not found: {item.Name}", $"Could not found the asset {item.Name}.asset. Want to create a new one?", "Yes", "No"))
            {
                Debug.Log($"${item.Name} Asset not found at {currentPath}, creating new one");
                uObject = ScriptableObject.CreateInstance(item);
                AssetDatabase.CreateAsset(uObject, $"{currentPath}");                
            }
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

    public static List<Object> FindAssetsByType(Type type, bool onlyMainAsset = true)
    {
        List<Object> assets = new List<Object>();
        string[] guids = AssetDatabase.FindAssets($"t:{type}");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] found = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int index = 0; index < found.Length; index++)
            {
               if ((!onlyMainAsset || AssetDatabase.IsMainAsset(found[index])) && found[index] is { } item && !assets.Contains(item))
                    assets.Add(item);
            }
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
