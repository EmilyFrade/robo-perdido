using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Cone de visao VISIVEL do drone. Leque de malha no plano horizontal, a frente do drone,
    /// semitransparente e emissivo (ambar na patrulha, vermelho ao detectar). A cada frame os
    /// raios do leque sao recalculados por raycast, de modo que o cone PARA nas paredes/obstaculos
    /// (nao atravessa). Transparencia reduzida pela metade a pedido.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        public float range = 7f;
        public float angle = 70f;   // angulo TOTAL, igual ao do PatrolDrone
        public int segments = 24;
        public float alpha = 0.08f; // 50% mais transparente que antes (era 0.16)

        PatrolDrone drone;
        Material mat;
        Mesh mesh;
        Vector3[] verts;

        public void Init(PatrolDrone d, float range, float angle)
        {
            drone = d;
            this.range = range;
            this.angle = angle;
            BuildMesh();
        }

        void Awake()
        {
            mat = BuildTransparentMaterial(GameColors.Ambar);
            MeshRenderer mr = GetComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void BuildMesh()
        {
            mesh = new Mesh { name = "VisionConeMesh" };
            int n = Mathf.Max(3, segments);
            verts = new Vector3[n + 2];
            int[] tris = new int[n * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            GetComponent<MeshFilter>().mesh = mesh;
        }

        void LateUpdate()
        {
            // Segue o drone (sem ser filho) para nao herdar a escala nao-uniforme do corpo.
            if (drone != null)
            {
                Vector3 p = drone.transform.position; p.y = 0.12f;
                transform.position = p;
                Vector3 f = drone.transform.forward; f.y = 0f;
                if (f.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(f.normalized);
            }

            UpdateConeMesh();

            if (mat == null) return;
            Color c = (drone != null && drone.Alerted) ? new Color(0.9f, 0.2f, 0.15f) : GameColors.Ambar;
            c.a = alpha;
            mat.color = c;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", c * 1.5f);
        }

        // Recalcula o leque com raycast por segmento: cada raio para na 1a parede/obstaculo.
        void UpdateConeMesh()
        {
            if (mesh == null || verts == null) return;
            int n = verts.Length - 2;
            float half = angle * 0.5f;
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Quaternion rot = transform.rotation;

            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(-half, half, (float)i / n) * Mathf.Deg2Rad;
                Vector3 localDir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 worldDir = rot * localDir;
                float dist = range;
                if (Physics.Raycast(origin, worldDir, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
                    dist = hit.distance;
                verts[i + 1] = localDir * dist;   // vertice em espaco local (a malha gira com o transform)
            }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
        }

        static Material BuildTransparentMaterial(Color c)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.EnableKeyword("_EMISSION");
                m.renderQueue = 3000;
            }
            Color col = c; col.a = 0.08f;
            m.color = col;
            return m;
        }
    }
}
