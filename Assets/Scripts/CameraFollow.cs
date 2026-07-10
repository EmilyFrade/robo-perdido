using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Câmera em terceira pessoa que ORBITA o robô conforme o mouse (Etapa 8): mouse X gira a
    /// visão (yaw) e mouse Y inclina (pitch). O cursor fica travado/oculto apenas durante o
    /// jogo (Playing); em menus, pausa e popups de puzzle ele é liberado. Mantém o raycast
    /// anticolisão para a câmera não atravessar paredes.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2.8f, -4.2f); // usado só para distância/altura iniciais
        public float smooth = 12f;

        [Header("Mouse-look")]
        public float mouseSensitivity = 2.6f;
        public float minPitch = -15f;
        public float maxPitch = 65f;

        [Header("Colisao da camera")]
        public float collisionBuffer = 0.3f;
        public float minDistance = 1.0f;

        float yaw;
        float pitch = 18f;
        float distance = 4.6f;
        float height = 1.7f;

        void Start()
        {
            // distância/altura derivadas do offset original.
            distance = new Vector2(offset.x, offset.z).magnitude;
            if (distance < 1f) distance = 4.6f;
            height = offset.y;
            if (target != null) yaw = target.eulerAngles.y;
        }

        bool Playing => GameManager.Instance != null && GameManager.Instance.IsPlaying;

        void LateUpdate()
        {
            if (target == null) return;

            // Cursor: travado só jogando; liberado em menus/pausa/popup.
            if (Playing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * (height * 0.5f);
            Vector3 desired = pivot + rot * new Vector3(0f, height * 0.45f, -distance);

            // Anticolisão: se há parede entre o pivô e a câmera, puxa a câmera para frente.
            Vector3 dir = desired - pivot;
            float dist = dir.magnitude;
            if (dist > 0.001f)
            {
                Vector3 dirN = dir / dist;
                float closest = dist;
                RaycastHit[] hits = Physics.RaycastAll(pivot, dirN, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
                    if (hit.distance < closest) closest = hit.distance;
                }
                float finalDist = Mathf.Max(minDistance, closest - collisionBuffer);
                desired = pivot + dirN * finalDist;

                Vector3 smoothed = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
                Vector3 d2 = smoothed - pivot;
                if (d2.magnitude > finalDist) smoothed = pivot + d2.normalized * finalDist;
                transform.position = smoothed;
            }
            else transform.position = desired;

            transform.LookAt(pivot);
        }
    }
}
