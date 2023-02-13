using System;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class RakelTransferMapModeDropdownController : MonoBehaviour
{
    OilPaintEngine OilPaintEngine;

    TMP_Dropdown EmitModeDropdown;

    public void Awake()
    {
        OilPaintEngine = GameObject.Find("OilPaintEngine").GetComponent<OilPaintEngine>();

        EmitModeDropdown = GameObject.Find("Rakel Transfer Map Mode Dropdown").GetComponent<TMP_Dropdown>();
        EmitModeDropdown.AddOptions(Enum.GetNames(typeof(TransferMapMode)).ToList());
        EmitModeDropdown.onValueChanged.AddListener(OnValueChanged);
    }

    public void Start()
    {
        EmitModeDropdown.SetValueWithoutNotify((int)OilPaintEngine.Configuration.TransferConfiguration.MapMode);
    }

    public void OnValueChanged(int value)
    {
        OilPaintEngine.UpdateRakelEmitMode((TransferMapMode)value);
    }
}
