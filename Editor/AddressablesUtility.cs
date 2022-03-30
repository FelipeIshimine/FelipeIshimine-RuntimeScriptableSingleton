using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public static class AddressablesUtility
{
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
            if (!@group)
                @group = settings.CreateGroup(groupName, false, false, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
 
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
 
            var addressableAssetEntry = settings.CreateOrMoveEntry(guid, @group, false, false);

            foreach (string label in labels)
                addressableAssetEntry.labels.Add(label);
            
            var entriesAdded = new List<AddressableAssetEntry> {addressableAssetEntry};
 
            @group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
        }
    }
}