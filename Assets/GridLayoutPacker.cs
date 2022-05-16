using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defines and controls a grid layout based on a maximum column count and minimum cell width
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutPacker : MonoBehaviour
{
    private GridLayoutGroup GridComponent;
    private RectTransform PanelRect;

    public float MinCellWidth = 800;
    public int MaxColumnCount = 2;

    private float LastPanelWidth = -1;

    void Start()
    {
        GridComponent = GetComponent<GridLayoutGroup>();
        PanelRect = GetComponent<RectTransform>();
        GridComponent.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    }

    void Update()
    {
        if(LastPanelWidth != PanelRect.rect.width)
        {
            LastPanelWidth = PanelRect.rect.width;
            int columns = Mathf.Max(Mathf.Min(Mathf.FloorToInt((LastPanelWidth) / MinCellWidth), MaxColumnCount), 1);
            GridComponent.cellSize = new Vector2(LastPanelWidth / columns, GridComponent.cellSize.y);
            GridComponent.constraintCount = columns;
        }
    }
    
}
