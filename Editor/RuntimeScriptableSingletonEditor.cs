using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RuntimeScriptableSingletonEditor
{
    [MenuItem("RuntimeScriptableSingleton/Instantiate Missing", priority = 1)]
    public static void GetOrInstantiateAllInstances()
    {
        RuntimeScriptableSingletonEditorInitializer.PreBuildProcess();
    }

}