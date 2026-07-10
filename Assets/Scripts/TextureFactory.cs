using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Texturas procedurais geradas em runtime (Etapa 8 — substituicao dos placeholders lisos
    /// por "arte"). Mantem a filosofia do projeto de NAO versionar assets binarios: tudo e
    /// pintado por codigo, seguindo a paleta da Etapa 5 (Aco Base, Ferrugem, Ambar, Acido).
    ///
    /// Cada metodo devolve uma Texture2D em modo Repeat, pronta para receber tiling por objeto
    /// (Renderer.material.mainTextureScale). As texturas sao deterministicas (System.Random com
    /// semente fixa) para o resultado ser identico em qualquer maquina.
    /// </summary>
    public static class TextureFactory
    {
        // -------------------------------------------------- helpers
        static Texture2D New(int size)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 2;
            return t;
        }

        static Color Jitter(Color c, System.Random rng, float amt)
        {
            float n = (float)(rng.NextDouble() * 2.0 - 1.0) * amt;
            return new Color(
                Mathf.Clamp01(c.r + n),
                Mathf.Clamp01(c.g + n),
                Mathf.Clamp01(c.b + n),
                c.a);
        }

        // -------------------------------------------------- piso: placas metalicas
        public static Texture2D Plates(Color baseC, int size = 256, int cells = 4)
        {
            Texture2D t = New(size);
            var rng = new System.Random(1001);
            Color line = baseC * 0.45f; line.a = 1f;
            Color rivet = baseC * 1.7f; rivet.a = 1f;
            int cell = size / cells;
            int seam = Mathf.Max(2, size / 110);

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Jitter(baseC, rng, 0.04f);

                    int mx = x % cell, my = y % cell;
                    bool onSeam = mx < seam || my < seam;
                    if (onSeam) c = Color.Lerp(c, line, 0.8f);

                    // rebites nos cantos das placas
                    float dx = Mathf.Min(mx, cell - mx);
                    float dy = Mathf.Min(my, cell - my);
                    if (dx < seam + 2 && dy < seam + 2) c = rivet;

                    px[y * size + x] = c;
                }
            }
            // alguns riscos diagonais claros
            AddScratches(px, size, rng, baseC * 1.3f, 14);
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- parede: paineis com costura vertical
        public static Texture2D Panel(Color baseC, int size = 256)
        {
            Texture2D t = New(size);
            var rng = new System.Random(2002);
            Color seamC = baseC * 0.5f; seamC.a = 1f;
            Color edge = baseC * 1.35f; edge.a = 1f;
            int panelW = size / 3;

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Jitter(baseC, rng, 0.05f);
                    int mx = x % panelW;
                    if (mx < 3) c = seamC;                 // costura vertical
                    else if (mx < 6) c = Color.Lerp(c, edge, 0.5f);
                    // friso horizontal de reforco
                    if (y % (size / 2) < 4) c = Color.Lerp(c, seamC, 0.7f);
                    px[y * size + x] = c;
                }
            }
            AddStains(px, size, rng, baseC * 0.6f, 8);
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- caixa: ferrugem
        public static Texture2D Rust(Color baseC, int size = 256)
        {
            Texture2D t = New(size);
            var rng = new System.Random(3003);
            Color rust = new Color(0.42f, 0.17f, 0.07f, 1f);
            Color dark = baseC * 0.55f; dark.a = 1f;

            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Jitter(baseC, rng, 0.07f);

            // manchas de ferrugem (discos de raio variado)
            AddStains(px, size, rng, rust, 26);
            AddStains(px, size, rng, dark, 10);
            // borda escura (chapa)
            int b = Mathf.Max(2, size / 32);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (x < b || y < b || x >= size - b || y >= size - b)
                        px[y * size + x] = Color.Lerp(px[y * size + x], dark, 0.7f);

            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- liquido corrosivo (acido)
        public static Texture2D Acid(Color baseC, int size = 128)
        {
            Texture2D t = New(size);
            var rng = new System.Random(4004);
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Jitter(baseC, rng, 0.08f);
            AddStains(px, size, rng, baseC * 1.6f, 30); // bolhas claras
            AddStains(px, size, rng, baseC * 0.5f, 14);
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- stains/scratches
        static void AddStains(Color[] px, int size, System.Random rng, Color c, int count)
        {
            for (int s = 0; s < count; s++)
            {
                int cx = rng.Next(size), cy = rng.Next(size);
                int r = 3 + rng.Next(size / 10);
                float strength = 0.3f + (float)rng.NextDouble() * 0.5f;
                for (int y = -r; y <= r; y++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        if (x * x + y * y > r * r) continue;
                        int px2 = ((cy + y + size) % size) * size + ((cx + x + size) % size);
                        float f = (1f - Mathf.Sqrt(x * x + y * y) / r) * strength;
                        px[px2] = Color.Lerp(px[px2], c, f);
                    }
                }
            }
        }

        static void AddScratches(Color[] px, int size, System.Random rng, Color c, int count)
        {
            for (int s = 0; s < count; s++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                int len = 10 + rng.Next(size / 3);
                int dx = rng.Next(2) == 0 ? 1 : -1;
                for (int i = 0; i < len; i++)
                {
                    int xx = (x + dx * i + size) % size;
                    int yy = (y + i + size) % size;
                    int idx = yy * size + xx;
                    px[idx] = Color.Lerp(px[idx], c, 0.4f);
                }
            }
        }

        // -------------------------------------------------- faixa de caminho (chevrons)
        // Setas apontando para +v (frente), para guiar o jogador pelo piso (pedido da Etapa 8).
        public static Texture2D PathArrows(Color baseC, Color accent, int size = 128)
        {
            Texture2D t = New(size);
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = baseC;

            int thick = Mathf.Max(4, size / 16);
            int half = size / 2;
            // chevron em "^": duas diagonais que se encontram no topo central
            for (int y = 0; y < size; y++)
            {
                // apice no topo (y grande); abre para baixo
                int dx = (size - 1 - y) / 2;
                for (int w = 0; w < thick; w++)
                {
                    int xl = half - dx + w;
                    int xr = half + dx - w;
                    if (xl >= 0 && xl < size) px[y * size + xl] = accent;
                    if (xr >= 0 && xr < size) px[y * size + xr] = accent;
                }
            }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- baforada de fumaça (alpha radial)
        public static Texture2D SmokePuff(int size = 64)
        {
            Texture2D t = New(size);
            t.wrapMode = TextureWrapMode.Clamp;
            var rng = new System.Random(6006);
            float c = (size - 1) * 0.5f;
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;                                   // borda suave
                    a *= 0.6f + (float)rng.NextDouble() * 0.4f;  // textura irregular
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- janela (vidro emissivo + caixilho)
        public static Texture2D Window(Color frame, Color glass, int size = 128)
        {
            Texture2D t = New(size);
            var rng = new System.Random(5005);
            Color[] px = new Color[size * size];
            int b = Mathf.Max(4, size / 14);   // espessura do caixilho
            int mull = Mathf.Max(3, size / 28); // travessas (cruz)
            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isFrame = x < b || y < b || x >= size - b || y >= size - b
                                   || Mathf.Abs(x - half) < mull || Mathf.Abs(y - half) < mull;
                    px[y * size + x] = isFrame ? frame : Jitter(glass, rng, 0.06f);
                }
            }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        // -------------------------------------------------- madeira (tábuas com veios)
        public static Texture2D Wood(int size = 256)
        {
            Texture2D t = New(size);
            var rng = new System.Random(7007);
            Color baseC = new Color(0.45f, 0.30f, 0.16f);
            Color dark = new Color(0.30f, 0.19f, 0.09f);
            int planks = 5;
            int ph = size / planks;
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                int row = y / ph;
                float off = (row * 37) % size; // desencontra os veios entre tábuas
                for (int x = 0; x < size; x++)
                {
                    Color c = Jitter(baseC, rng, 0.05f);
                    // veios horizontais (grão)
                    float grain = Mathf.Sin((x + off) * 0.08f) * 0.5f + Mathf.Sin((x + off) * 0.21f) * 0.25f;
                    c = Color.Lerp(c, dark, Mathf.Clamp01(grain * 0.4f + 0.2f) * 0.5f);
                    // junta entre tábuas
                    if (y % ph < 2) c = dark;
                    px[y * size + x] = c;
                }
            }
            t.SetPixels(px); t.Apply(true);
            return t;
        }
    }
}
