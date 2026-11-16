
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugador;
    private Camera camara;

    [Header("Seguimiento")]
    [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 0f, -10f);
    [SerializeField] private float velocidadSeguimiento = 5f;

    [Header("Configuracion de camara")]
    [SerializeField] private float tamano2D = 5f;
    [SerializeField] private float campoVision3D = 60f;

    private bool modo2D = true;
    private Vector3 velocidadActual = Vector3.zero;

    private void Start()
    {
        camara = GetComponent<Camera>();

        ConfigurarModo2D();
    }

    private void Update()
    {
        // Alternar entre 2D y 3D con P
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            modo2D = !modo2D;

            if (modo2D)
                ConfigurarModo2D();
            else
                ConfigurarModo3D();
        }
    }

    private void LateUpdate()
    {
        if (jugador == null) return;

        // Seguir al jugador
        Vector3 posicionObjetivo = jugador.position + desplazamiento;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionObjetivo,
            ref velocidadActual,
            1f / velocidadSeguimiento
        );
    }

    private void ConfigurarModo2D()
    {
        camara.orthographic = true;
        camara.orthographicSize = tamano2D;

        // Vista Front
        transform.rotation = Quaternion.identity;
    }

    private void ConfigurarModo3D()
    {
        camara.orthographic = false;
        camara.fieldOfView = campoVision3D;

        // Mantener vista Front
        transform.rotation = Quaternion.identity;
    }
}
