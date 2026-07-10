using UnityEngine;

namespace RoboPerdido
{
    /// <summary>Gira o objeto continuamente (hélices do drone).</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 1100f;
        public Vector3 axis = Vector3.up;
        void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); }
    }
}
