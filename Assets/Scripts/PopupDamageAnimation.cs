using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;

public class PopupDamageAnimation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AnimationCurve opacityCurve;
    public AnimationCurve scaleCurve;
    public AnimationCurve heightCurve;
    private Vector3 origin;

    private TextMeshProUGUI tmp;
    private float floatTime = 0;

    private void Awake()
    {
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        origin = transform.position;
    }
    private void Update()
    {
        tmp.color = new Color(1,1,1, opacityCurve.Evaluate(floatTime));
        transform.localScale = Vector3.one * scaleCurve.Evaluate(floatTime);
        transform.position = origin + new Vector3(0, 1 + heightCurve.Evaluate(floatTime), 0);
        floatTime += Time.deltaTime;
    }
}
