using UnityEngine;

public class TargetMovementDGC : MonoBehaviour
{
    public float verticalSpeed = 5;
    public float horizontalSpeed = 5;
    public float desplazamientoHorizontal = 5;
    public float desplazamientoVertical = 5;
    private Vector3 posicionInicial;
    private float tiempoTranscurrido;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;

        float amplitudHorizontal = Mathf.Max(0, desplazamientoHorizontal);
        float amplitudVertical = Mathf.Max(0, desplazamientoVertical);

        float desplazamientoX = amplitudHorizontal > 0
            ? Mathf.PingPong(tiempoTranscurrido * horizontalSpeed + amplitudHorizontal / 2, amplitudHorizontal) - amplitudHorizontal / 2
            : 0;
        float desplazamientoY = amplitudVertical > 0
            ? Mathf.PingPong(tiempoTranscurrido * verticalSpeed + amplitudVertical / 2, amplitudVertical) - amplitudVertical / 2
            : 0;

        transform.position = posicionInicial + new Vector3(desplazamientoX, desplazamientoY, 0);

    }
}
