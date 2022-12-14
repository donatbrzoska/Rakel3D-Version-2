using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class RakelRotationLockedController : MonoBehaviour
{
    OilPaintEngine OilPaintEngine;
    Toggle Toggle;

    public void Awake()
    {
        OilPaintEngine = GameObject.Find("OilPaintEngine").GetComponent<OilPaintEngine>();
        Toggle = GetComponent<Toggle>();
        Toggle.onValueChanged.AddListener(OnValueChanged);
    }

    // Start is called before the first frame update
    void Start()
    {
        Toggle.SetIsOnWithoutNotify(OilPaintEngine.RakelRotationLocked);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnValueChanged(bool locked)
    {
        OilPaintEngine.UpdateRakelRotationLocked(locked);
    }
}
