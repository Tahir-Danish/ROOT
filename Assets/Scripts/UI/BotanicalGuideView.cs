using UnityEngine;
using UnityEngine.UI;

public class BotanicalGuideView : MonoBehaviour
{
    public Text responseAreaText;
    public InputField questionInputField;
    public Button askButton;
    public Button[] quickQuestionButtons;

    void Start()
    {
        if (askButton != null)
        {
            askButton.onClick.AddListener(OnAskClicked);
        }
    }

    public void Populate(SpeciesData data)
    {
        if (responseAreaText != null)
        {
            responseAreaText.text = $"Hello! I'm your Botanical Guide. I can help you learn more about the {data.commonName}. Ask me anything!";
        }
    }

    private void OnAskClicked()
    {
        if (questionInputField != null && !string.IsNullOrEmpty(questionInputField.text))
        {
            if (responseAreaText != null)
            {
                responseAreaText.text = "This is a prototype UI shell. AI response functionality is not yet integrated.\n\nYou asked: " + questionInputField.text;
            }
            questionInputField.text = "";
        }
    }
}
