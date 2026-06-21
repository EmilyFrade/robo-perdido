using System.Collections.Generic;
using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Estado global da demo + HUD (desenhada via IMGUI/OnGUI para evitar dependencia de
    /// Canvas/TextMeshPro nesta fase de placeholders). Controla chaves, vitoria/derrota,
    /// tempo de jogo, mensagens contextuais e os "letreiros" de tutorial implicito.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public BatterySystem battery;
        public string sectorName = "Setor 1 — Montagem";
        public int keysRequired = 1;   // a demo exige 1 (a Chave 1/3)
        public int keysTotalGame = 3;
        public int keysCollected;
        public readonly List<InteractPanel> panels = new List<InteractPanel>();

        public enum State { Playing, Win, GameOver }
        public State state = State.Playing;
        public bool IsPlaying => state == State.Playing;

        float playTime;
        string hazardMsg = "";
        float hazardUntil;

        struct WorldLabel { public Transform t; public string text; public Color color; }
        readonly List<WorldLabel> labels = new List<WorldLabel>();

        Texture2D _tex;
        Texture2D Tex
        {
            get
            {
                if (_tex == null)
                {
                    _tex = new Texture2D(1, 1);
                    _tex.SetPixel(0, 0, Color.white);
                    _tex.Apply();
                }
                return _tex;
            }
        }

        void Awake() { Instance = this; }

        public void AddLabel(Transform t, string text, Color color)
        {
            labels.Add(new WorldLabel { t = t, text = text, color = color });
        }

        public void OnKeyCollected()
        {
            keysCollected++;
            FlagHazard("Chave " + keysCollected + "/" + keysTotalGame + " coletada!");
        }

        public void OnCellCollected(float amt)
        {
            FlagHazard("+" + Mathf.RoundToInt(amt) + "% de bateria");
        }

        public void FlagHazard(string m)
        {
            hazardMsg = m;
            hazardUntil = Time.time + 2.2f;
        }

        public void TryReachExit()
        {
            if (state != State.Playing) return;
            if (keysCollected >= keysRequired) state = State.Win;
            else FlagHazard("A saida esta trancada — falta a Chave 1/3");
        }

        public void OnBatteryDead()
        {
            if (state == State.Playing) state = State.GameOver;
        }

        void Update()
        {
            if (state == State.Playing) playTime += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.R)) Restart();

            // Alterna tela cheia <-> janela (alem do Alt+Enter nativo do player).
            if (Input.GetKeyDown(KeyCode.F11))
            {
                bool fs = !Screen.fullScreen;
                Screen.fullScreen = fs;
                FlagHazard(fs ? "Tela cheia" : "Modo janela");
            }
        }

        void Restart()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // ----------------------------- HUD -----------------------------
        void Bar(Rect r, float fill, Color c)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(r, Tex);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Tex);
            GUI.color = Color.white;
        }

        void Label(float x, float y, float w, float h, string s, int size, Color c, TextAnchor anchor)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                fontStyle = FontStyle.Bold
            };
            st.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(x + 1, y + 1, w, h), s, st); // sombra
            st.normal.textColor = c;
            GUI.Label(new Rect(x, y, w, h), s, st);
        }

        void OnGUI()
        {
            // ---- Letreiros de mundo (tutorial implicito) ----
            Camera cam = Camera.main;
            if (cam != null)
            {
                foreach (WorldLabel wl in labels)
                {
                    if (wl.t == null) continue;
                    Vector3 sp = cam.WorldToScreenPoint(wl.t.position + Vector3.up * 1.4f);
                    if (sp.z <= 0f) continue;
                    Label(sp.x - 130f, Screen.height - sp.y, 260f, 24f, wl.text, 14, wl.color, TextAnchor.MiddleCenter);
                }
            }

            // ---- Bateria ----
            float pct = battery != null ? battery.Percent : 0f;
            Color barColor = pct > 0.5f ? GameColors.Ambar : (pct > 0.2f ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.85f, 0.2f, 0.15f));
            Label(24, 18, 300, 24, "BATERIA  " + Mathf.CeilToInt(pct * 100f) + "%", 18, GameColors.Papel, TextAnchor.MiddleLeft);
            Bar(new Rect(26, 46, 300, 22), pct, barColor);

            // Ajuste 1: indicador de queda da bateria logo nos primeiros segundos.
            if (battery != null && Time.time - battery.lastDrainTime < 0.4f && battery.lastDrainAmount > 0.02f)
                Label(332, 44, 120, 24, "▼ gastando", 14, new Color(0.95f, 0.5f, 0.4f), TextAnchor.MiddleLeft);

            // ---- Progresso ----
            Label(24, 78, 360, 22, sectorName, 16, GameColors.Papel, TextAnchor.MiddleLeft);
            Label(24, 102, 360, 22, "Chaves: " + keysCollected + "/" + keysTotalGame + "   ·   Tempo: " + Mathf.FloorToInt(playTime) + "s", 15, GameColors.Papel, TextAnchor.MiddleLeft);

            // ---- Tutorial inicial (Ajuste 1: deixar claro que mover gasta bateria) ----
            if (state == State.Playing && playTime < 9f)
                Label(0, Screen.height - 96, Screen.width, 24, "WASD movem o M-37. Cada passo gasta bateria — pense antes de agir.", 16, GameColors.Ambar, TextAnchor.MiddleCenter);

            // ---- Controles ----
            Label(0, Screen.height - 30, Screen.width, 22,
                "WASD mover  ·  Shift correr  ·  C agachar  ·  E interagir  ·  R reiniciar  ·  F11 tela cheia",
                13, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);

            // ---- Banner contextual ----
            if (Time.time < hazardUntil && !string.IsNullOrEmpty(hazardMsg))
                Label(0, 150, Screen.width, 30, hazardMsg, 20, GameColors.Ambar, TextAnchor.MiddleCenter);

            // ---- Telas de fim ----
            if (state == State.Win)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
                GUI.color = Color.white;
                Label(0, Screen.height / 2 - 60, Screen.width, 40, "SETOR MONTAGEM CONCLUIDO", 30, GameColors.Ambar, TextAnchor.MiddleCenter);
                Label(0, Screen.height / 2 - 10, Screen.width, 30, "Você escapou com a Chave 1/3 e " + Mathf.CeilToInt(pct * 100f) + "% de bateria — em " + Mathf.FloorToInt(playTime) + "s.", 18, GameColors.Papel, TextAnchor.MiddleCenter);
                Label(0, Screen.height / 2 + 30, Screen.width, 30, "Pressione R para jogar de novo", 18, GameColors.Papel, TextAnchor.MiddleCenter);
            }
            else if (state == State.GameOver)
            {
                GUI.color = new Color(0.15f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
                GUI.color = Color.white;
                Label(0, Screen.height / 2 - 60, Screen.width, 40, "BATERIA ESGOTADA", 30, new Color(0.9f, 0.3f, 0.25f), TextAnchor.MiddleCenter);
                Label(0, Screen.height / 2 - 10, Screen.width, 30, "O M-37 desligou. Aprenda a rota e economize energia.", 18, GameColors.Papel, TextAnchor.MiddleCenter);
                Label(0, Screen.height / 2 + 30, Screen.width, 30, "Pressione R para reiniciar o setor", 18, GameColors.Papel, TextAnchor.MiddleCenter);
            }
        }
    }
}
