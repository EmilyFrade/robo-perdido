using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Chave industrial. Na demo existe a Chave 1/3 (Setor Montagem).
    /// Coletar a chave + chegar a saida = vitoria do setor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class IndustrialKey : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            BatterySystem battery = other.GetComponentInParent<BatterySystem>();
            if (battery == null) return;

            if (GameManager.Instance != null) GameManager.Instance.OnKeyCollected();
            Destroy(gameObject);
        }
    }
}
