using UnityEditor;
using UnityEngine;

/// <summary>自动化 Play 验证辅助：编辑器失焦时播放循环会冻结，先置 runInBackground 再排一帧。</summary>
public static class PlayResume
{
    public static string Run()
    {
        Application.runInBackground = true;
        EditorApplication.QueuePlayerLoopUpdate();
        return "frame=" + Time.frameCount;
    }
}
