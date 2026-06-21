using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Camera em terceira pessoa proxima e baixa (dentro do ambiente). Faz um raycast do robo
    /// ate a posicao desejada da camera: se uma parede/teto fica no meio, puxa a camera para
    /// frente do obstaculo para o M-37 nunca ficar escondido. Ignora o proprio robo e gatilhos.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2.8f, -4.2f);
        public float smooth = 8f;

        [Header("Colisao da camera")]
        public float collisionBuffer = 0.3f; // folga para nao colar na parede
        public float minDistance = 1.0f;      // distancia minima ao robo quando espremida

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 pivot = target.position + Vector3.up * 0.9f; // ponto que a camera olha
            Vector3 desired = target.position + offset;
            Vector3 dir = desired - pivot;
            float dist = dir.magnitude;

            if (dist > 0.001f)
            {
                Vector3 dirN = dir / dist;
                float closest = dist;

                // RaycastAll para poder ignorar o proprio robo; gatilhos ja sao ignorados.
                RaycastHit[] hits = Physics.RaycastAll(pivot, dirN, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
                    if (hit.distance < closest) closest = hit.distance;
                }

                float finalDist = Mathf.Max(minDistance, closest - collisionBuffer);
                desired = pivot + dirN * finalDist;

                // Suaviza, mas garante que nunca fique ALEM da parede (sem clipping nem entrar Lerp).
                Vector3 smoothed = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
                Vector3 d2 = smoothed - pivot;
                if (d2.magnitude > finalDist) smoothed = pivot + d2.normalized * finalDist;
                transform.position = smoothed;
            }
            else
            {
                transform.position = desired;
            }

            transform.LookAt(pivot);
        }
    }
}
