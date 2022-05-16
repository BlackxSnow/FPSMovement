using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Input panel with textbox intended to hold numeric values
/// </summary>
public class NumericalInputPanel : MonoBehaviour
{
    public TextMeshProUGUI VariableName;
    public TextMeshProUGUI VariableSummary;
    public TMP_InputField VariableInput;

    public Action<string> SetVariable;
    public Func<string> GetVariable;

    private void Update()
    {
        if(!VariableInput.isFocused && GetVariable != null)
        {
            string currentValue = GetVariable();
            if (VariableInput.text != currentValue)
            {
                VariableInput.text = currentValue;
            }
        }
    }

    public void UpdateVariable(string value)
    {
        SetVariable(value);
    }
}
