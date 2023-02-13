﻿using System;
using System.Collections.Generic;

public class FillColorController : DropdownController
{
    new public void Awake()
    {
        base.Awake();
        //InitializeElements(typeof(_Color));
        List<string> colorNames = new List<string>();
        foreach (_Color c in Enum.GetValues(typeof(_Color)))
        {
            colorNames.Add(Colors.GetName(c));
        }
        Dropdown.AddOptions(colorNames);
    }

    public void Start()
    {
        Dropdown.SetValueWithoutNotify((int)OilPaintEngine.Configuration.FillConfiguration.Color);
    }

    override public void OnValueChanged(int value)
    {
        OilPaintEngine.UpdateFillColor((_Color)value);
    }
}
