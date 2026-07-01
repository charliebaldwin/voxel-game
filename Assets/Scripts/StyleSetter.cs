using Evo.UI;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class StyleSetter : MonoBehaviour
{
    public List<StylerObject> Stylers = new List<StylerObject>();
    public StylerObject ParentStyler;

    private void Awake()
    {
        SetStyle(ParentStyler.Preset);
    }
    public void SetStyle(StylerPreset preset)
    {
        foreach (StylerObject styler in Stylers)
        {
            styler.Preset = preset;
        }
    }
}
