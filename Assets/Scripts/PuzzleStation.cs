using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Estação de puzzle de uma sala. Ao interagir (E) perto dela, resolve um desafio que abre
    /// a porta da sala. Tipos: Button (apertar), Cables (ligar cabos — popup), MultiChoice
    /// (responder — popup), CleanDust (limpar poeira — popup) e KeyGate (porta final: só abre
    /// com a chave do setor). Os popups são desenhados via IMGUI pelo GameManager.
    /// </summary>
    public class PuzzleStation : MonoBehaviour
    {
        public enum PuzzleType { Button, Cables, MultiChoice, CleanDust, KeyGate }

        public PuzzleType type;
        public SlidingDoor door;
        public float radius = 3.4f;
        public bool solved;

        // MultiChoice
        public string question = "Qual cor indica energia no painel?";
        public string[] options = { "Vermelho", "Âmbar", "Verde" };
        public int correctIndex = 1;

        // Cables / CleanDust
        public int pairCount = 4;
        public int dustCols = 8, dustRows = 5;

        // ---- estado dos popups ----
        Color[] cabColors;
        int[] cabRightPerm;
        bool[] cabLeftDone, cabRightDone;
        int[] cabConn;          // para cada nó da esquerda, o slot da direita conectado (-1 = nenhum)
        int cabSel = -1;
        bool[] dust;
        int dustCleared;
        float wrongFlash;
        float dragSoundT;
        Color[] panelColors;   // botões do painel de controle (tipo Button)
        int panelCorrect;

        static readonly Color[] Palette =
        {
            new Color(0.85f,0.25f,0.2f), new Color(0.9f,0.65f,0.15f), new Color(0.3f,0.7f,0.35f),
            new Color(0.25f,0.55f,0.85f), new Color(0.7f,0.4f,0.8f), new Color(0.3f,0.75f,0.75f),
        };

        public bool InRange(Vector3 p) => Vector3.Distance(p, transform.position) <= radius;

        public string Hint()
        {
            switch (type)
            {
                case PuzzleType.Button: return "apertar botão (E)";
                case PuzzleType.Cables: return "ligar cabos (E)";
                case PuzzleType.MultiChoice: return "responder (E)";
                case PuzzleType.CleanDust: return "limpar poeira (E)";
                default: return "abrir porta (E) — precisa da chave";
            }
        }

        // Chamado pelo RobotController ao apertar E.
        public void Interact(GameManager gm)
        {
            if (solved) return;
            switch (type)
            {
                case PuzzleType.Button:
                    gm.OpenPuzzle(this);   // abre o painel de controle (botões coloridos)
                    break;
                case PuzzleType.KeyGate:
                    if (gm.HasLevelKey) Solve();
                    else gm.FlagHazard("A porta final só abre com a chave do setor");
                    break;
                default:
                    gm.OpenPuzzle(this);
                    break;
            }
        }

        public void BeginPopup()
        {
            if (type == PuzzleType.Cables)
            {
                cabColors = new Color[pairCount];
                for (int i = 0; i < pairCount; i++) cabColors[i] = Palette[i % Palette.Length];
                cabRightPerm = new int[pairCount];
                for (int i = 0; i < pairCount; i++) cabRightPerm[i] = i;
                for (int i = pairCount - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (cabRightPerm[i], cabRightPerm[j]) = (cabRightPerm[j], cabRightPerm[i]); }
                cabLeftDone = new bool[pairCount];
                cabRightDone = new bool[pairCount];
                cabConn = new int[pairCount];
                for (int i = 0; i < pairCount; i++) cabConn[i] = -1;
                cabSel = -1;
            }
            else if (type == PuzzleType.CleanDust)
            {
                dust = new bool[dustCols * dustRows];
                for (int i = 0; i < dust.Length; i++) dust[i] = true;
                dustCleared = 0;
            }
            else if (type == PuzzleType.Button)
            {
                // painel com botões coloridos; o VERDE abre a porta (posição embaralhada).
                panelColors = new[]
                {
                    new Color(0.85f,0.25f,0.2f), new Color(0.25f,0.55f,0.85f),
                    new Color(0.9f,0.65f,0.15f), new Color(0.3f,0.7f,0.35f),
                };
                for (int i = panelColors.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (panelColors[i], panelColors[j]) = (panelColors[j], panelColors[i]); }
                for (int i = 0; i < panelColors.Length; i++) if (panelColors[i].g > 0.6f && panelColors[i].r < 0.5f) panelCorrect = i;
            }
            wrongFlash = 0f;
        }

        public void Solve()
        {
            if (solved) return;
            solved = true;
            if (door != null) door.Open();
            if (GameManager.Instance != null) GameManager.Instance.FlagHazard("Puzzle resolvido — porta liberada");
            if (SoundManager.Instance != null) SoundManager.Instance.UiSolved();
        }

        // ============================================================ POPUPS (IMGUI)
        static Texture2D _tex;
        static Texture2D Tex
        {
            get { if (_tex == null) { _tex = new Texture2D(1, 1); _tex.SetPixel(0, 0, Color.white); _tex.Apply(); } return _tex; }
        }
        static void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Tex); GUI.color = Color.white; }
        static void Lbl(Rect r, string s, int size, Color c, TextAnchor a)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = a, fontStyle = FontStyle.Bold, wordWrap = true };
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }
        static bool Btn(Rect r, string s, int size)
        {
            var st = new GUIStyle(GUI.skin.button) { fontSize = size, fontStyle = FontStyle.Bold };
            return GUI.Button(r, s, st);
        }

        public void DrawPopup(Rect area, GameManager gm)
        {
            Fill(area, new Color(0.11f, 0.15f, 0.19f, 0.98f));
            Fill(new Rect(area.x, area.y, area.width, 4f), GameColors.Ambar);

            string title = type == PuzzleType.Cables ? "LIGAR CABOS"
                : type == PuzzleType.MultiChoice ? "PAINEL DE DIAGNÓSTICO"
                : type == PuzzleType.Button ? "PAINEL DE CONTROLE"
                : "LIMPAR POEIRA DOS CONTATOS";
            Lbl(new Rect(area.x, area.y + 12, area.width, 28), title, 20, GameColors.Ambar, TextAnchor.MiddleCenter);

            if (type == PuzzleType.Cables) DrawCables(area, gm);
            else if (type == PuzzleType.MultiChoice) DrawMultiChoice(area, gm);
            else if (type == PuzzleType.Button) DrawControlPanel(area, gm);
            else DrawCleanDust(area, gm);

            // botão fechar/cancelar
            if (Btn(new Rect(area.xMax - 110, area.y + 10, 96, 30), "Cancelar (ESC)", 12)) gm.ClosePuzzle();
        }

        void DrawCables(Rect area, GameManager gm)
        {
            Lbl(new Rect(area.x, area.y + 44, area.width, 22), "Clique numa cor à esquerda e na MESMA cor à direita.", 14, GameColors.Papel, TextAnchor.MiddleCenter);
            float top = area.y + 90, gap = (area.height - 140) / Mathf.Max(1, pairCount);
            float lx = area.x + 70, rx = area.xMax - 110, s = 38;

            // 1) linhas dos cabos já conectados (atrás dos nós)
            for (int i = 0; i < pairCount; i++)
            {
                if (cabConn == null || cabConn[i] < 0) continue;
                Vector2 a = new Vector2(lx + s, top + i * gap + s / 2f);
                Vector2 b = new Vector2(rx, top + cabConn[i] * gap + s / 2f);
                Line(a, b, 5f, cabColors[i]);
            }

            // 2) nós da esquerda e da direita
            for (int i = 0; i < pairCount; i++)
            {
                float yy = top + i * gap;
                Fill(new Rect(lx - 3, yy - 3, s + 6, s + 6), cabLeftDone[i] ? GameColors.Acido : new Color(0, 0, 0, 0.5f));
                if (GUI.Button(new Rect(lx, yy, s, s), GUIContent.none)) { if (!cabLeftDone[i]) { cabSel = i; if (SoundManager.Instance != null) SoundManager.Instance.UiClick(); } }
                Fill(new Rect(lx + 4, yy + 4, s - 8, s - 8), cabColors[i]);
                if (cabSel == i) Fill(new Rect(lx, yy, s, 4), GameColors.Ambar);

                int rc = cabRightPerm[i];
                Fill(new Rect(rx - 3, yy - 3, s + 6, s + 6), cabRightDone[i] ? GameColors.Acido : new Color(0, 0, 0, 0.5f));
                if (GUI.Button(new Rect(rx, yy, s, s), GUIContent.none))
                {
                    if (!cabRightDone[i] && cabSel >= 0)
                    {
                        int li = cabSel;
                        if (cabColors[li] == cabColors[rc]) { cabLeftDone[li] = true; cabRightDone[i] = true; cabConn[li] = i; cabSel = -1; if (SoundManager.Instance != null) SoundManager.Instance.UiConnect(); CheckCables(gm); }
                        else { cabSel = -1; wrongFlash = Time.unscaledTime + 0.6f; if (SoundManager.Instance != null) SoundManager.Instance.UiWrong(); }
                    }
                }
                Fill(new Rect(rx + 4, yy + 4, s - 8, s - 8), cabColors[rc]);
            }
            if (Time.unscaledTime < wrongFlash)
                Lbl(new Rect(area.x, area.yMax - 36, area.width, 24), "Cores diferentes!", 14, new Color(0.95f, 0.4f, 0.3f), TextAnchor.MiddleCenter);
        }

        // desenha uma linha (retângulo rotacionado) — o "cabo" conectado.
        static void Line(Vector2 a, Vector2 b, float w, Color c)
        {
            Matrix4x4 saved = GUI.matrix;
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b);
            GUIUtility.RotateAroundPivot(ang, a);
            Fill(new Rect(a.x, a.y - w * 0.5f, len, w), c);
            GUI.matrix = saved;
        }

        void CheckCables(GameManager gm)
        {
            for (int i = 0; i < pairCount; i++) if (!cabLeftDone[i]) return;
            Solve(); gm.ClosePuzzle();
        }

        void DrawControlPanel(Rect area, GameManager gm)
        {
            Lbl(new Rect(area.x, area.y + 44, area.width, 22), "Aperte o botão VERDE para liberar a porta.", 14, GameColors.Papel, TextAnchor.MiddleCenter);
            if (panelColors == null) return;
            float bw = 90, bh = 70, gap = 24;
            float total = panelColors.Length * bw + (panelColors.Length - 1) * gap;
            float x0 = area.x + (area.width - total) / 2f, y0 = area.y + area.height / 2f - bh / 2f;
            for (int i = 0; i < panelColors.Length; i++)
            {
                Rect r = new Rect(x0 + i * (bw + gap), y0, bw, bh);
                Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.05f, 0.07f, 0.09f));
                bool hover = r.Contains(Event.current.mousePosition);
                Fill(r, panelColors[i] * (hover ? 1.25f : 1f));
                Fill(new Rect(r.x + 8, r.y + 8, r.width - 16, 6), new Color(1f, 1f, 1f, 0.35f)); // brilho
                if (Event.current.type == EventType.MouseDown && hover)
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.UiClick();
                    if (i == panelCorrect) { Solve(); gm.ClosePuzzle(); return; }
                    else { if (SoundManager.Instance != null) SoundManager.Instance.UiWrong(); wrongFlash = Time.unscaledTime + 0.7f; }
                }
            }
            if (Time.unscaledTime < wrongFlash)
                Lbl(new Rect(area.x, area.yMax - 36, area.width, 24), "Botão errado — tente outro.", 14, new Color(0.95f, 0.4f, 0.3f), TextAnchor.MiddleCenter);
        }

        void DrawMultiChoice(Rect area, GameManager gm)
        {
            Lbl(new Rect(area.x + 30, area.y + 50, area.width - 60, 50), question, 17, GameColors.Papel, TextAnchor.MiddleCenter);
            float bw = area.width - 120, bh = 40, bx = area.x + 60, by = area.y + 120;
            for (int i = 0; i < options.Length; i++)
            {
                if (Btn(new Rect(bx, by + i * (bh + 12), bw, bh), options[i], 16))
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.UiClick();
                    if (i == correctIndex) { Solve(); gm.ClosePuzzle(); }
                    else { if (SoundManager.Instance != null) SoundManager.Instance.UiWrong(); wrongFlash = Time.unscaledTime + 0.8f; }
                }
            }
            if (Time.unscaledTime < wrongFlash)
                Lbl(new Rect(area.x, area.yMax - 36, area.width, 24), "Resposta errada — tente de novo.", 14, new Color(0.95f, 0.4f, 0.3f), TextAnchor.MiddleCenter);
        }

        void DrawCleanDust(Rect area, GameManager gm)
        {
            Lbl(new Rect(area.x, area.y + 44, area.width, 22), "Passe o mouse com o botão pressionado para limpar a poeira.", 14, GameColors.Papel, TextAnchor.MiddleCenter);
            float gx = area.x + 50, gy = area.y + 84;
            float gw = area.width - 100, gh = area.height - 150;
            float cw = gw / dustCols, ch = gh / dustRows;

            // base (contatos limpos por baixo)
            Fill(new Rect(gx, gy, gw, gh), new Color(0.2f, 0.28f, 0.32f));
            for (int c = 0; c < dustCols; c++)
                for (int rr = 0; rr < dustRows; rr++)
                {
                    Rect cell = new Rect(gx + c * cw, gy + rr * ch, cw - 1, ch - 1);
                    int idx = rr * dustCols + c;
                    if (dust[idx])
                    {
                        Event e = Event.current;
                        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cell.Contains(e.mousePosition))
                        {
                            dust[idx] = false; dustCleared++;
                            if (Time.unscaledTime - dragSoundT > 0.06f) { dragSoundT = Time.unscaledTime; if (SoundManager.Instance != null) SoundManager.Instance.UiDrag(); }
                            if (dustCleared >= dust.Length) { Solve(); gm.ClosePuzzle(); return; }
                        }
                        Fill(cell, new Color(0.55f, 0.5f, 0.42f, 0.95f));
                    }
                }
            float pct = dust.Length > 0 ? (float)dustCleared / dust.Length : 1f;
            Lbl(new Rect(area.x, area.yMax - 36, area.width, 24), "Limpo: " + Mathf.RoundToInt(pct * 100f) + "%", 14, GameColors.Ambar, TextAnchor.MiddleCenter);
        }
    }
}
