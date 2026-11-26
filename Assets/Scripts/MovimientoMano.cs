using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class MovimientoMano : MonoBehaviour
{
    public XRHandTrackingEvents mano;
    public XROrigin origen;
    public TeleportationProvider teleport;
    public NearFarInteractor interactor;
    public Material material;
    public float giro = 45f;
    public float fuerza = 8f;

    readonly Vector3[] arco = new Vector3[40];
    readonly RaycastHit[] golpes = new RaycastHit[8];
    LineRenderer linea;
    Transform marca;
    MaterialPropertyBlock color;
    bool pellizco, bloqueado, apuntando, valido;
    float inicio;
    Vector3 direccion, destino;

    void Start()
    {
        linea = gameObject.AddComponent<LineRenderer>();
        linea.sharedMaterial = material;
        linea.widthMultiplier = 0.012f;
        linea.numCapVertices = 4;
        linea.positionCount = 0;
        color = new MaterialPropertyBlock();

        marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        marca.name = "Marca teletransporte";
        Destroy(marca.GetComponent<Collider>());
        marca.localScale = new Vector3(0.35f, 0.005f, 0.35f);
        marca.GetComponent<Renderer>().sharedMaterial = material;
        marca.gameObject.SetActive(false);
    }

    void OnEnable() => mano.jointsUpdated.AddListener(Actualizar);
    void OnDisable() => mano.jointsUpdated.RemoveListener(Actualizar);

    void Update()
    {
        if (!mano.handIsTracked && (pellizco || apuntando))
        {
            pellizco = bloqueado = false;
            Cancelar();
        }
    }

    void Actualizar(XRHandJointsUpdatedEventArgs args)
    {
        if (!args.hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var pulgar) ||
            !args.hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indice) ||
            !args.hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var palma))
            return;

        var espacio = origen.CameraFloorOffsetObject.transform;
        var punta = espacio.TransformPoint((pulgar.position + indice.position) / 2);
        var normalPalma = espacio.TransformDirection(palma.rotation * Vector3.down);
        float separacion = Vector3.Distance(pulgar.position, indice.position);
        bool ahora = separacion < (pellizco ? 0.035f : 0.015f);

        if (ahora && !pellizco)
        {
            inicio = Time.time;
            bloqueado = Ocupada();
            if (!bloqueado && Vector3.Dot(normalPalma, Vector3.up) > 0.7f)
            {
                origen.RotateAroundCameraUsingOriginUp(mano.handedness == Handedness.Left ? -giro : giro);
                bloqueado = true;
            }
        }
        else if (!ahora && pellizco)
        {
            if (apuntando && valido)
                teleport.QueueTeleportRequest(new TeleportRequest
                {
                    destinationPosition = destino,
                    matchOrientation = MatchOrientation.None,
                    requestTime = Time.time,
                });
            bloqueado = false;
            Cancelar();
        }
        pellizco = ahora;

        if (!pellizco || bloqueado) return;
        if (Ocupada())
        {
            bloqueado = true;
            Cancelar();
        }
        else if (Time.time - inicio > 0.2f)
        {
            Apuntar(punta);
        }
    }

    bool Ocupada() => interactor != null && (interactor.hasSelection || interactor.interactablesHovered.Count > 0);

    void Apuntar(Vector3 punta)
    {
        var cabeza = origen.Camera.transform;
        var delante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
        var lado = Vector3.Cross(Vector3.up, delante) * (mano.handedness == Handedness.Left ? -0.15f : 0.15f);
        var hombro = cabeza.position + Vector3.down * 0.2f + lado;
        var nueva = (punta - hombro).normalized;
        direccion = apuntando ? Vector3.Slerp(direccion, nueva, 0.3f) : nueva;
        apuntando = true;

        var p = punta;
        var v = direccion * fuerza;
        const float paso = 0.03f;
        int n = 1;
        arco[0] = p;
        valido = false;
        for (; n < arco.Length; n++)
        {
            var siguiente = p + v * paso;
            v += Physics.gravity * paso;
            if (Golpe(p, siguiente, out var hit))
            {
                arco[n++] = hit.point;
                destino = hit.point;
                valido = hit.normal.y > 0.7f && hit.collider.GetComponentInParent<TeleportationArea>() != null;
                break;
            }
            arco[n] = siguiente;
            p = siguiente;
        }

        linea.positionCount = n;
        linea.SetPositions(arco);
        color.SetColor("_BaseColor", valido ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.35f, 0.3f));
        linea.SetPropertyBlock(color);
        marca.gameObject.SetActive(valido);
        if (valido) marca.position = destino + Vector3.up * 0.01f;
    }

    bool Golpe(Vector3 a, Vector3 b, out RaycastHit mejor)
    {
        mejor = default;
        var d = b - a;
        int total = Physics.RaycastNonAlloc(a, d.normalized, golpes, d.magnitude, ~0, QueryTriggerInteraction.Ignore);
        float minimo = float.MaxValue;
        for (int i = 0; i < total; i++)
        {
            if (golpes[i].transform.IsChildOf(origen.transform) || golpes[i].distance >= minimo) continue;
            minimo = golpes[i].distance;
            mejor = golpes[i];
        }
        return minimo < float.MaxValue;
    }

    void Cancelar()
    {
        apuntando = valido = false;
        if (linea != null) linea.positionCount = 0;
        if (marca != null) marca.gameObject.SetActive(false);
    }
}
