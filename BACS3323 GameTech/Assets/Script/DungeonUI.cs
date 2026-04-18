using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RoomData;

public class DungeonUI : MonoBehaviour
{
    public DungeonController controller;

    public TMP_InputField sizeInput;
    public TMP_InputField seedInput;
    public Slider difficultySlider;
    public Toggle branchToggle;
    public TMP_InputField cellSpacingInput;

    public TMP_Dropdown combatInput;
    public TMP_Dropdown treasureInput;
    public TMP_Dropdown trapInput;

    public TMP_Text debugText;

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


        controller.maxCombat = combatInput.value;

        controller.maxTreasure = treasureInput.value;

        controller.maxTrap = trapInput.value;


        controller.RunGeneration();

        debugText.text =
            $"Size: {controller.size}\n" +
            $"Seed: {controller.seed}\n" +
            $"Difficulty: {controller.difficulty:F2}";
    }

    public void OnRandomSeed()
    {
        int s = Random.Range(0, 99999);
        seedInput.text = s.ToString();

        OnGenerateClicked();
    }

    
}