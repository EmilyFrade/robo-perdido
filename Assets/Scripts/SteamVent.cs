using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Jato de vapor escaldante (Setor Caldeiras) — perigo TEMPORIZADO que ensina janelas de
    /// tempo. Alterna entre seguro e ativo; quando ativo, drena bateria de quem estiver dentro.
    /// O vapor é uma FUMAÇA real (ParticleSystem): emite enquanto o jato está aberto e dissipa
    /// sozinho quando fecha.
    /// </summary>
    public class SteamVent : MonoBehaviour
    {
        public float safeTime = 3.0f;
        public float ventTime = 2.0f;
        public float phase = 0f;
        public float damagePerSecond = 9f;
        public ParticleSystem steam;
        public Light steamLight;
        public AudioSource hiss;   // som de fumaça saindo (3D)

        bool venting;
        float t;

        void Start()
        {
            t = phase;
            if (steam != null) steam.Play();
        }

        void Update()
        {
            t += Time.deltaTime;
            float cycle = Mathf.Max(0.1f, safeTime + ventTime);
            venting = Mathf.Repeat(t, cycle) >= safeTime;

            if (steam != null)
            {
                ParticleSystem.EmissionModule em = steam.emission;
                em.rateOverTime = venting ? 45f : 0f;   // ao fechar, a fumaça já emitida sobe e some
            }
            if (steamLight != null)
                steamLight.intensity = Mathf.Lerp(steamLight.intensity, venting ? 1.3f : 0.05f, Time.deltaTime * 5f);
            if (hiss != null)
                hiss.volume = Mathf.Lerp(hiss.volume, venting ? 0.6f : 0f, Time.deltaTime * 5f);
        }

        void OnTriggerStay(Collider other)
        {
            if (!venting) return;
            BatterySystem b = other.GetComponentInParent<BatterySystem>();
            if (b == null) return;

            b.Drain(damagePerSecond * Time.deltaTime);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FlagHazard("Vapor escaldante! Espere o jato fechar");
                GameManager.Instance.SetDeathCause("você foi atingido pelo vapor das caldeiras.");
            }
        }
    }
}
