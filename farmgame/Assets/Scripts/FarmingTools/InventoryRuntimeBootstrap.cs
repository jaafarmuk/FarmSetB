using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InventoryRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInventoryRuntime()
    {
        EnsureEventSystem();

        if (Object.FindAnyObjectByType<InventorySystem>() == null)
        {
            GameObject inventorySystemObject = new GameObject("Manager_Inventory");
            inventorySystemObject.AddComponent<InventorySystem>();
        }

        if (Object.FindAnyObjectByType<HotbarController>() == null)
        {
            GameObject hotbarControllerObject = new GameObject("Manager_Hotbar");
            hotbarControllerObject.AddComponent<HotbarController>();
        }

        if (Object.FindAnyObjectByType<InventoryUI>() != null)
        {
            return;
        }

        Canvas canvas = EnsureCanvas();
        GameObject inventoryUiObject = new GameObject("UI_InventoryController", typeof(RectTransform));
        inventoryUiObject.transform.SetParent(canvas.transform, false);
        inventoryUiObject.AddComponent<InventoryUI>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystemObject);
    }

    private static Canvas EnsureCanvas()
    {
        Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();

        if (existingCanvas != null)
        {
            return existingCanvas;
        }

        GameObject canvasObject = new GameObject("UI_Root", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }
}
