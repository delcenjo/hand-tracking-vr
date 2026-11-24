using TMPro;
using UnityEngine;

public class Canasta : MonoBehaviour
{
    public TextMeshPro marcador;
    public Bolas bolas;

    int puntos;

    void Start()
    {
        Pintar();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null || other.name != "Bola") return;
        puntos++;
        Pintar();
        if (bolas != null) bolas.Confeti();
        Destroy(other.attachedRigidbody.gameObject, 0.5f);
    }

    public void Reiniciar()
    {
        puntos = 0;
        Pintar();
    }

    void Pintar()
    {
        if (marcador != null) marcador.text = "Puntos: " + puntos;
    }
}
