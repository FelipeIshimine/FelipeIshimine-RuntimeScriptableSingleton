using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class RuntimeScriptableSingletonBuildValidator  : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            string errorMessage = RuntimeScriptableSingletonEditorInitializer.PreBuildProcess();
            RuntimeScriptableSingletonInitializer.Clear();
            if (errorMessage.Length > 0)
            {
                Debug.LogError("Error");
                throw new UnityEditor.Build.BuildFailedException(new System.Exception(errorMessage));
            }
        }
        catch (System.Exception e) //Relanzamos el error
        {
            throw new UnityEditor.Build.BuildFailedException(e);
        }
    }
}