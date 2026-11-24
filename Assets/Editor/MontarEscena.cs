using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class MontarEscena
{
    const string escenaFinal = "Assets/Scenes/HandTracking.unity";
    const string escenaSynty = "Assets/Synty/PolygonStarter/Scenes/Demo.unity";
    const string escenaManos = "Assets/Samples/XR Interaction Toolkit/3.7.0/Hands Interaction Demo/HandsDemoScene.unity";
    const string rutaGestos = "Assets/Samples/XR Hands/1.10.0/Gestures/Examples/";
    const string confeti = "Assets/Samples/XR Interaction Toolkit/3.7.0/Starter Assets/DemoAssets/Prefabs/Interactables/Confetti.prefab";
    const string simulador = "Assets/Samples/XR Interaction Toolkit/3.7.0/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("Hand Tracking/Montar escena", priority = 4)]
    public static void Montar()
    {
        RecursosTMP();

        if (File.Exists(escenaFinal)) AssetDatabase.DeleteAsset(escenaFinal);
        AssetDatabase.CopyAsset(escenaSynty, escenaFinal);
        var escena = EditorSceneManager.OpenScene(escenaFinal, OpenSceneMode.Single);

        // La cámara de la demo de Synty mira a la plaza: ahí ponemos la zona de juego
        var camSynty = escena.GetRootGameObjects().First(r => r.GetComponent<Camera>() != null).transform;
        Physics.SyncTransforms();
        var plaza = Physics.Raycast(camSynty.position, camSynty.forward, out var golpe, 200) ? golpe.point : Vector3.zero;
        var mirada = Vector3.ProjectOnPlane(camSynty.forward, Vector3.up).normalized;
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.GetComponent<Camera>() != null) Object.DestroyImmediate(raiz);

        // Mesa, jugador y gestores de la demo de manos de XRI
        var demo = EditorSceneManager.OpenScene(escenaManos, OpenSceneMode.Additive);
        var traidos = new List<GameObject>();
        foreach (var raiz in demo.GetRootGameObjects())
        {
            if (raiz.name == "Environment" || raiz.name == "Tutorial Player" || raiz.GetComponent<Light>() != null) continue;
            SceneManager.MoveGameObjectToScene(raiz, escena);
            traidos.Add(raiz);
        }
        EditorSceneManager.CloseScene(demo, true);

        var sitio = BuscarSitio(traidos, plaza, out float suelo);
        var giro = Quaternion.LookRotation(mirada);
        var centro = new Vector3(sitio.x, suelo, sitio.z);

        foreach (var go in traidos)
        {
            go.transform.SetPositionAndRotation(centro + giro * go.transform.position, giro * go.transform.rotation);
        }
        var origen = traidos.First(g => g.name.StartsWith("XR Origin"));
        origen.transform.SetPositionAndRotation(centro + giro * new Vector3(0, 0, -0.55f), giro);
        var cabeza = origen.GetComponentInChildren<Camera>(true).transform;

        // Teletransporte por todo el suelo del pueblo
        foreach (var col in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (col.name.Contains("Ground") && col.GetComponent<TeleportationArea>() == null)
                col.gameObject.AddComponent<TeleportationArea>();

        var mat = Material("Bola", new Color(1f, 0.5f, 0.2f));

        var juego = new GameObject("Juego");
        var salida = new GameObject("Salida bolas").transform;
        salida.SetParent(juego.transform);
        salida.position = centro + giro * new Vector3(0, 1.2f, 0.45f);
        var bolas = juego.AddComponent<Bolas>();
        bolas.salida = salida;
        bolas.material = mat;
        bolas.confeti = AssetDatabase.LoadAssetAtPath<GameObject>(confeti);

        CrearCanasta(juego.transform, centro + giro * new Vector3(1.4f, 0, 0.5f), giro, bolas);
        CrearCartel(juego.transform, centro + giro * new Vector3(0, 1.85f, 1.6f), giro);
        CrearGestos(juego.transform, origen, cabeza, bolas);

        var sim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(simulador), escena);
        sim.SetActive(false);

        EditorSceneManager.SaveScene(escena);
        var lista = EditorBuildSettings.scenes.Where(s => s.path != escenaFinal).ToList();
        lista.Insert(0, new EditorBuildSettingsScene(escenaFinal, true));
        EditorBuildSettings.scenes = lista.ToArray();
        Debug.Log($"Escena montada en {sitio} (suelo {suelo:0.00})");
    }

    [MenuItem("Hand Tracking/Activar o quitar simulador", priority = 5)]
    public static void Simulador()
    {
        var sim = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name.StartsWith("XR Device Simulator"));
        if (sim == null) return;
        sim.SetActive(!sim.activeSelf);
        EditorSceneManager.MarkSceneDirty(sim.scene);
        Debug.Log("Simulador " + (sim.activeSelf ? "activado" : "desactivado"));
    }

    static void RecursosTMP()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
        AssetDatabase.ImportPackage(Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"), false);
        AssetDatabase.Refresh();
    }

    // Busca una zona plana del pueblo sin edificios encima
    static Vector3 BuscarSitio(List<GameObject> ignorar, Vector3 plaza, out float suelo)
    {
        Physics.SyncTransforms();
        var raices = new HashSet<Transform>(ignorar.Select(g => g.transform));
        var objetos = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r => !raices.Contains(r.transform.root))
            .Select(r => r.bounds)
            .Where(b => b.size.x < 15 && b.size.z < 15)
            .ToList();
        int sinSuelo = 0, desnivel = 0, choque = 0, tapado = 0;
        const float radio = 2.2f;

        var mejor = plaza;
        float mejorNota = float.MinValue;
        suelo = plaza.y;
        for (int x = -44; x <= 44; x++)
        for (int z = -34; z <= 70; z++)
        {
            var alturas = new List<float>();
            bool valido = true;
            for (float i = -radio; i <= radio && valido; i += radio / 2)
            for (float j = -radio; j <= radio && valido; j += radio / 2)
            {
                var o = new Vector3(x + i, 40, z + j);
                if (!Physics.Raycast(o, Vector3.down, out var hit, 80, ~0, QueryTriggerInteraction.Ignore) ||
                    raices.Contains(hit.collider.transform.root))
                    valido = false;
                else
                    alturas.Add(hit.point.y);
            }
            if (!valido) { sinSuelo++; continue; }
            if (alturas.Max() - alturas.Min() > 0.15f) { desnivel++; continue; }

            float y = alturas.Max();
            if (Physics.OverlapBox(new Vector3(x, y + 1.4f, z), new Vector3(radio, 1f, radio), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => !raices.Contains(c.transform.root))) { choque++; continue; }
            if (objetos.Any(b => b.max.y > y + 0.3f && b.min.y < y + 2.4f &&
                                 b.max.x > x - radio && b.min.x < x + radio && b.max.z > z - radio && b.min.z < z + radio)) { tapado++; continue; }

            float nota = -Vector2.Distance(new Vector2(plaza.x, plaza.z), new Vector2(x, z));
            if (nota > mejorNota)
            {
                mejorNota = nota;
                mejor = new Vector3(x, y, z);
                suelo = y;
            }
        }
        Debug.Log($"Busqueda: plaza {plaza}, sin suelo {sinSuelo}, desnivel {desnivel}, choque {choque}, tapado {tapado}, nota {mejorNota}");
        return mejor;
    }

    static Material Material(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
        var ruta = $"Assets/Materiales/{nombre}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CrearCanasta(Transform padre, Vector3 pos, Quaternion giro, Bolas bolas)
    {
        var canasta = new GameObject("Canasta");
        canasta.transform.SetParent(padre);
        canasta.transform.SetPositionAndRotation(pos, giro);
        var mat = Material("Canasta", new Color(0.25f, 0.55f, 0.9f));

        void Pieza(string nombre, Vector3 centro, Vector3 tam)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = nombre;
            p.transform.SetParent(canasta.transform, false);
            p.transform.localPosition = centro;
            p.transform.localScale = tam;
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }
        Pieza("Base", new Vector3(0, 0.025f, 0), new Vector3(0.7f, 0.05f, 0.7f));
        Pieza("Pared 1", new Vector3(0, 0.3f, 0.33f), new Vector3(0.7f, 0.55f, 0.04f));
        Pieza("Pared 2", new Vector3(0, 0.3f, -0.33f), new Vector3(0.7f, 0.55f, 0.04f));
        Pieza("Pared 3", new Vector3(0.33f, 0.3f, 0), new Vector3(0.04f, 0.55f, 0.7f));
        Pieza("Pared 4", new Vector3(-0.33f, 0.3f, 0), new Vector3(0.04f, 0.55f, 0.7f));

        var zona = new GameObject("Zona");
        zona.transform.SetParent(canasta.transform, false);
        zona.transform.localPosition = new Vector3(0, 0.25f, 0);
        var trigger = zona.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.6f, 0.4f, 0.6f);

        var texto = Texto(canasta.transform, "Marcador", "Puntos: 0", 1.2f);
        texto.transform.parent.localPosition = new Vector3(0, 1.0f, 0);

        var c = zona.AddComponent<Canasta>();
        c.marcador = texto;
        c.bolas = bolas;
    }

    static void CrearCartel(Transform padre, Vector3 pos, Quaternion giro)
    {
        var t = Texto(padre, "Cartel",
            "Pulgar arriba: bola nueva\nPulgar abajo: quitar bolas\nShaka: confeti\n\nCoge las bolas y mételas en la canasta", 0.9f);
        t.transform.parent.SetPositionAndRotation(pos, giro);
    }

    // El texto va dentro de un objeto normal: es el que se coloca
    static TextMeshPro Texto(Transform padre, string nombre, string contenido, float tam)
    {
        var contenedor = new GameObject(nombre);
        contenedor.transform.SetParent(padre, false);
        var go = new GameObject("Texto");
        go.transform.SetParent(contenedor.transform, false);
        var t = go.AddComponent<TextMeshPro>();
        t.text = contenido;
        t.fontSize = tam;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(1.6f, 0.8f);
        return t;
    }

    static void CrearGestos(Transform padre, GameObject origen, Transform cabeza, Bolas bolas)
    {
        var gestos = new GameObject("Gestos");
        gestos.transform.SetParent(padre);
        var manos = new List<XRHandTrackingEvents>();
        foreach (var lado in new[] { Handedness.Left, Handedness.Right })
        {
            var go = new GameObject(lado == Handedness.Left ? "Mano izquierda" : "Mano derecha");
            go.transform.SetParent(gestos.transform);
            var m = go.AddComponent<XRHandTrackingEvents>();
            m.handedness = lado;
            manos.Add(m);
        }

        void Gesto(XRHandTrackingEvents mano, string asset, UnityAction accion)
        {
            var g = mano.gameObject.AddComponent<GestoMano>();
            g.mano = mano;
            g.cabeza = cabeza;
            g.gesto = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rutaGestos + asset);
            g.alHacerGesto = new UnityEvent();
            UnityEventTools.AddPersistentListener(g.alHacerGesto, accion);
        }

        foreach (var mano in manos)
        {
            Gesto(mano, "Hand Poses/Thumbs Up.asset", bolas.CrearBola);
            Gesto(mano, "Hand Poses/Thumbs Down.asset", bolas.QuitarBolas);
            Gesto(mano, "Hand Shapes/Shaka Hand Shape.asset", bolas.Confeti);
        }
        Debug.Log("Gestos en " + manos.Count + " manos");
    }
}
