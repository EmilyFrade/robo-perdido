using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Luz de emergência vermelha pulsante (giroflex) para algumas paredes. Pulsa tanto a
    /// emissão da lente quanto uma luz pontual, dando o clima de alerta da fábrica abandonada.
    /// </summary>
    public class EmergencyLight : MonoBehaviour
    {
        public float speed = 3.2f;
        public Color color = new Color(0.95f, 0.12f, 0.1f);

        Light l;
        Renderer rend;
        float phase;

        void Start()
        {
            l = GetComponentInChildren<Light>();
            rend = GetComponent<Renderer>();
            phase = Random.value * 10f;
            if (rend != null) rend.material.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            float e = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((Time.time + phase) * speed), 3f); // pulso seco
            if (l != null) l.intensity = 0.1f + e * 3.2f;
            if (rend != null) rend.material.SetColor("_EmissionColor", color * (0.25f + e * 2.2f));
        }
    }
}
