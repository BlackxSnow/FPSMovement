using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

/// <summary>
/// Input with checkbox
/// </summary>
public class BooleanInputPanel : MonoBehaviour
{
    public TextMeshProUGUI VariableName;
    public TextMeshProUGUI VariableSummary;
    public Toggle VariableInput;

    public Action<bool> SetVariable;
    public Func<bool> GetVariable;

    private void Start()
    {
        bool currentValue = GetVariable();
        VariableInput.isOn = currentValue;
    }

    public void UpdateVariable(bool value)
    {
        SetVariable(value);
    }
}
