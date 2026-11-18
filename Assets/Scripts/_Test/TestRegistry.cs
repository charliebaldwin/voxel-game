using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Reflection;

public class TestRegistry : Sirenix.OdinInspector.SerializedMonoBehaviour
{
    public Dictionary<ItemID, string> MyDictionary = new Dictionary<ItemID, string>();
    public ItemID id;

}

public class AddKeyValueResolver : OdinAttributeProcessor<TempKeyValuePair<ItemID, string>>
{
    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
    {
        if (member.Name == "Key")
        {
            attributes.Add(new EnumToggleButtonsAttribute());
        }
        else if (member.Name == "Value")
        {
            attributes.Add(new ProgressBarAttribute(0, 10));
        }
    }
}

public class EditKeyValueResolver : OdinAttributeProcessor<EditableKeyValuePair<ItemID, string>>
{
    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
    {
        if (member.Name == "Key")
        {
            attributes.Add(new DisplayAsStringAttribute());
        }
        else if (member.Name == "Value")
        {
            attributes.Add(new DisplayAsStringAttribute());
        }
    }
}
