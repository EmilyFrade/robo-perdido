using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Porta de sala que desce (abre) quando o puzzle correspondente é resolvido. Ao abrir,
    /// brilha em verde (cor Ácido) e ganha uma luz, sinalizando o caminho liberado.
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(0f, -5.4f, 0f);
        public float speed = 5f;
        public bool open;

        Vector3 closedPos;
        bool started;

        void Start() { closedPos = transform.position; started = true; }

        public void Open()
        {
            if (open) return;
            open = true;

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", GameColors.Acido * 1.3f);
            }
            GameObject g = new GameObject("DoorGlow");
            g.transform.SetParent(transform, false);
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point; l.color = GameColors.Acido; l.range = 6f; l.intensity = 2.2f;

            // som do portão abrindo (3D, na própria porta)
            AudioSource a = gameObject.AddComponent<AudioSource>();
            if (SoundManager.Instance != null) a.clip = SoundManager.Instance.Gate;
            a.spatialBlend = 1f; a.minDistance = 3f; a.maxDistance = 22f; a.volume = 0.85f;
            if (a.clip != null) a.Play();
        }

        void Update()
        {
            if (open && started)
                transform.position = Vector3.MoveTowards(transform.position, closedPos + openOffset, speed * Time.deltaTime);
        }
    }
}
