using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Drone de patrulha (antagonista). Segue uma rota fixa entre waypoints (dinamica
    /// "memorizacao de patrulha" da Etapa 6). Detecta o M-37 por VISAO (cone + linha de visada,
    /// que coberturas bloqueiam) ou por AUDICAO (se o robo estiver correndo por perto).
    /// Ao detectar, drena um dano grande de bateria, com cooldown para nao zerar instantaneo.
    /// O primeiro drone da demo e proposital mente lento (Ajuste 2 do teste em papel).
    /// </summary>
    public class PatrolDrone : MonoBehaviour
    {
        public Vector3[] waypoints;
        public float speed = 2.0f;
        public float visionRange = 7f;
        public float visionAngle = 70f;   // angulo TOTAL do cone
        public float hearRange = 4.5f;
        public float damageOnSpot = 40f;
        public float damageCooldown = 1.5f;

        public bool Alerted { get; private set; }

        int idx;
        float lastHit = -999f;
        Transform player;
        BatterySystem playerBattery;
        RobotController playerCtrl;
        Renderer eyeRend;
        Light eyeLight;

        public void Init(Transform p, BatterySystem b, RobotController c)
        {
            player = p; playerBattery = b; playerCtrl = c;
        }

        void Start()
        {
            eyeRend = GetComponentInChildren<Renderer>();
            eyeLight = GetComponentInChildren<Light>();
        }

        void Update()
        {
            Patrol();
            Detect();
            UpdateVisual();
        }

        void Patrol()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Vector3 target = waypoints[idx];
            target.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            Vector3 flat = target - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 5f * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.2f)
                idx = (idx + 1) % waypoints.Length;
        }

        void Detect()
        {
            Alerted = false;
            if (player == null) return;

            Vector3 to = player.position - transform.position;
            float dist = to.magnitude;

            bool seen = false;
            if (dist <= visionRange && dist > 0.1f)
            {
                float ang = Vector3.Angle(transform.forward, to);
                if (ang <= visionAngle * 0.5f)
                {
                    // Linha de visada: comeca um pouco a frente para nao bater no proprio corpo.
                    Vector3 origin = transform.position + to.normalized * 0.7f + Vector3.up * 0.3f;
                    Vector3 dest = player.position + Vector3.up * 0.4f;
                    if (Physics.Linecast(origin, dest, out RaycastHit hit))
                        seen = hit.collider.GetComponentInParent<RobotController>() != null; // algo no caminho? so ve se for o robo
                    else
                        seen = true; // nada no caminho
                }
            }

            bool heard = dist <= hearRange && playerCtrl != null && playerCtrl.IsNoisy;

            if (seen || heard)
            {
                Alerted = true;
                if (Time.time - lastHit >= damageCooldown && playerBattery != null)
                {
                    playerBattery.Drain(damageOnSpot);
                    lastHit = Time.time;
                    if (GameManager.Instance != null)
                        GameManager.Instance.FlagHazard(seen ? "Drone te detectou! -bateria" : "Drone te ouviu! -bateria");
                }
            }
        }

        void UpdateVisual()
        {
            Color c = Alerted ? Color.red : GameColors.Ambar;
            if (eyeRend != null)
            {
                eyeRend.material.color = c;
                eyeRend.material.EnableKeyword("_EMISSION");
                eyeRend.material.SetColor("_EmissionColor", c * 2f);
            }
            if (eyeLight != null) eyeLight.color = c;
        }
    }
}
