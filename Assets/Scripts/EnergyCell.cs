using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Celula de energia: o "alivio" do loop central. Recarrega a bateria ao ser encostada.
    /// No teste em papel, esse foi o momento em que todos os jogadores sorriram.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnergyCell : MonoBehaviour
    {
        public float chargeAmount = 25f;

        Vector3 baseScale;
        float t;

        void Awake() { baseScale = transform.localScale; }

        void Update()
        {
            // Feedback visual de "objeto vivo": gira e pulsa.
            t += Time.deltaTime;
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            transform.localScale = baseScale * (1f + Mathf.Sin(t * 3f) * 0.08f);
        }

        void OnTriggerEnter(Collider other)
        {
            BatterySystem battery = other.GetComponentInParent<BatterySystem>();
            if (battery == null) return;

            battery.Charge(chargeAmount);
            if (GameManager.Instance != null) GameManager.Instance.OnCellCollected(chargeAmount);
            Destroy(gameObject);
        }
    }
}
