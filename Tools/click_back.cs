using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Play 验证辅助：用 UGUI 事件管线派发 Back 按钮的 pointerClick（legacy input 无注入 API）。</summary>
public static class ClickBack
{
    public static string Run()
    {
        var buttons = Object.FindObjectsOfType<Button>();
        foreach (var button in buttons)
        {
            if (button.name != "Back")
            {
                continue;
            }

            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
            return "clicked Back";
        }

        return "Back button not found";
    }
}
