using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Marca uma caixa como "movel" (condicao da mecanica Empurrar/Puxar da Etapa 6).
    /// O empurrao em si e aplicado pelo RobotController via OnControllerColliderHit,
    /// e empurrar custa mais bateria do que correr.
    /// </summary>
    public class PushBox : MonoBehaviour
    {
        public bool isMovable = true;
    }
}
