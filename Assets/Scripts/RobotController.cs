using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Controle do M-37 e o coracao do CORE LOOP: cada acao (mover, correr, interagir)
    /// gasta bateria em ordem crescente de custo, exatamente como definido na tabela de
    /// mecanicas da Etapa 6. Agachar deixa o robo lento, porem silencioso.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(BatterySystem))]
    public class RobotController : MonoBehaviour
    {
        [Header("Movimento")]
        public float walkSpeed = 3.0f;
        public float runSpeed = 5.3f;
        public float crouchSpeed = 1.7f;
        public float gravity = 20f;
        public float rotSpeed = 12f;

        [Header("Custo de bateria por segundo (ordem crescente - Etapa 6)")]
        public float idleCost = 0.05f;
        public float walkCost = 1.2f;
        public float runCost = 3.0f;
        public float crouchCost = 0.7f;
        public float interactCost = 3.0f;    // custo unico ao interagir com um puzzle

        // Estados lidos pelo drone e pela HUD.
        public bool IsNoisy { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsMoving { get; private set; }

        CharacterController cc;
        BatterySystem battery;
        Transform cam;
        float vSpeed;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            battery = GetComponent<BatterySystem>();
        }

        public void SetCamera(Transform c) { cam = c; }

        void Update()
        {
            GameManager gm = GameManager.Instance;

            if (gm != null && !gm.IsPlaying) { GravityOnly(); return; }
            if (battery != null && battery.IsDead)
            {
                GravityOnly();
                if (gm != null) gm.OnBatteryDead();
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 raw = new Vector3(h, 0f, v);
            IsMoving = raw.sqrMagnitude > 0.01f;

            IsCrouching = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
            IsRunning = Input.GetKey(KeyCode.LeftShift) && !IsCrouching && IsMoving;

            // Direcao relativa a camera (terceira pessoa).
            Vector3 dir = Vector3.zero;
            if (IsMoving)
            {
                Vector3 fwd = Vector3.forward, right = Vector3.right;
                if (cam != null)
                {
                    fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
                    right = cam.right; right.y = 0f; right.Normalize();
                }
                dir = (fwd * v + right * h).normalized;
            }

            float speed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);

            if (cc.isGrounded) vSpeed = -1f; else vSpeed -= gravity * Time.deltaTime;

            Vector3 motion = dir * speed;
            motion.y = vSpeed;
            cc.Move(motion * Time.deltaTime);

            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotSpeed * Time.deltaTime);

            IsNoisy = IsRunning && !IsCrouching; // correr faz barulho; agachar silencia

            // ---- Gasto de bateria ----
            if (battery != null)
            {
                float cost = idleCost;
                if (IsMoving)
                    cost = IsCrouching ? crouchCost : (IsRunning ? runCost : walkCost);
                battery.Drain(cost * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        }

        void GravityOnly()
        {
            if (cc.isGrounded) vSpeed = -1f; else vSpeed -= gravity * Time.deltaTime;
            cc.Move(new Vector3(0f, vSpeed, 0f) * Time.deltaTime);
            IsMoving = IsRunning = IsNoisy = false;
        }

        void TryInteract()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            PuzzleStation best = null;
            float bestD = float.MaxValue;
            foreach (PuzzleStation ps in gm.puzzles)
            {
                if (ps == null || ps.solved) continue;
                if (!ps.InRange(transform.position)) continue;
                float d = Vector3.Distance(transform.position, ps.transform.position);
                if (d < bestD) { best = ps; bestD = d; }
            }

            if (best != null)
            {
                if (battery != null) battery.Drain(interactCost * 0.3f); // pequeno custo ao interagir
                best.Interact(gm);
            }
        }
    }
}
