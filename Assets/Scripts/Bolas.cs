using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Bolas : MonoBehaviour
{
    public Transform salida;
    public Material material;
    public GameObject confeti;
    public int maximo = 10;
    public float tam = 0.1f;

    readonly List<XRGrabInteractable> bolas = new List<XRGrabInteractable>();
    PhysicsMaterial goma;

    void Start()
    {
        goma = new PhysicsMaterial("Goma")
        {
            bounciness = 0.4f,
            dynamicFriction = 0.8f,
            staticFriction = 0.8f,
            bounceCombine = PhysicsMaterialCombine.Average,
        };
        InvokeRepeating(nameof(Recoger), 2f, 1f);
    }

    public void CrearBola()
    {
        bolas.RemoveAll(b => b == null);
        if (bolas.Count >= maximo)
        {
            var vieja = bolas.Find(b => !b.isSelected);
            if (vieja == null) return;
            bolas.Remove(vieja);
            Destroy(vieja.gameObject);
        }

        var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bola.name = "Bola";
        bola.transform.localScale = Vector3.one * tam;
        bola.transform.position = SitioLibre();
        bola.GetComponent<Collider>().sharedMaterial = goma;

        var render = bola.GetComponent<Renderer>();
        render.material = material;
        render.material.color = Color.HSVToRGB(Random.value, 0.7f, 1f);

        var rb = bola.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.angularDamping = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var grab = bola.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;
        grab.attachEaseInTime = 0.08f;
        grab.farAttachMode = InteractableFarAttachMode.Near;
        grab.throwOnDetach = true;
        grab.throwSmoothingDuration = 0.1f;
        grab.throwVelocityScale = 1.3f;

        bolas.Add(grab);
    }

    public void QuitarBolas()
    {
        foreach (var b in bolas)
            if (b != null) Destroy(b.gameObject);
        bolas.Clear();
    }

    public void Confeti()
    {
        if (confeti == null) return;
        var c = Instantiate(confeti, salida.position + Vector3.up * 0.3f, Quaternion.identity);
        foreach (var p in c.GetComponentsInChildren<ParticleSystem>()) p.Play();
        Destroy(c, 5f);
    }

    Vector3 SitioLibre()
    {
        var pos = salida.position;
        for (int i = 0; i < 8 && Physics.CheckSphere(pos, tam * 0.6f, ~0, QueryTriggerInteraction.Ignore); i++)
            pos += Vector3.up * tam * 1.2f;
        return pos;
    }

    void Recoger()
    {
        foreach (var b in bolas)
        {
            if (b == null || b.isSelected) continue;
            var p = b.transform.position;
            if (p.y > salida.position.y - 0.6f &&Vector3.Distance(p, salida.position) < 20f) continue;

            var rb = b.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = SitioLibre();
        }
    }
}
