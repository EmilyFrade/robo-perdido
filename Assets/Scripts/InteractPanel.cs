using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Painel interativo (Puzzle 2 da demo: conectar um cabo eletrico para abrir a saida).
    /// Mecanica "Interagir/Conectar" da Etapa 6: so funciona dentro do raio, acende em ambar
    /// e move a porta associada. O custo de bateria do uso e debitado pelo RobotController.
    /// </summary>
    public class InteractPanel : MonoBehaviour
    {
        public Transform door;                                   // barreira que abaixa ao ativar
        public Vector3 doorOpenOffset = new Vector3(0f, -3.4f, 0f);
        public float interactRadius = 2.4f;
        public string hint = "Conectar cabo (E)";
        public bool activated;

        Renderer rend;
        Vector3 doorClosedPos;
        bool hasDoor;

        // Em Start (e nao em Awake): o Bootstrap atribui 'door' logo apos o AddComponent,
        // entao Awake rodaria cedo demais, com door == null, e a porta nunca abriria.
        void Start()
        {
            rend = GetComponentInChildren<Renderer>();
            if (door != null) { doorClosedPos = door.position; hasDoor = true; }
        }

        public bool InRange(Vector3 p)
        {
            return Vector3.Distance(p, transform.position) <= interactRadius;
        }

        public void Activate()
        {
            if (activated) return;
            activated = true;
            if (rend != null)
            {
                rend.material.color = GameColors.Ambar;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", GameColors.Ambar * 1.5f);
            }
        }

        void Update()
        {
            if (activated && hasDoor)
            {
                Vector3 target = doorClosedPos + doorOpenOffset;
                door.position = Vector3.MoveTowards(door.position, target, 4f * Time.deltaTime);
            }
        }
    }
}
