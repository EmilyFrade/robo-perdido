using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Saida do setor. Condicao de vitoria da demo: chegar aqui COM a Chave 1/3.
    /// A porta so abre depois que o Puzzle 2 (painel de cabo) e resolvido.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            BatterySystem battery = other.GetComponentInParent<BatterySystem>();
            if (battery == null) return;

            if (GameManager.Instance != null) GameManager.Instance.TryReachExit();
        }
    }
}
