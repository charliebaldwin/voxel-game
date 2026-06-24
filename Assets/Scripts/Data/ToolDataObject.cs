using UnityEngine;

[CreateAssetMenu(fileName = "ToolDataObject", menuName = "Scriptable Objects/ToolDataObject")]
public class ToolDataObject : ItemDataObject
{
    public new ToolData Data = new ToolData();
}
