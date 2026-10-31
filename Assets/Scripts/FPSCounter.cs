using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    private Text textComponent;
    private float avg;

    private void Start()
    {
        textComponent = GetComponent<Text>();
    }

    private void Update()
    {
        avg += (Time.deltaTime - avg) * 0.03f;
        float displayValue = 1f / avg;
        textComponent.text = "FPS: " + (int)displayValue;
    }
}
