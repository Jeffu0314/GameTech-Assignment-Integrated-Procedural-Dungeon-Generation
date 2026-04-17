using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DungeonUI : MonoBehaviour
{
    public DungeonController controller;

    public TMP_InputField sizeInput;
    public TMP_InputField seedInput;
    public Slider difficultySlider;
    public Toggle branchToggle;
    public TMP_InputField cellSpacingInput;

    public void OnGenerateClicked()
    {
        controller.difficulty = difficultySlider.value;
        controller.enableBranches = branchToggle.isOn;

        if (int.TryParse(sizeInput.text, out int size))
            controller.size = size;

        if (int.TryParse(seedInput.text, out int seed))
            controller.seed = seed;

        if (float.TryParse(cellSpacingInput.text, out float cellSpacing))
            controller.cellSpacing = cellSpacing;

        controller.RunGeneration();
    }
}