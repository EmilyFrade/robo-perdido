using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboPerdido
{
    /// <summary>
    /// Estado global da demo + toda a UI (desenhada via IMGUI/OnGUI). Na Etapa 8 deixou de ser
    /// "so HUD do gameplay" e passou a controlar o jogo de ponta a ponta:
    /// Classificacao indicativa -> Menu inicial -> Jogo -> Pausa -> Vitoria/Derrota.
    ///
    /// O mundo e construido pelo Bootstrap a cada carga da cena; este componente decide em qual
    /// estado o jogo comeca (campo estatico bootState) e faz as transicoes recarregando a cena,
    /// o que garante um reset limpo (sem assets binarios, lembrando a arquitetura do projeto).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        // Em qual estado a cena deve iniciar. Na 1a execucao do app mostra a Classificacao;
        // depois as transicoes (Jogar / Menu) ajustam isso antes de recarregar a cena.
        static State bootState = State.Classind;

        public BatterySystem battery;
        public string sectorName = "Setor 1 — Montagem";
        public int keysTotalGame = 4;          // 1 chave por setor (4 setores)
        public const int TotalLevels = 4;

        // Progresso do JOGO (persiste entre fases via static; recarregar a cena nao zera).
        public static int currentLevel;        // 0..3
        public static int keysBanked;          // chaves de setores ja concluidos
        bool levelKeyCollected;                // chave do setor atual ja foi pega?

        public int KeysShown => keysBanked + (levelKeyCollected ? 1 : 0);
        public bool IsLastLevel => currentLevel >= TotalLevels - 1;
        public bool HasLevelKey => levelKeyCollected;

        public readonly List<PuzzleStation> puzzles = new List<PuzzleStation>();
        PuzzleStation activePuzzle;

        public enum State { Classind, Menu, Controls, Credits, Playing, Paused, Puzzle, LevelComplete, Win, GameOver, Ending }

        // História da Fase 1 (digitada no canto superior direito) e os 3 finais (Portão Externo).
        const string StoryL1 = "O M-37 foi abandonado pelo seu dono dentro da fábrica Ferralis-9. Para reencontrá-lo, ele precisa atravessar a indústria e escapar.";
        static readonly string[] EndingLines =
        {
            "Você chegou ao Portão Externo. Qual será o destino do M-37?",
            "1) Atravessar a luz e descobrir o futuro — e se reencontrou seu dono.",
            "2) Permanecer na sua insignificância dentro da indústria.",
            "3) Desligar — o M-37 encerra a própria bateria.",
        };
        float endingStart;
        bool endingChosen;          // já escolheu uma opção? (mostra o desfecho antes de agir)
        int endingChoice;
        float endingTextStart;
        string endingOutcome = "";
        public State state = State.Menu;
        public bool IsPlaying => state == State.Playing;

        float playTime;
        string hazardMsg = "";
        float hazardUntil;

        // Causa da morte (Ajuste do playtest: a tela de derrota nomeia o que matou o jogador).
        string deathCause = "A energia se esgotou aos poucos.";
        float deathCauseTime = -999f;

        struct WorldLabel { public Transform t; public string text; public Color color; }
        readonly List<WorldLabel> labels = new List<WorldLabel>();

        Transform Player => battery != null ? battery.transform : null;

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

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            AudioListener.pause = false;   // garante áudio retomado a cada carga da cena
            state = bootState;
        }

        // -------------------------------------------------- transicoes de estado
        static void Reload()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Novo jogo (do Setor 1).
        public static void StartGame() { currentLevel = 0; keysBanked = 0; bootState = State.Playing; Reload(); }
        // Avanca para o proximo setor, guardando a chave do setor concluido.
        public static void AdvanceLevel() { keysBanked++; currentLevel++; bootState = State.Playing; Reload(); }
        // Reinicia apenas o setor atual (mantem chaves de setores anteriores).
        public static void RestartLevel() { bootState = State.Playing; Reload(); }
        public static void ReturnToMenu() { bootState = State.Menu; Reload(); }

        void Resume() { state = State.Playing; Time.timeScale = 1f; }
        void Pause() { state = State.Paused; Time.timeScale = 0f; }

        // Escolha do final (Portão Externo): grava o desfecho; a ação só roda depois do texto.
        void ChooseEnding(int n)
        {
            if (endingChosen) return;
            Click();
            endingChosen = true; endingChoice = n; endingTextStart = Time.unscaledTime;
            endingOutcome = OutcomeText(n);
            if (SoundManager.Instance != null) { if (n == 3) SoundManager.Instance.Lose(); else SoundManager.Instance.Win(); }
        }

        // Executa a ação do final escolhido (após o jogador ler o desfecho).
        void ExecuteEnding()
        {
            if (endingChoice == 1) state = State.Win;   // atravessar a luz → tela de vitória (volta ao menu)
            else if (endingChoice == 2) StartGame();    // permanecer → Fase 1
            else ReturnToMenu();                        // desligar → menu
        }

        static string OutcomeText(int n)
        {
            switch (n)
            {
                case 1: return "O M-37 avança para o portão e a luz o engole por inteiro. Do outro lado pode estar seu dono, à sua espera — ou apenas o mundo, imenso e desconhecido. Não importa: pela primeira vez, ele está livre para descobrir.";
                case 2: return "O M-37 dá meia-volta e recolhe-se a um canto escuro da fábrica. As máquinas seguem girando, indiferentes à sua presença. Talvez um dia ele crie coragem de tentar de novo.";
                default: return "O M-37 fecha os dois olhos vermelhos. Um último estalo, e a bateria se apaga em silêncio. Ferralis-9 fica um pouco mais escura — e o pequeno robô, enfim, descansa.";
            }
        }

        // Efeito "máquina de escrever": revela os primeiros chars conforme o tempo passa.
        static string TypeChars(string s, float elapsed, float cps, bool cursor)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsed) * cps), 0, s.Length);
            string vis = s.Substring(0, n);
            if (cursor && n < s.Length && Mathf.FloorToInt(Time.unscaledTime * 2.6f) % 2 == 0) vis += "▌";
            return vis;
        }

        // Abre o popup de um puzzle (cabos / múltipla escolha / poeira).
        public void OpenPuzzle(PuzzleStation s)
        {
            activePuzzle = s;
            s.BeginPopup();
            state = State.Puzzle;
            Time.timeScale = 0f;
            AudioListener.pause = true;   // silencia o jogo; só os sons do minigame tocam
        }
        public void ClosePuzzle()
        {
            activePuzzle = null;
            state = State.Playing;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // -------------------------------------------------- eventos do gameplay
        public void AddLabel(Transform t, string text, Color color)
        {
            labels.Add(new WorldLabel { t = t, text = text, color = color });
        }

        public void OnKeyCollected()
        {
            levelKeyCollected = true;
            FlagHazard("Chave " + KeysShown + "/" + keysTotalGame + " coletada!");
            if (SoundManager.Instance != null) SoundManager.Instance.Key();
        }

        public void OnCellCollected(float amt)
        {
            FlagHazard("+" + Mathf.RoundToInt(amt) + "% de bateria");
            if (SoundManager.Instance != null) SoundManager.Instance.Cell();
        }

        public void FlagHazard(string m)
        {
            hazardMsg = m;
            hazardUntil = Time.unscaledTime + 2.2f;
        }

        // Registra a causa do dano recente, para a tela de derrota explicar o que aconteceu.
        public void SetDeathCause(string cause)
        {
            deathCause = cause;
            deathCauseTime = Time.time;
        }

        public void TryReachExit()
        {
            if (state != State.Playing) return;
            if (!levelKeyCollected)
            {
                FlagHazard("A saida esta trancada — pegue a chave deste setor");
                return;
            }
            if (IsLastLevel) { state = State.Ending; endingStart = Time.unscaledTime; endingChosen = false; }  // Portão Externo: escolha do final
            else state = State.LevelComplete;          // setor concluido, vai para o proximo
            if (SoundManager.Instance != null) SoundManager.Instance.Win();
        }

        public void OnBatteryDead()
        {
            if (state == State.Playing)
            {
                state = State.GameOver;
                if (SoundManager.Instance != null) SoundManager.Instance.Lose();
            }
        }

        void Update()
        {
            if (state == State.Playing) playTime += Time.deltaTime;

            switch (state)
            {
                case State.Classind:
                    if (Input.anyKeyDown) { state = State.Menu; Click(); }
                    break;
                case State.Controls:
                case State.Credits:
                    if (Input.GetKeyDown(KeyCode.Escape)) { state = State.Menu; Click(); }
                    break;
                case State.Playing:
                    if (Input.GetKeyDown(KeyCode.Escape)) { Pause(); Click(); }
                    if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
                    if (Input.GetKeyDown(KeyCode.F11)) ToggleFullscreen();
                    break;
                case State.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape)) { Resume(); Click(); }
                    break;
                case State.Puzzle:
                    if (Input.GetKeyDown(KeyCode.Escape)) { Click(); ClosePuzzle(); }
                    break;
                case State.LevelComplete:
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                    { Click(); AdvanceLevel(); }
                    break;
                case State.Win:
                    if (Input.GetKeyDown(KeyCode.R)) StartGame();
                    if (Input.GetKeyDown(KeyCode.M)) ReturnToMenu();
                    break;
                case State.GameOver:
                    if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
                    if (Input.GetKeyDown(KeyCode.M)) ReturnToMenu();
                    break;
                case State.Ending:
                    if (!endingChosen)
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ChooseEnding(1);
                        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ChooseEnding(2);
                        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ChooseEnding(3);
                    }
                    else if ((Time.unscaledTime - endingTextStart) * 30f >= endingOutcome.Length &&
                             (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
                        ExecuteEnding();
                    break;
            }
        }

        void ToggleFullscreen()
        {
            bool fs = !Screen.fullScreen;
            Screen.fullScreen = fs;
            FlagHazard(fs ? "Tela cheia" : "Modo janela");
        }

        void Click() { if (SoundManager.Instance != null) SoundManager.Instance.Click(); }

        // ============================================================ UI / HUD
        void Bar(Rect r, float fill, Color c)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(r, Tex);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Tex);
            GUI.color = Color.white;
        }

        void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Tex);
            GUI.color = Color.white;
        }

        void Label(float x, float y, float w, float h, string s, int size, Color c, TextAnchor anchor)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            st.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(x + 1, y + 1, w, h), s, st); // sombra
            st.normal.textColor = c;
            GUI.Label(new Rect(x, y, w, h), s, st);
        }

        bool Button(Rect r, string text)
        {
            Fill(r, new Color(GameColors.Ferrugem.r, GameColors.Ferrugem.g, GameColors.Ferrugem.b, 0.9f));
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover) Fill(new Rect(r.x, r.y, r.width, 2f), GameColors.Ambar);
            Label(r.x, r.y, r.width, r.height, text, 20, hover ? GameColors.Ambar : GameColors.Papel, TextAnchor.MiddleCenter);
            bool clicked = Event.current.type == EventType.MouseDown && hover;
            if (clicked) Click();
            return clicked;
        }

        void OnGUI()
        {
            switch (state)
            {
                case State.Classind: DrawClassind(); return;
                case State.Menu: DrawMenu(); return;
                case State.Controls: DrawControls(); return;
                case State.Credits: DrawCredits(); return;
            }

            // ---- HUD do gameplay (Playing / Paused / Win / GameOver) ----
            DrawWorldLabels();
            DrawHud();

            if (state == State.Paused) DrawPaused();
            else if (state == State.Puzzle) DrawPuzzle();
            else if (state == State.LevelComplete) DrawLevelComplete();
            else if (state == State.Win) DrawWin();
            else if (state == State.GameOver) DrawGameOver();
            else if (state == State.Ending) DrawEnding();
        }

        // -------------------------------------------------- telas de menu
        void Dim(float a)
        {
            GUI.color = new Color(GameColors.AcoBase.r, GameColors.AcoBase.g, GameColors.AcoBase.b, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
            GUI.color = Color.white;
        }

        // Selo de classificacao indicativa (sistema ClassInd / Brasil) — "Livre".
        void ClassindBadge(float x, float y, float s)
        {
            Fill(new Rect(x, y, s, s), new Color(0.12f, 0.5f, 0.18f)); // verde "Livre"
            Fill(new Rect(x + 3, y + 3, s - 6, s - 6), new Color(0.16f, 0.6f, 0.22f));
            Label(x, y, s, s, "L", Mathf.RoundToInt(s * 0.55f), Color.white, TextAnchor.MiddleCenter);
        }

        void DrawClassind()
        {
            Dim(0.97f);
            float cx = Screen.width / 2f;
            ClassindBadge(cx - 55, Screen.height / 2f - 150, 110);
            Label(0, Screen.height / 2f - 25, Screen.width, 30, "CLASSIFICACAO INDICATIVA", 24, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f + 12, Screen.width, 28, "LIVRE para todos os públicos", 20, GameColors.Ambar, TextAnchor.MiddleCenter);
            Label(cx - 320, Screen.height / 2f + 48, 640, 60,
                "Jogo de aventura e furtividade. Contém tensão/suspense leves. Não há violência explícita, conteúdo sexual, linguagem imprópria ou drogas.",
                15, new Color(0.8f, 0.78f, 0.7f), TextAnchor.UpperCenter);
            Label(0, Screen.height - 60, Screen.width, 24, "Pressione qualquer tecla para continuar", 16, GameColors.Papel, TextAnchor.MiddleCenter);
        }

        void DrawMenu()
        {
            Dim(0.78f);
            float cx = Screen.width / 2f;
            Label(0, Screen.height * 0.16f, Screen.width, 70, "ROBÔ PERDIDO", 56, GameColors.Ferrugem, TextAnchor.MiddleCenter);
            Label(0, Screen.height * 0.16f + 70, Screen.width, 30, "Setor Montagem — Demo · Unidade M-37", 18, GameColors.Papel, TextAnchor.MiddleCenter);

            float bw = 280, bh = 52, gap = 14;
            float bx = cx - bw / 2f;
            float by = Screen.height * 0.42f;

            if (Button(new Rect(bx, by, bw, bh), "JOGAR")) StartGame();
            if (Button(new Rect(bx, by + (bh + gap), bw, bh), "COMO JOGAR")) { state = State.Controls; }
            if (Button(new Rect(bx, by + 2 * (bh + gap), bw, bh), "CRÉDITOS")) { state = State.Credits; }
            if (Button(new Rect(bx, by + 3 * (bh + gap), bw, bh), "SAIR")) Application.Quit();

            // selo + legenda da classificacao no rodape
            ClassindBadge(28, Screen.height - 90, 60);
            Label(96, Screen.height - 84, 300, 24, "Classificação: Livre", 15, GameColors.Papel, TextAnchor.MiddleLeft);
            Label(96, Screen.height - 60, 320, 24, "Tensão/suspense leves", 13, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleLeft);

            Label(0, Screen.height - 30, Screen.width, 22, "UFOP · Design e Desenvolvimento de Jogos", 13, new Color(0.7f, 0.68f, 0.62f), TextAnchor.MiddleCenter);
        }

        void DrawControls()
        {
            Dim(0.9f);
            Label(0, Screen.height * 0.12f, Screen.width, 40, "COMO JOGAR", 34, GameColors.Ambar, TextAnchor.MiddleCenter);

            float cx = Screen.width / 2f;
            Label(cx - 340, Screen.height * 0.12f + 56, 680, 60,
                "Você é o M-37, um robô de manutenção preso na fábrica Ferralis-9. A BATERIA é a sua VIDA: cada ação gasta energia. Atravesse o setor, pegue a Chave 1/3 e chegue à saída sem zerar a bateria.",
                16, GameColors.Papel, TextAnchor.UpperCenter);

            string[,] rows = {
                { "WASD / setas", "Mover (anda devagar, gasta pouca bateria)" },
                { "Shift", "Correr (rápido, gasta mais e faz BARULHO)" },
                { "C / Ctrl", "Agachar (lento, porém silencioso)" },
                { "E", "Interagir / conectar painel" },
                { "ESC", "Pausar" },
                { "R", "Reiniciar o setor" },
                { "F11 / Alt+Enter", "Tela cheia / janela" },
            };
            float ty = Screen.height * 0.34f;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Label(cx - 320, ty + i * 30, 200, 26, rows[i, 0], 16, GameColors.Ambar, TextAnchor.MiddleLeft);
                Label(cx - 100, ty + i * 30, 440, 26, rows[i, 1], 16, GameColors.Papel, TextAnchor.MiddleLeft);
            }

            Label(cx - 320, ty + 7 * 30 + 14, 640, 26, "Dica: agache (C) atrás das caixas para escapar do cone de visão do drone.", 14, new Color(0.85f, 0.8f, 0.6f), TextAnchor.MiddleLeft);

            if (Button(new Rect(cx - 110, Screen.height - 90, 220, 48), "VOLTAR")) state = State.Menu;
        }

        void DrawCredits()
        {
            Dim(0.9f);
            float cx = Screen.width / 2f;
            Label(0, Screen.height * 0.14f, Screen.width, 40, "CRÉDITOS", 34, GameColors.Ambar, TextAnchor.MiddleCenter);

            Label(0, Screen.height * 0.30f, Screen.width, 28, "ROBÔ PERDIDO — Demo (Setor Montagem)", 20, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height * 0.30f + 40, Screen.width, 26, "Emily Frade dos Santos — 23.1.8001", 17, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height * 0.30f + 68, Screen.width, 26, "Luís Eduardo Bastos Rocha — 23.1.8095", 17, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height * 0.30f + 110, Screen.width, 26, "UFOP · Design e Desenvolvimento de Jogos", 15, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);
            Label(0, Screen.height * 0.30f + 140, Screen.width, 26, "Engine: Unity 6 · C# · Arte, texturas e áudio gerados por código", 14, new Color(0.75f, 0.72f, 0.66f), TextAnchor.MiddleCenter);

            if (Button(new Rect(cx - 110, Screen.height - 90, 220, 48), "VOLTAR")) state = State.Menu;
        }

        // -------------------------------------------------- HUD do jogo
        void DrawWorldLabels()
        {
            Camera cam = Camera.main;
            if (cam == null || Player == null) return;

            // Declutter (Ajuste do playtest): so mostra os letreiros perto do robo, evitando o
            // empilhamento ilegivel do corredor inicial.
            foreach (WorldLabel wl in labels)
            {
                if (wl.t == null) continue;
                float d = Vector3.Distance(Player.position, wl.t.position);
                if (d > 8.5f) continue;
                Vector3 sp = cam.WorldToScreenPoint(wl.t.position + Vector3.up * 1.4f);
                if (sp.z <= 0f) continue;
                float alpha = Mathf.Clamp01(1.2f - d / 8.5f);
                Color c = wl.color; c.a = alpha;
                Label(sp.x - 140f, Screen.height - sp.y, 280f, 24f, wl.text, 14, c, TextAnchor.MiddleCenter);
            }
        }

        void DrawHud()
        {
            float pct = battery != null ? battery.Percent : 0f;
            Color barColor = pct > 0.5f ? GameColors.Ambar : (pct > 0.2f ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.85f, 0.2f, 0.15f));

            Label(24, 16, 320, 24, "BATERIA  " + Mathf.CeilToInt(pct * 100f) + "%", 18, GameColors.Papel, TextAnchor.MiddleLeft);
            Bar(new Rect(26, 44, 300, 22), pct, barColor);

            // indicador de queda da bateria
            if (battery != null && Time.time - battery.lastDrainTime < 0.4f && battery.lastDrainAmount > 0.02f)
                Label(332, 42, 140, 24, "▼ gastando", 14, new Color(0.95f, 0.5f, 0.4f), TextAnchor.MiddleLeft);

            Label(24, 76, 420, 22, "Fase " + (currentLevel + 1) + "/" + TotalLevels + " — " + sectorName, 16, GameColors.Papel, TextAnchor.MiddleLeft);
            Label(24, 100, 420, 22, "Chaves: " + KeysShown + "/" + keysTotalGame + "   ·   Tempo: " + Mathf.FloorToInt(playTime) + "s", 15, GameColors.Papel, TextAnchor.MiddleLeft);

            // Estado CORRENDO (Ajuste do playtest: feedback claro de que correr denuncia).
            RobotController rc = Player != null ? Player.GetComponent<RobotController>() : null;
            if (rc != null && state == State.Playing)
            {
                if (rc.IsRunning) Label(0, 120, Screen.width, 26, "● CORRENDO — faz barulho!", 16, new Color(0.95f, 0.4f, 0.3f), TextAnchor.MiddleCenter);
                else if (rc.IsCrouching) Label(0, 120, Screen.width, 26, "○ agachado — silencioso", 15, new Color(0.6f, 0.85f, 0.6f), TextAnchor.MiddleCenter);
            }

            // Tutorial inicial
            if (state == State.Playing && playTime < 9f)
                Label(0, Screen.height - 96, Screen.width, 24, "WASD movem o M-37. Cada passo gasta bateria — pense antes de agir.", 16, GameColors.Ambar, TextAnchor.MiddleCenter);

            Label(0, Screen.height - 30, Screen.width, 22,
                "WASD mover · Shift correr · C agachar · E interagir · ESC pausar · R reiniciar",
                13, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);

            if (Time.unscaledTime < hazardUntil && !string.IsNullOrEmpty(hazardMsg))
                Label(0, 150, Screen.width, 30, hazardMsg, 20, GameColors.Ambar, TextAnchor.MiddleCenter);

            // História da Fase 1 digitando no canto superior DIREITO.
            if (currentLevel == 0 && state == State.Playing)
            {
                float w = 330f, x = Screen.width - w - 18f;
                Fill(new Rect(x - 10, 14, w + 18, 138), new Color(0f, 0f, 0f, 0.5f));
                Fill(new Rect(x - 10, 14, w + 18, 3), GameColors.Ambar);
                Label(x + 4, 20, w, 20, "▶ REGISTRO M-37", 13, GameColors.Ambar, TextAnchor.UpperLeft);
                Label(x + 4, 44, w, 104, TypeChars(StoryL1, playTime - 1f, 14f, true), 15, GameColors.Papel, TextAnchor.UpperLeft);
            }
        }

        // -------------------------------------------------- overlays de pausa / fim
        void DrawPuzzle()
        {
            Dim(0.7f);
            if (activePuzzle == null) { ClosePuzzle(); return; }
            float w = Mathf.Min(620f, Screen.width - 80f);
            float h = Mathf.Min(440f, Screen.height - 80f);
            Rect area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            activePuzzle.DrawPopup(area, this);
        }

        void DrawPaused()
        {
            Dim(0.65f);
            float cx = Screen.width / 2f;
            Label(0, Screen.height / 2f - 130, Screen.width, 40, "PAUSADO", 34, GameColors.Ambar, TextAnchor.MiddleCenter);

            float bw = 280, bh = 50, gap = 14, bx = cx - bw / 2f, by = Screen.height / 2f - 60;
            if (Button(new Rect(bx, by, bw, bh), "RETOMAR")) Resume();
            if (Button(new Rect(bx, by + (bh + gap), bw, bh), "REINICIAR SETOR")) RestartLevel();
            if (Button(new Rect(bx, by + 2 * (bh + gap), bw, bh), "MENU PRINCIPAL")) ReturnToMenu();
            if (Button(new Rect(bx, by + 3 * (bh + gap), bw, bh), "SAIR")) Application.Quit();
        }

        // Setor intermediario concluido -> proximo setor.
        void DrawLevelComplete()
        {
            float pct = battery != null ? battery.Percent : 0f;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
            GUI.color = Color.white;

            string next = currentLevel + 1 < TotalLevels ? SectorNameFor(currentLevel + 1) : "";
            Label(0, Screen.height / 2f - 100, Screen.width, 40, "SETOR CONCLUÍDO — " + sectorName, 28, GameColors.Ambar, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f - 52, Screen.width, 30, "Chave " + KeysShown + "/" + keysTotalGame + " obtida · " + Mathf.CeilToInt(pct * 100f) + "% de bateria · " + Mathf.FloorToInt(playTime) + "s", 18, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f - 14, Screen.width, 26, "Próximo: " + next, 17, new Color(0.85f, 0.8f, 0.6f), TextAnchor.MiddleCenter);

            float cx = Screen.width / 2f;
            if (Button(new Rect(cx - 170, Screen.height / 2f + 36, 340, 50), "CONTINUAR (ESPAÇO)")) AdvanceLevel();
        }

        // Vitoria final (Portao Externo nos Servidores) -> fim do jogo.
        void DrawWin()
        {
            float pct = battery != null ? battery.Percent : 0f;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
            GUI.color = Color.white;

            Label(0, Screen.height / 2f - 120, Screen.width, 44, "VOCÊ ESCAPOU DE FERRALIS-9", 34, GameColors.Ambar, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f - 66, Screen.width, 30, "Com as " + keysTotalGame + " chaves, o M-37 abriu o Portão Externo.", 18, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f - 32, Screen.width, 26, "Bateria final: " + Mathf.CeilToInt(pct * 100f) + "%  ·  Tempo no último setor: " + Mathf.FloorToInt(playTime) + "s", 15, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f + 2, Screen.width, 26, "Obrigado por jogar a demo de Robô Perdido.", 15, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);

            float cx = Screen.width / 2f;
            if (Button(new Rect(cx - 230, Screen.height / 2f + 44, 220, 48), "JOGAR DE NOVO (R)")) StartGame();
            if (Button(new Rect(cx + 10, Screen.height / 2f + 44, 220, 48), "MENU (M)")) ReturnToMenu();
        }

        // Tela dos 3 FINAIS (Portão Externo): escolha digitando e, depois, o DESFECHO digitando.
        void DrawEnding()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
            GUI.color = Color.white;

            float cx = Screen.width / 2f;
            float bw = Mathf.Min(740f, Screen.width - 80f);
            float x = cx - bw / 2f;
            Label(0, Screen.height * 0.13f, Screen.width, 40, "PORTÃO EXTERNO", 32, GameColors.Ambar, TextAnchor.MiddleCenter);

            if (!endingChosen) { DrawEndingChoices(x, bw); return; }

            // ---- DESFECHO da opção escolhida (texto digitando) ----
            string title = endingChoice == 1 ? "ATRAVESSAR A LUZ" : endingChoice == 2 ? "PERMANECER" : "DESLIGAR";
            Label(0, Screen.height * 0.28f, Screen.width, 30, title, 22, GameColors.Papel, TextAnchor.MiddleCenter);
            string vis = TypeChars(endingOutcome, Time.unscaledTime - endingTextStart, 30f, true);
            Label(x, Screen.height * 0.36f, bw, 200, vis, 18, GameColors.Papel, TextAnchor.UpperLeft);

            bool done = (Time.unscaledTime - endingTextStart) * 30f >= endingOutcome.Length;
            if (done)
            {
                Rect r = new Rect(cx - 150f, Screen.height - 96f, 300f, 46f);
                bool hover = r.Contains(Event.current.mousePosition);
                Fill(r, new Color(GameColors.Ferrugem.r, GameColors.Ferrugem.g, GameColors.Ferrugem.b, hover ? 0.85f : 0.6f));
                Label(r.x, r.y, r.width, r.height, "CONTINUAR (ENTER)", 18, hover ? GameColors.Ambar : GameColors.Papel, TextAnchor.MiddleCenter);
                if (Event.current.type == EventType.MouseDown && hover) ExecuteEnding();
            }
        }

        void DrawEndingChoices(float x, float bw)
        {
            int revealed = Mathf.FloorToInt((Time.unscaledTime - endingStart) * 30f);
            float y = Screen.height * 0.30f;
            int consumed = 0;
            bool allDone = true;
            for (int li = 0; li < EndingLines.Length; li++)
            {
                string s = EndingLines[li];
                int show = Mathf.Clamp(revealed - consumed, 0, s.Length);
                bool done = show >= s.Length;
                if (!done) allDone = false;
                string vis = s.Substring(0, show);
                if (show > 0 && !done && Mathf.FloorToInt(Time.unscaledTime * 2.6f) % 2 == 0) vis += "▌";

                if (li == 0)
                {
                    Label(x, y, bw, 50, vis, 18, GameColors.Papel, TextAnchor.UpperLeft);
                    y += 70;
                }
                else
                {
                    Rect r = new Rect(x, y, bw, 44);
                    bool active = done;
                    bool hover = active && r.Contains(Event.current.mousePosition);
                    if (active) Fill(new Rect(r.x - 8, r.y - 4, r.width + 16, r.height + 6), new Color(GameColors.Ferrugem.r, GameColors.Ferrugem.g, GameColors.Ferrugem.b, hover ? 0.55f : 0.3f));
                    Label(r.x + 4, r.y + 8, bw, 30, vis, 18, hover ? GameColors.Ambar : GameColors.Papel, TextAnchor.UpperLeft);
                    if (active && Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) ChooseEnding(li);
                    y += 56;
                }
                consumed += s.Length;
            }
            if (allDone)
                Label(0, Screen.height - 56, Screen.width, 24, "Clique numa opção ou pressione 1, 2 ou 3.", 15, new Color(0.85f, 0.8f, 0.6f), TextAnchor.MiddleCenter);
        }

        // Nome do setor por indice (usado nas telas de transicao).
        public static string SectorNameFor(int level)
        {
            switch (level)
            {
                case 0: return "Setor 1 — Montagem";
                case 1: return "Setor 2 — Caldeiras";
                case 2: return "Setor 3 — Subsolo Químico";
                case 3: return "Setor 4 — Servidores";
                default: return "Setor";
            }
        }

        void DrawGameOver()
        {
            GUI.color = new Color(0.15f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Tex);
            GUI.color = Color.white;

            Label(0, Screen.height / 2f - 90, Screen.width, 40, "BATERIA ESGOTADA", 32, new Color(0.9f, 0.3f, 0.25f), TextAnchor.MiddleCenter);
            // nomeia a causa (Ajuste do playtest)
            string causa = (Time.time - deathCauseTime < 3f) ? deathCause : "A energia se esgotou aos poucos.";
            Label(0, Screen.height / 2f - 40, Screen.width, 30, "O M-37 desligou. Causa: " + causa, 18, GameColors.Papel, TextAnchor.MiddleCenter);
            Label(0, Screen.height / 2f - 6, Screen.width, 26, "Aprenda a rota, use as coberturas e economize energia.", 15, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);

            float cx = Screen.width / 2f;
            if (Button(new Rect(cx - 230, Screen.height / 2f + 40, 220, 48), "TENTAR DE NOVO (R)")) RestartLevel();
            if (Button(new Rect(cx + 10, Screen.height / 2f + 40, 220, 48), "MENU (M)")) ReturnToMenu();
        }
    }
}
