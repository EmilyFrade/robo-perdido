using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Poca corrosiva (verde, cor Acido da paleta). Ensina visualmente que algumas areas
    /// machucam, preparando o jogador para o Subsolo Quimico (Etapa 6, secao 6.1).
    /// Drena bateria continuamente enquanto o M-37 esta dentro.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CorrosivePuddle : MonoBehaviour
    {
        // Ajuste do playtest (Etapa 7): a poca matava quase instantaneamente (14/s). Reduzido
        // para um valor que AVISA antes de matar, dando tempo de o jogador sair.
        public float damagePerSecond = 6f;

        void OnTriggerStay(Collider other)
        {
            BatterySystem battery = other.GetComponentInParent<BatterySystem>();
            if (battery == null) return;

            battery.Drain(damagePerSecond * Time.deltaTime);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FlagHazard("Poça corrosiva! Saia já — bateria caindo");
                GameManager.Instance.SetDeathCause("você ficou tempo demais na poça corrosiva.");
            }
        }
    }
}
