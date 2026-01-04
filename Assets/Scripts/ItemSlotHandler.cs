using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class ItemSlotHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action OnClick;
    public Action OnLongPress;

    private bool isPressed = false;
    private float pressTime = 0f;
    private const float LongPressDuration = 0.5f;
    private bool longPressTriggered = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressTime = 0f;
        longPressTriggered = false;
        StartCoroutine(TrackPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (!longPressTriggered)
        {
            OnClick?.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPressed = false;
        longPressTriggered = false; // Cancel if dragged out
    }

    private IEnumerator TrackPress()
    {
        while (isPressed && !longPressTriggered)
        {
            pressTime += Time.unscaledDeltaTime; // Use unscaled to work even if paused (optional)
            
            if (pressTime >= LongPressDuration)
            {
                longPressTriggered = true;
                OnLongPress?.Invoke();
                // Optional: Provide haptic feedback or visual cue here
            }
            yield return null;
        }
    }
}
