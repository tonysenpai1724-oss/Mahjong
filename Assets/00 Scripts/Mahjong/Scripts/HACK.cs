using MahjongOut3D.Managers;
using TMPro;
using UnityEngine;

public class HACK : MonoBehaviour
{
    //input field is textmeshpro input field, hack panel is the panel that contains the input field and button to load level
    public TMP_InputField inputField;
    public GameObject hackPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //write a function to load level from input field and call level manager to load the level
    public void LoadLevelFromInput()
    {
        if (inputField == null)
        {
            Debug.LogError("Input field is not assigned.");
            return;
        }

        string rawLevelIndex = inputField.text == null ? string.Empty : inputField.text.Trim();
        int levelIndex;
        if (System.Int32.TryParse(rawLevelIndex, out levelIndex))
        {
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.LoadLevel(levelIndex);
            }
            else
            {
                Debug.LogError("LevelManager not found in the scene.");
            }
        }
        else
        {
            Debug.LogError("Invalid level index entered.");
        }
        hackPanel.SetActive(false);
    }
}
