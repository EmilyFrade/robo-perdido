using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Cano que goteja: uma gota cai do cano até a poça no chão; ao TOCAR a poça, toca o som de
    /// pingo (3D) e reinicia. Puramente ambiental.
    /// </summary>
    public class WaterDrip : MonoBehaviour
    {
        public Transform drop;
        public float topY;
        public float puddleY;
        public float speed = 2.6f;
        public AudioSource sfx;
        public Transform splash;   // disco que "abre" ao bater na poça

        float y;
        float splashT;

        void Start() { y = topY; }

        void Update()
        {
            y -= speed * Time.deltaTime;
            if (y <= puddleY)
            {
                y = topY;
                if (sfx != null) sfx.Play();      // pingo ao encostar na poça
                splashT = 0.25f;
            }
            if (drop != null)
            {
                Vector3 p = drop.position; p.y = y; drop.position = p;
            }
            // pequeno "splash" na poça
            if (splash != null)
            {
                splashT -= Time.deltaTime;
                float s = splashT > 0f ? Mathf.Lerp(0.2f, 0.6f, 1f - splashT / 0.25f) : 0.2f;
                splash.localScale = new Vector3(s, splash.localScale.y, s);
            }
        }
    }
}
