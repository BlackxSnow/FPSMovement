using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public static bool IsPaused;

    public GameObject InputGroupPrefab;
    public GameObject VariableInputPrefab;
    public GameObject BooleanInputPrefab;

    private Transform VariableInputGrid;
    private Transform Instructions;
    private Transform Changelog;
    private TMPro.TextMeshProUGUI ChangelogText;

    private GridPanelGroup CurrentGroup;

    [Header("HUD")]
    [SerializeField]
    private TMPro.TextMeshProUGUI SpeedText;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        VariableInputGrid = transform.Find("VarInputGrid");
        Instructions = transform.Find("Instructions");
        Changelog = transform.Find("Changelog");
        ChangelogText = Changelog.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
    }

    private void Start()
    {
        PlayerMovement.PlayerInput.Player.Pause.performed += TogglePause;
        PlayerMovement.PlayerInput.Player.Instructions.performed += ToggleInstructions;
        PlayerMovement.PlayerInput.Player.Changelog.performed += ToggleChangelog;
        SetChangelogText();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Vector3 playerVelocity = PlayerMovement.Player.GetVelocity();
        SpeedText.text = $"Total Speed: {Mathf.Round(playerVelocity.magnitude* 10) / 10}\n" +
            $"Horizontal Speed: {Mathf.Round(new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude * 10) / 10}";
    }

    public void Exit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Set player back to intitial position and rotation
    /// </summary>
    public void ResetPlayer()
    {
        PlayerMovement.Player.transform.position = new Vector3(0, 1, 0);
        PlayerMovement.Player.transform.rotation = Quaternion.identity;
        PlayerMovement.Player.HeadCamera.transform.localRotation = Quaternion.identity;
    }

    private bool HasNotShownPause = true;
    /// <summary>
    /// Set pause state to the opposite of its current value
    /// </summary>
    /// <param name="context"></param>
    private void TogglePause(CallbackContext context)
    {
        IsPaused = !IsPaused;
        Cursor.lockState = IsPaused ? CursorLockMode.Confined : CursorLockMode.Locked;
        VariableInputGrid.gameObject.SetActive(IsPaused);
        if(HasNotShownPause)
        {
            SetDirty();
        }
    }

    /// <summary>
    /// Set visibility of instructions to the opposite of its current value
    /// </summary>
    /// <param name="c"></param>
    public void ToggleInstructions(CallbackContext c)
    {
        Instructions.gameObject.SetActive(!Instructions.gameObject.activeSelf);
    }


    public void ToggleChangelog(CallbackContext c)
    {
        ToggleChangelog();
    }
    /// <summary>
    /// Set visibility of the changelog to the opposite of its current value
    /// </summary>
    /// <param name="c"></param>
    public void ToggleChangelog()
    {
        Changelog.gameObject.SetActive(!Changelog.gameObject.activeSelf);
    }
    
    /// <summary>
    /// Initialise changelog text from file
    /// </summary>
    private void SetChangelogText()
    {
        ChangelogText.text = Resources.Load<TextAsset>("Changelog").text;
        ChangelogText.text = ChangelogText.text.Replace("\\t", "\t");
    }


    /// <summary>
    /// Rebuild the UI
    /// </summary>
    public void SetDirty()
    {
        RectTransform varRect = VariableInputGrid.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(varRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(varRect);
    }

    /// <summary>
    /// Create a new visual grouping of variable modifier panels. All following panels are placed in this group until this is called again
    /// </summary>
    /// <param name="title"></param>
    public void CreatePanelGroup(string title)
    {
        GameObject obj = Instantiate(InputGroupPrefab, VariableInputGrid);
        CurrentGroup = obj.GetComponent<GridPanelGroup>();
        CurrentGroup.Title.text = title;
    }

    /// <summary>
    /// Create a new variable input panel
    /// </summary>
    /// <typeparam name="VarType">Type inteded to be given to UI display</typeparam>
    /// <typeparam name="ReturnType">Type intended to be received from user input</typeparam>
    /// <param name="getter">Gets the value of the variable for display</param>
    /// <param name="setter">Sets the value of the variable from input</param>
    /// <param name="name">Display name of the variable</param>
    /// <param name="summary">Short description of the variable</param>
    public void CreateVariableInput<VarType, ReturnType>(Func<ReturnType> getter, Action<ReturnType> setter, string name, string summary)
    {
        switch(setter)
        {
            case Action<string> action:
                CreateNumericalTextInput<VarType>(getter as Func<string>, action, name, summary);
                break;
            case Action<bool> action:
                CreateBooleanInput(getter as Func<bool>, action, name, summary);
                break;
        }
    }

    /// <summary>
    /// Wrapper for creating an input with a textbox intended to hold numeric types
    /// </summary>
    /// <typeparam name="VarType"></typeparam>
    /// <param name="getter"></param>
    /// <param name="setter"></param>
    /// <param name="name"></param>
    /// <param name="summary"></param>
    private void CreateNumericalTextInput<VarType>(Func<string> getter, Action<string> setter, string name, string summary)
    {
        GameObject varInput = Instantiate(VariableInputPrefab, CurrentGroup.Content);
        NumericalInputPanel script = varInput.GetComponent<NumericalInputPanel>();
        script.GetVariable = getter;
        script.SetVariable = setter;
        script.VariableName.text = name;
        script.VariableSummary.text = summary;

        if (typeof(VarType) == typeof(int))
        {
            script.VariableInput.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
        }
        else
        {
            script.VariableInput.contentType = TMPro.TMP_InputField.ContentType.DecimalNumber;
        }
    }

    /// <summary>
    /// Wrapper for creating an input with a checkbox
    /// </summary>
    /// <param name="getter"></param>
    /// <param name="setter"></param>
    /// <param name="name"></param>
    /// <param name="summary"></param>
    private void CreateBooleanInput(Func<bool> getter, Action<bool> setter, string name, string summary)
    {
        GameObject varInput = Instantiate(BooleanInputPrefab, CurrentGroup.Content);
        BooleanInputPanel script = varInput.GetComponent<BooleanInputPanel>();
        script.GetVariable = getter;
        script.SetVariable = setter;
        script.VariableName.text = name;
        script.VariableSummary.text = summary;
    }
}
