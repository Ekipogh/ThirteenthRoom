using UnityEngine;

public class Landing : DynamicDoorRoom
{
    [Header("Landing Specific")]
    [SerializeField] GameObject leftStaircase;
    [SerializeField] GameObject leftStairsCeiling;
    [SerializeField] GameObject rightStaircase;
    [SerializeField] GameObject rightStairsCeiling;
    [SerializeField] GameObject floorsParent;

    [SerializeField] PointNode upDoorPointLeft;
    [SerializeField] PointNode upDoorPointRight;
    [SerializeField] PointNode downDoorPointLeft;
    [SerializeField] PointNode downDoorPointRight;

    [SerializeField] private int floor = 0;

    protected override void Start()
    {
        base.Start();
        ManageStaircaseVisibility();
    }

    public void UpdateDoorsAndStairs()
    {
        UpdateDynamicDoors();
        ManageStaircaseVisibility();
    }

    private void ManageStaircaseVisibility()
    {
        if (floor > 0)
        {
            if (floorsParent != null) floorsParent.SetActive(false);
        }
        if (Up == null)
        {
            if (leftStaircase != null) leftStaircase.SetActive(false);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(true);
            if (rightStaircase != null) rightStaircase.SetActive(false);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(true);
            upDoorPoint = null;
            downDoorPoint = floor % 2 == 0 ? downDoorPointRight : downDoorPointLeft;
            return;
        }
        if (floor % 2 == 0)
        {
            if (leftStaircase != null) leftStaircase.SetActive(true);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(true);
            if (rightStaircase != null) rightStaircase.SetActive(false);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(false);
            upDoorPoint = upDoorPointLeft;
            downDoorPoint = downDoorPointRight;
        }
        else
        {
            if (leftStaircase != null) leftStaircase.SetActive(false);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(false);
            if (rightStaircase != null) rightStaircase.SetActive(true);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(true);
            upDoorPoint = upDoorPointRight;
            downDoorPoint = downDoorPointLeft;
        }
        if (Down == null)
        {
            downDoorPoint = null;
        }
    }

    public void SetFloor(int floor)
    {
        this.floor = floor;
    }
}
