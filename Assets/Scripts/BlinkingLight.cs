using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Emissão pulsante para detalhes de arte: LEDs de racks de servidor, painéis com falha e
    /// hologramas tremeluzentes. Apenas cosmético — anima a cor de emissão do material.
    /// </summary>
    public class BlinkingLight : MonoBehaviour
    {
        public Color color = GameColors.Ambar;
        public float speed = 2.5f;
        public float min = 0.15f;
        public float max = 1.7f;

        Renderer rend;
        float phase;

        void Start()
        {
            rend = GetComponent<Renderer>();
            phase = Random.value * 10f;
            if (rend != null) rend.material.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            if (rend == null) return;
            float e = Mathf.Lerp(min, max, Mathf.Sin((Time.time + phase) * speed) * 0.5f + 0.5f);
            rend.material.SetColor("_EmissionColor", color * e);
        }
    }
}
