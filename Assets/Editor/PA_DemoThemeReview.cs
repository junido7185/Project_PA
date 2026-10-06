using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// D2의 단일 검수 확장. 기존 P3 입력/격리 저장/정리 경로를 그대로 사용한다.
public static class PA_DemoThemeReview
{
    const string Key = "PA.D2.ThemeReview";
    public static bool Active => SessionState.GetBool(Key,false);
    [MenuItem("Project PA/Validation/D2 Theme Island Panels")]
    public static void Island() { SessionState.SetBool(Key,true); PA_P3DirectGatherReview.RunUiInteractions(); }
    [MenuItem("Project PA/Validation/D2 Theme Departure Panels")]
    public static void Departure() { SessionState.SetBool(Key,true); PA_P3DirectGatherReview.RunIntro(); }
    public static void Clear() => SessionState.EraseBool(Key);

    public static async Task Capture720(string directory, string name)
    {
        var editor = typeof(Editor).Assembly;
        var type = editor.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(type);
        var selected = type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        int original = (int)selected.GetValue(view);
        var mouse = Mouse.current;
        Vector2 pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        Vector2 fraction = new Vector2(pointer.x/Screen.width,pointer.y/Screen.height);
        try
        {
            var sizesType = editor.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(editor.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
            var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
            int index = Array.FindIndex(labels,l=>l.Contains("1280") && l.Contains("720"));
            if (index < 0)
            {
                var sizeType = editor.GetType("UnityEditor.GameViewSize");
                var kind = editor.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
                    new object[]{Enum.Parse(kind,"FixedResolution"),1280,720,"D2 1280x720"},null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
                index = ((string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null)).Length-1;
            }
            selected.SetValue(view,index); view.Repaint();
            await Task.Delay(350);
            if (Screen.width != 1280 || Screen.height != 720) throw new InvalidOperationException("D2 GameView did not resize to720p");
            if (mouse != null) InputSystem.QueueStateEvent(mouse,new MouseState { position = Vector2.Scale(fraction,new Vector2(1280,720)) });
            await Task.Delay(200);
            string path = Path.Combine(directory,name+"-720.png");
            ScreenCapture.CaptureScreenshot(path);
            double until = EditorApplication.timeSinceStartup+6;
            while (!File.Exists(path) && EditorApplication.timeSinceStartup<until) await Task.Delay(100);
            if (!File.Exists(path)) throw new InvalidOperationException("D2 missing720p screenshot "+name);
        }
        finally
        {
            selected.SetValue(view,original); view.Repaint();
            await Task.Delay(300);
            if (mouse != null) InputSystem.QueueStateEvent(mouse,new MouseState { position = pointer });
            await Task.Delay(150);
        }
    }
}
