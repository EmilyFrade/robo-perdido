using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Audio do jogo, 100% sintetizado em runtime (Etapa 8 — som da demo sem versionar
    /// arquivos .wav/.mp3, coerente com a filosofia "tudo por codigo" do projeto).
    /// Todos os clips sao gerados com osciladores simples + ruido no Awake e tocados por
    /// eventos do gameplay (passos, empurrar, interagir, recarga, chave, alerta do drone,
    /// vitoria/derrota) alem de um zumbido industrial de ambiente em loop.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        const int SR = 44100;

        AudioSource sfx;      // PlayOneShot de eventos
        AudioSource steps;    // passos (nao empilha)
        AudioSource ambient;  // zumbido em loop
        AudioSource musicCalm;   // trilha calma (base)
        AudioSource musicTense;  // camada de tensao (sobe com a bateria baixa)
        AudioSource puzzleSfx;   // sons do minigame — tocam mesmo com o jogo silenciado (ignoreListenerPause)

        AudioClip step, stepRun, push, interact, cell, key, alert, win, lose, click, rotor, hiss, drip, gate, uiDrag, uiWrong;

        // Clips usados por AudioSources 3D nos objetos (drone / vaporizador / cano / portão).
        public AudioClip Rotor => rotor;
        public AudioClip Hiss => hiss;
        public AudioClip Drip => drip;
        public AudioClip Gate => gate;

        void Awake()
        {
            Instance = this;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false; sfx.spatialBlend = 0f; sfx.volume = 0.7f;
            steps = gameObject.AddComponent<AudioSource>();
            steps.playOnAwake = false; steps.spatialBlend = 0f; steps.volume = 0.45f;
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false; ambient.loop = true; ambient.spatialBlend = 0f; ambient.volume = 0.08f;

            step = NoiseHit(0.10f, 320f, 0.6f);
            stepRun = NoiseHit(0.08f, 520f, 0.85f);
            push = NoiseHit(0.22f, 140f, 0.5f);
            interact = Arp(new[] { 440f, 660f }, 0.16f, Wave.Square, 0.35f);
            cell = Sweep(420f, 980f, 0.45f, 0.4f);
            key = Arp(new[] { 660f, 880f, 1320f }, 0.30f, Wave.Sine, 0.4f);
            alert = Sweep(900f, 240f, 0.5f, 0.55f, Wave.Saw);
            win = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.6f, Wave.Sine, 0.45f);
            lose = Sweep(440f, 90f, 1.1f, 0.5f, Wave.Saw);
            click = NoiseHit(0.05f, 700f, 0.4f);

            ambient.clip = Hum(2.0f);
            ambient.Play();

            rotor = RotorClip(1.0f);
            hiss = HissClip(1.2f);
            drip = DripClip();
            gate = GateClip(1.4f);
            uiDrag = NoiseHit(0.06f, 520f, 0.35f);
            uiWrong = Sweep(320f, 150f, 0.24f, 0.4f, Wave.Square);

            // Sons do minigame: tocam mesmo quando o áudio do jogo está pausado (popup de puzzle).
            puzzleSfx = gameObject.AddComponent<AudioSource>();
            puzzleSfx.playOnAwake = false; puzzleSfx.spatialBlend = 0f; puzzleSfx.volume = 0.75f;
            puzzleSfx.ignoreListenerPause = true;

            // Trilha dinamica: duas camadas em loop com crossfade pela bateria.
            musicCalm = gameObject.AddComponent<AudioSource>();
            musicCalm.playOnAwake = false; musicCalm.loop = true; musicCalm.spatialBlend = 0f; musicCalm.volume = 0.42f;
            musicCalm.clip = MusicCalm(4f); musicCalm.Play();

            musicTense = gameObject.AddComponent<AudioSource>();
            musicTense.playOnAwake = false; musicTense.loop = true; musicTense.spatialBlend = 0f; musicTense.volume = 0.0f;
            musicTense.clip = MusicTense(2f); musicTense.Play();
        }

        // Crossfade da trilha conforme a energia: bateria cheia = calmo; bateria baixa = TENSO.
        void Update()
        {
            float pct = 1f;
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.battery != null && gm.IsPlaying) pct = gm.battery.Percent;
            float tension = Mathf.Clamp01(1f - pct);

            // música bem audível: calma sempre presente, tensa entra cedo e domina no fim.
            float calmTarget = 0.42f * (0.55f + 0.45f * pct);
            float tenseTarget = 0.6f * Mathf.Pow(tension, 1.05f);
            float k = Time.unscaledDeltaTime * 2.5f;
            if (musicCalm != null) musicCalm.volume = Mathf.Lerp(musicCalm.volume, calmTarget, k);
            if (musicTense != null)
            {
                musicTense.volume = Mathf.Lerp(musicTense.volume, tenseTarget, k);
                musicTense.pitch = Mathf.Lerp(musicTense.pitch, 1f + 0.22f * tension, k);
            }
        }

        // -------------------------------------------------- API de eventos
        public void Step(bool running) { if (steps != null) { steps.PlayOneShot(running ? stepRun : step); } }
        public void Push() { One(push, 0.5f); }
        public void Interact() { One(interact); }
        public void Cell() { One(cell); }
        public void Key() { One(key); }
        public void Alert() { One(alert); }
        public void Win() { One(win); }
        public void Lose() { One(lose); }
        public void Click() { One(click); }

        // ---- Sons do MINIGAME (ignoram a pausa do áudio do jogo) ----
        public void UiClick() { PuzzlePlay(click); }
        public void UiDrag() { PuzzlePlay(uiDrag, 0.5f); }
        public void UiConnect() { PuzzlePlay(interact); }
        public void UiWrong() { PuzzlePlay(uiWrong); }
        public void UiSolved() { PuzzlePlay(key); }
        void PuzzlePlay(AudioClip c, float vol = 1f) { if (puzzleSfx != null && c != null) puzzleSfx.PlayOneShot(c, vol); }

        void One(AudioClip c, float vol = 1f)
        {
            if (sfx != null && c != null) sfx.PlayOneShot(c, vol);
        }

        // -------------------------------------------------- sintese
        enum Wave { Sine, Square, Saw }

        static float Osc(Wave w, float phase)
        {
            switch (w)
            {
                case Wave.Square: return Mathf.Sin(phase) >= 0f ? 1f : -1f;
                case Wave.Saw: return Mathf.Repeat(phase / (2f * Mathf.PI), 1f) * 2f - 1f;
                default: return Mathf.Sin(phase);
            }
        }

        // tom puro com envelope ataque/decaimento
        AudioClip Tone(float freq, float dur, float vol, Wave w)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            float ph = 0f, step = 2f * Mathf.PI * freq / SR;
            for (int i = 0; i < n; i++)
            {
                float env = Env(i, n);
                d[i] = Osc(w, ph) * env * vol;
                ph += step;
            }
            return Make("tone", d);
        }

        // varredura de frequencia (f0 -> f1)
        AudioClip Sweep(float f0, float f1, float dur, float vol, Wave w = Wave.Sine)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float f = Mathf.Lerp(f0, f1, (float)i / n);
                ph += 2f * Mathf.PI * f / SR;
                d[i] = Osc(w, ph) * Env(i, n) * vol;
            }
            return Make("sweep", d);
        }

        // arpejo: sequencia de tons concatenados
        AudioClip Arp(float[] freqs, float dur, Wave w, float vol)
        {
            int total = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[total];
            int seg = total / freqs.Length;
            for (int k = 0; k < freqs.Length; k++)
            {
                float ph = 0f, step = 2f * Mathf.PI * freqs[k] / SR;
                int start = k * seg;
                for (int i = 0; i < seg && start + i < total; i++)
                {
                    d[start + i] = Osc(w, ph) * Env(i, seg) * vol;
                    ph += step;
                }
            }
            return Make("arp", d);
        }

        // batida de ruido (passo/empurrar) com corte grave -> som metalico abafado
        AudioClip NoiseHit(float dur, float toneFreq, float vol)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            var rng = new System.Random(7);
            float ph = 0f, step = 2f * Mathf.PI * toneFreq / SR;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float lp = Mathf.Lerp(prev, noise, 0.35f); // passa-baixa simples
                prev = lp;
                float env = Mathf.Pow(1f - (float)i / n, 2f); // decaimento rapido
                d[i] = (lp * 0.7f + Mathf.Sin(ph) * 0.3f) * env * vol;
                ph += step;
            }
            return Make("hit", d);
        }

        // zumbido industrial em loop (fundamental grave + harmonico + leve ruido)
        AudioClip Hum(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            var rng = new System.Random(11);
            // frequencias inteiras no comprimento -> loop sem clique
            float baseF = Mathf.Round(58f * dur) / dur;
            float h2 = Mathf.Round(116f * dur) / dur;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float ph1 = 2f * Mathf.PI * baseF * i / SR;
                float ph2 = 2f * Mathf.PI * h2 * i / SR;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, noise, 0.04f);
                float lfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.3f * i / SR);
                d[i] = (Mathf.Sin(ph1) * 0.6f + Mathf.Sin(ph2) * 0.2f + prev * 0.25f) * lfo * 0.5f;
            }
            return Make("hum", d);
        }

        // Snap de frequência para um número inteiro de ciclos no loop (evita clique na emenda).
        static float Snap(float f, float dur) { float c = Mathf.Round(f * dur); if (c < 1f) c = 1f; return c / dur; }

        // Trilha CALMA: arpejo melódico (lá menor) sobre um baixo suave. Frequências "snapadas"
        // para o loop fechar sem clique; envelope por nota evita estalos nas trocas.
        AudioClip MusicCalm(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            float[] notes = { 220f, 261.63f, 329.63f, 440f, 329.63f, 261.63f, 220f, 196f };
            for (int k = 0; k < notes.Length; k++) notes[k] = Snap(notes[k], dur);
            float bass = Snap(110f, dur);
            int seg = Mathf.Max(1, n / notes.Length);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                int ni = Mathf.Min(notes.Length - 1, i / seg);
                float env = Env(i % seg, seg);
                float melody = Mathf.Sin(2f * Mathf.PI * notes[ni] * t) * env * 0.45f;
                float low = (Mathf.Sin(2f * Mathf.PI * bass * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * bass * 2f * t)) * 0.22f;
                d[i] = (melody + low) * 0.6f;
            }
            return Make("musicCalm", d);
        }

        // Trilha TENSA: arpejo mais rápido com dissonância + batida grave tipo coração.
        AudioClip MusicTense(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            float[] notes = { 220f, 233.08f, 220f, 293.66f, 220f, 311.13f, 246.94f, 220f };
            for (int k = 0; k < notes.Length; k++) notes[k] = Snap(notes[k], dur);
            float bass = Snap(110f, dur), beat = Snap(4f, dur);
            int seg = Mathf.Max(1, n / notes.Length);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                int ni = Mathf.Min(notes.Length - 1, i / seg);
                float env = Env(i % seg, seg);
                float melody = OscSquareSoft(notes[ni] * t) * env * 0.32f;
                float pulse = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * beat * t - Mathf.PI / 2f), 3f);
                float heart = Mathf.Sin(2f * Mathf.PI * bass * t) * (0.25f + 0.55f * pulse);
                d[i] = (melody + heart) * 0.55f;
            }
            return Make("musicTense", d);
        }

        // onda quase-quadrada suave (mais "tensa" que o seno), por harmônicos ímpares.
        static float OscSquareSoft(float ftimes_t)
        {
            float p = 2f * Mathf.PI * ftimes_t;
            return Mathf.Sin(p) + 0.33f * Mathf.Sin(3f * p) + 0.2f * Mathf.Sin(5f * p);
        }

        // Rotor de helicóptero: portadora grave + ruído, modulados pela passagem das pás.
        AudioClip RotorClip(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            var rng = new System.Random(99);
            float carrier = Snap(95f, dur), chop = Snap(14f, dur);
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float chopEnv = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * chop * t), 4f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, noise, 0.5f);
                float body = Mathf.Sin(2f * Mathf.PI * carrier * t) * 0.6f + prev * 0.4f;
                d[i] = body * chopEnv * 0.8f;
            }
            return Make("rotor", d);
        }

        // Chiado de vapor (ruído passa-alta em loop) — som de fumaça saindo do vaporizador.
        AudioClip HissClip(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            var rng = new System.Random(55);
            float prev = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = noise - prev; prev = noise;     // passa-alta = "chhh"
                lp = Mathf.Lerp(lp, hp, 0.7f);             // suaviza um pouco o agudo
                d[i] = lp * 0.6f;
            }
            return Make("hiss", d);
        }

        // Portão abrindo: rangido metálico (rumor grave + ruído moído + chiado intermitente).
        AudioClip GateClip(float dur)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            var rng = new System.Random(33);
            float prev = 0f, ph1 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                ph1 += 2f * Mathf.PI * 68f / SR;
                float rumble = Mathf.Sin(ph1) * 0.4f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, noise, 0.22f);          // ruído moído (passa-baixa)
                float grind = prev * 0.55f;
                float squeak = Mathf.Sin(2f * Mathf.PI * 850f * t) * 0.12f * Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 2.5f * t));
                float atk = Mathf.Min(1f, i / (0.06f * n));
                float rel = Mathf.Min(1f, (n - i) / (0.22f * n));
                d[i] = (rumble + grind + squeak) * atk * rel;
            }
            return Make("gate", d);
        }

        // Pingo d'água: blip senoidal que cai de tom, decaimento rápido.
        AudioClip DripClip()
        {
            float dur = 0.18f;
            int n = Mathf.Max(1, (int)(SR * dur));
            float[] d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float f = Mathf.Lerp(1500f, 580f, (float)i / n);
                ph += 2f * Mathf.PI * f / SR;
                float env = Mathf.Pow(1f - (float)i / n, 3f);
                d[i] = Mathf.Sin(ph) * env * 0.5f;
            }
            return Make("drip", d);
        }

        static float Env(int i, int n)
        {
            int atk = Mathf.Max(1, n / 20);
            int rel = Mathf.Max(1, n / 4);
            if (i < atk) return (float)i / atk;
            if (i > n - rel) return (float)(n - i) / rel;
            return 1f;
        }

        AudioClip Make(string name, float[] data)
        {
            AudioClip c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
