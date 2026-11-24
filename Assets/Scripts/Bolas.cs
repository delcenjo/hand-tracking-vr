using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Bolas : MonoBehaviour
{
    public Transform salida;
    public Material material;
    public GameObject confeti;
    public int maximo = 15;

    readonly List<GameObject> bolas = new List<GameObject>();

    public void CrearBola()
    {
        bolas.RemoveAll(b => b == null);
        if (bolas.Count >= maximo)
        {
            Destroy(bolas[0]);
            bolas.RemoveAt(0);
        }

        var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bola.name = "Bola";
        bola.transform.position = salida.position + Random.insideUnitSphere * 0.05f;
        bola.transform.localScale = Vector3.one * 0.1f;

        var render = bola.GetComponent<Renderer>();
        render.material = material;
        render.material.color = Color.HSVToRGB(Random.value, 0.7f, 1f);

        var rb = bola.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var grab = bola.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = true;
        grab.useDynamicAttach = true;

        bolas.Add(bola);
    }

    public void QuitarBolas()
    {
        foreach (var b in bolas)
            if (b != null) Destroy(b);
        bolas.Clear();
    }

    public void Confeti()
    {
        if (confeti == null) return;
        var c = Instantiate(confeti, salida.position + Vector3.up * 0.3f, Quaternion.identity);
        foreach (var p in c.GetComponentsInChildren<ParticleSystem>()) p.Play();
        Destroy(c, 5f);
    }
}
