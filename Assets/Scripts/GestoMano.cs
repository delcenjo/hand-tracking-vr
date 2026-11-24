using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

public class GestoMano : MonoBehaviour
{
    public XRHandTrackingEvents mano;
    public ScriptableObject gesto;
    public Transform cabeza;
    public float tiempoMinimo = 0.3f;
    public UnityEvent alHacerGesto;

    XRHandShape forma;
    XRHandPose pose;
    bool detectado;
    bool hecho;
    float inicio;

    void OnEnable()
    {
        forma = gesto as XRHandShape;
        pose = gesto as XRHandPose;
        if (pose != null && pose.relativeOrientation != null)
            pose.relativeOrientation.targetTransform = cabeza;
        if (mano != null)
            mano.jointsUpdated.AddListener(Comprobar);
    }

    void OnDisable()
    {
        if (mano != null)
            mano.jointsUpdated.RemoveListener(Comprobar);
    }

    void Comprobar(XRHandJointsUpdatedEventArgs args)
    {
        bool ahora = mano.handIsTracked &&
                     ((forma != null && forma.CheckConditions(args)) || (pose != null && pose.CheckConditions(args)));

        if (ahora && !detectado) inicio = Time.time;
        if (!ahora) hecho = false;
        detectado = ahora;

        if (ahora && !hecho && Time.time - inicio >= tiempoMinimo)
        {
            hecho = true;
            alHacerGesto.Invoke();
        }
    }
}
