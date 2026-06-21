using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// MECANICA CENTRAL: a bateria do M-37 funciona como vida.
    /// Toda acao gasta energia; coletar celulas recarrega. Quando chega a 0%, e derrota.
    /// Este componente NAO decide os custos (quem decide e quem chama Drain), ele apenas
    /// guarda o estado e registra o ultimo gasto para o feedback visual da HUD (Ajuste 1).
    /// </summary>
    public class BatterySystem : MonoBehaviour
    {
        public float max = 100f;
        public float current = 100f;

        public float Percent => Mathf.Clamp01(current / max);
        public bool IsDead => current <= 0f;

        // Usado pela HUD para mostrar a queda da bateria logo nos primeiros passos (Ajuste 1).
        public float lastDrainAmount;
        public float lastDrainTime = -999f;

        public void Drain(float amount)
        {
            if (amount <= 0f || IsDead) return;
            current = Mathf.Max(0f, current - amount);
            lastDrainAmount = amount;
            lastDrainTime = Time.time;
        }

        public void Charge(float amount)
        {
            if (amount <= 0f) return;
            current = Mathf.Min(max, current + amount);
        }
    }
}
