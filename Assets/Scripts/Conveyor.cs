using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Esteira transportadora (Setor Montagem). Zona-gatilho que empurra o que estiver em cima
    /// na direção da esteira: o robô (CharacterController) e caixas (Rigidbody). A textura da
    /// correia rola para reforçar o movimento. Bootstrap cria a correia visível (sólida, para
    /// pisar) e esta zona-gatilho logo acima dela.
    /// </summary>
    public class Conveyor : MonoBehaviour
    {
        public Vector3 worldDir = Vector3.forward;
        public float speed = 1.6f;
        public Renderer beltRenderer; // correia visível (rola a textura)

        void OnTriggerStay(Collider other)
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.Move(worldDir.normalized * speed * Time.deltaTime);
                return;
            }
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 v = worldDir.normalized * speed;
                rb.linearVelocity = new Vector3(v.x, rb.linearVelocity.y, v.z);
            }
        }

        void Update()
        {
            if (beltRenderer != null)
                beltRenderer.material.mainTextureOffset += new Vector2(0f, speed * 0.35f * Time.deltaTime);
        }
    }
}
