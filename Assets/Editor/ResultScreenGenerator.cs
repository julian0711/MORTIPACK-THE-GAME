using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ResultScreenGenerator
{
    [MenuItem("Tools/Fix Result Screen")]
    public static void CreateResultScreen()
    {
        // 1. Find MobileUI
        GameObject mobileUI = GameObject.Find("MobileUI");
        if (mobileUI == null)
        {
            Debug.LogError("MobileUI not found!");
            return;
        }

        // 2. Create Panel
        GameObject resultScreen = new GameObject("ResultScreen");
        resultScreen.transform.SetParent(mobileUI.transform, false);
        
        RectTransform rt = resultScreen.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        Image img = resultScreen.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        // 3. Create Button
        GameObject nextButton = new GameObject("NextButton");
        nextButton.transform.SetParent(resultScreen.transform, false);
        
        RectTransform btnRT = nextButton.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(200, 60);
        
        Image btnImg = nextButton.AddComponent<Image>();
        Button btn = nextButton.AddComponent<Button>();

        // 4. Create Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(nextButton.transform, false);
        
        Text txt = textObj.AddComponent<Text>();
        txt.text = "Next Floor";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 24;
        txt.color = Color.black;
        // Try to load standard font
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        RectTransform textRT = txt.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // 5. Try to Link to GameUIManager if possible
        // This part is hard because we can't easily serialize the persistent listener in code without serialized object
        // So we will just tell the user the structure is made.
        
        // Hide initially
        resultScreen.SetActive(false);
        
        Debug.Log("ResultScreen Created Successfully!");
        
        // Register Undo so user can Ctrl+Z if they hate it
        Undo.RegisterCreatedObjectUndo(resultScreen, "Create Result Screen");
    }
}
