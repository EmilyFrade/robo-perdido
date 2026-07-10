using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Constroi em RUNTIME o setor de GameManager.currentLevel (0..3). Cada setor é um MAPA
    /// NÃO-LINEAR de SALAS numa grade: o caminho vira (esquerda/direita) e há rampas. Cada sala
    /// tem uma PORTA liberada por um PUZZLE (botão, ligar cabos, múltipla escolha, limpar poeira)
    /// e a sala final tem a CHAVE + o portão final (que só abre com a chave). Tudo é gerado por
    /// código (geometria, arte, props, som), sem assets binários.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        const float CELL = 12f, H = 5f, WY = 2.5f, T = 0.5f, DW = 3.6f;
        static readonly Vector2Int[] DIRS = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };

        Material matFloor, matWall, matCrate, matDoor, matPipe, matBelt, matMetal;
        Material matRobo, matRoboDark, matRoboHead;
        Material matAmber, matAcid, matCheckpoint, matGlass, matPath, matHeat, matData, matSmoke, matGold;
        Material matWood, matLed, matBattery, matBolt, matRed, matGate, matWater, matPuddle, matRoboRust;

        Color regFloor, regWall, regAccent, regAccentBright, regGlass, regLampColor, regFog, regAmbient;
        float regLampIntensity, regFogDensity;

        int level;
        GameManager gm;
        Transform player;
        BatterySystem battery;

        void Start()
        {
            ApplyDebugLevelArg();
            level = Mathf.Clamp(GameManager.currentLevel, 0, GameManager.TotalLevels - 1);
            SetRegion(level);
            BuildMaterials();

            gameObject.AddComponent<SoundManager>();
            gm = gameObject.AddComponent<GameManager>();
            gm.sectorName = GameManager.SectorNameFor(level);

            Vector2Int[] path = PathFor(level);
            Vector2Int firstStep = path[1] - path[0];
            Vector3 face = new Vector3(firstStep.x, 0f, firstStep.y);
            Vector3 spawn = CellCenter(path[0]) - face * 3.2f + Vector3.up * 1f;

            player = BuildPlayer(spawn, face);
            Camera cam = BuildCamera(player);
            player.GetComponent<RobotController>().SetCamera(cam.transform);
            battery = player.GetComponent<BatterySystem>();
            gm.battery = battery;

            BuildLevel(path);
            BuildLighting(path);
            BuildMist(path);
        }

        void ApplyDebugLevelArg()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-rp_level" && int.TryParse(args[i + 1], out int n))
                    GameManager.currentLevel = Mathf.Clamp(n, 0, GameManager.TotalLevels - 1);
        }

        // ============================================================ paths (mapas não-retos)
        Vector2Int[] PathFor(int lv)
        {
            switch (lv)
            {
                case 0: return new[] { C(0, 0), C(0, 1), C(1, 1), C(1, 2), C(2, 2) }; // N,E,N,E
                case 1: return new[] { C(0, 0), C(1, 0), C(1, 1), C(2, 1), C(2, 2) }; // E,N,E,N
                case 2: return new[] { C(0, 0), C(0, 1), C(0, 2), C(1, 2), C(1, 3) }; // N,N,E,N
                default: return new[] { C(0, 0), C(1, 0), C(1, -1), C(2, -1), C(2, 0) }; // E,S,E,N
            }
        }
        static Vector2Int C(int x, int z) => new Vector2Int(x, z);
        Vector3 CellCenter(Vector2Int c) => new Vector3(c.x * CELL, 0f, c.y * CELL);
        static bool Contains(Vector2Int[] a, Vector2Int v) { foreach (var x in a) if (x == v) return true; return false; }
        static bool Lower(Vector2Int a, Vector2Int b) => a.x < b.x || (a.x == b.x && a.y < b.y);

        // ============================================================ regiao / materiais
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        void SetRegion(int lv)
        {
            switch (lv)
            {
                case 0:
                    regFloor = Hex("#1E2A35"); regWall = Hex("#33414D"); regAccent = Hex("#8B3A1A");
                    regAccentBright = Hex("#C25A2A"); regGlass = Hex("#3A6E8C");
                    regLampColor = new Color(1f, 0.88f, 0.72f); regLampIntensity = 1.5f;
                    regFog = Hex("#1E2A35"); regFogDensity = 0.03f; regAmbient = new Color(0.08f, 0.09f, 0.11f);
                    break;
                case 1:
                    regFloor = Hex("#241A14"); regWall = Hex("#3D2F26"); regAccent = Hex("#8B3A1A");
                    regAccentBright = Hex("#E08A3A"); regGlass = Hex("#C2662A");
                    regLampColor = new Color(1f, 0.7f, 0.45f); regLampIntensity = 1.4f;
                    regFog = Hex("#2A1C12"); regFogDensity = 0.034f; regAmbient = new Color(0.11f, 0.08f, 0.05f);
                    break;
                case 2:
                    regFloor = Hex("#15201A"); regWall = Hex("#26332B"); regAccent = Hex("#4B7C3F");
                    regAccentBright = Hex("#6FB04F"); regGlass = Hex("#3F7C54");
                    regLampColor = new Color(0.6f, 0.85f, 0.6f); regLampIntensity = 0.9f;
                    regFog = Hex("#101810"); regFogDensity = 0.052f; regAmbient = new Color(0.05f, 0.07f, 0.05f);
                    break;
                default:
                    regFloor = Hex("#141A26"); regWall = Hex("#27303F"); regAccent = Hex("#2A7C8B");
                    regAccentBright = Hex("#38C0D0"); regGlass = Hex("#2A7C8B");
                    regLampColor = new Color(0.7f, 0.85f, 1f); regLampIntensity = 1.4f;
                    regFog = Hex("#0F1626"); regFogDensity = 0.04f; regAmbient = new Color(0.06f, 0.08f, 0.12f);
                    break;
            }
        }

        Material NewMat(Color c, bool emissive = false)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            m.color = c;
            if (emissive && m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f); }
            return m;
        }

        void SetMetal(Material m, float metallic, float smooth)
        {
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        }

        Material NewTransparent(Color c)
        {
            Shader sh = Shader.Find("Standard");
            Material m = new Material(sh != null ? sh : Shader.Find("Sprites/Default"));
            if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
            }
            m.color = c;
            return m;
        }

        void BuildMaterials()
        {
            matFloor = NewMat(regFloor); matFloor.mainTexture = TextureFactory.Plates(regFloor); SetMetal(matFloor, 0.6f, 0.35f);
            matWall = NewMat(regWall); matWall.mainTexture = TextureFactory.Panel(regWall); SetMetal(matWall, 0.4f, 0.25f);
            matCrate = NewMat(regAccent);
            matCrate.mainTexture = level <= 1 ? TextureFactory.Rust(regAccent) : TextureFactory.Panel(regAccent);
            SetMetal(matCrate, 0.25f, 0.2f);
            matDoor = NewMat(regWall * 0.8f); SetMetal(matDoor, 0.8f, 0.4f);
            matPipe = NewMat(regWall * 1.25f); SetMetal(matPipe, 0.9f, 0.6f);
            matBelt = NewMat(regWall * 0.5f); matBelt.mainTexture = TextureFactory.Panel(regWall * 0.5f); SetMetal(matBelt, 0.6f, 0.3f);
            matMetal = NewMat(GameColors.Robo * 0.9f); SetMetal(matMetal, 0.8f, 0.5f);

            matRobo = NewMat(GameColors.Robo); SetMetal(matRobo, 0.7f, 0.55f);
            matRoboDark = NewMat(GameColors.Robo * 0.45f); SetMetal(matRoboDark, 0.8f, 0.5f);
            matRoboHead = NewMat(GameColors.Robo * 1.15f); SetMetal(matRoboHead, 0.7f, 0.6f);

            matAmber = NewMat(GameColors.Ambar, true);
            matAcid = NewMat(GameColors.Acido, true); matAcid.mainTexture = TextureFactory.Acid(GameColors.Acido);
            matCheckpoint = NewMat(GameColors.Ambar, true);
            matGlass = NewMat(regGlass, true); matGlass.mainTexture = TextureFactory.Window(regWall * 0.6f, regGlass);
            if (matGlass.HasProperty("_EmissionColor")) matGlass.SetColor("_EmissionColor", regGlass * 0.9f);
            matPath = NewMat(regFloor * 0.7f, true); matPath.mainTexture = TextureFactory.PathArrows(regFloor * 0.6f, regAccentBright);
            if (matPath.HasProperty("_EmissionColor")) matPath.SetColor("_EmissionColor", regAccentBright * 0.5f);
            matHeat = NewMat(Hex("#E0631A"), true);
            matData = NewMat(regAccentBright, true);
            matGold = NewMat(Hex("#E5B73B"), true); SetMetal(matGold, 1f, 0.85f);
            if (matGold.HasProperty("_EmissionColor")) matGold.SetColor("_EmissionColor", Hex("#4A3408"));

            matWood = NewMat(Hex("#73502A")); matWood.mainTexture = TextureFactory.Wood(); SetMetal(matWood, 0.0f, 0.15f);
            matLed = NewMat(Hex("#FFF3D6"), true); if (matLed.HasProperty("_EmissionColor")) matLed.SetColor("_EmissionColor", new Color(1.5f, 1.4f, 1.15f));
            matBattery = NewMat(Hex("#E8C21E"), true); SetMetal(matBattery, 0.3f, 0.5f); if (matBattery.HasProperty("_EmissionColor")) matBattery.SetColor("_EmissionColor", Hex("#5A4A06"));
            matBolt = NewMat(Hex("#1A1A20"));
            matRed = NewMat(Hex("#B5281E")); SetMetal(matRed, 0.2f, 0.4f);
            matGate = NewMat(Hex("#0D0D11")); SetMetal(matGate, 0.7f, 0.5f);
            matWater = NewTransparent(new Color(0.35f, 0.55f, 0.72f, 0.55f)); SetMetal(matWater, 0.1f, 0.9f);
            matPuddle = NewTransparent(new Color(0.30f, 0.6f, 0.26f, 0.85f)); SetMetal(matPuddle, 0.1f, 0.95f);
            matPuddle.mainTexture = TextureFactory.Acid(GameColors.Acido);
            if (matPuddle.HasProperty("_EmissionColor")) { matPuddle.EnableKeyword("_EMISSION"); matPuddle.SetColor("_EmissionColor", GameColors.Acido * 0.7f); }
            // robô enferrujado: cinza-metal com manchas de ferrugem
            matRoboRust = NewMat(GameColors.Robo * 0.95f); matRoboRust.mainTexture = TextureFactory.Rust(GameColors.Robo * 0.8f); SetMetal(matRoboRust, 0.55f, 0.35f);

            Shader psh = Shader.Find("Sprites/Default");
            if (psh == null) psh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (psh == null) psh = Shader.Find("Standard");
            matSmoke = new Material(psh);
            matSmoke.mainTexture = TextureFactory.SmokePuff();
        }

        // ============================================================ primitivas
        GameObject Box(string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = center; go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }
        GameObject Sphere(string name, Vector3 center, float d, Material mat, bool trigger = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name; go.transform.position = center; go.transform.localScale = Vector3.one * d;
            go.GetComponent<Renderer>().material = mat;
            if (trigger) go.GetComponent<Collider>().isTrigger = true;
            return go;
        }
        GameObject Cyl(string name, Vector3 center, Vector3 scale, Material mat, bool collider = false, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.position = center; go.transform.localScale = scale; go.transform.rotation = Quaternion.Euler(euler);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) { Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c); }
            return go;
        }
        GameObject TriggerZone(string name, Vector3 center, Vector3 size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = center; go.transform.localScale = size;
            Destroy(go.GetComponent<Renderer>());
            go.GetComponent<Collider>().isTrigger = true;
            return go;
        }
        GameObject Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            go.transform.SetParent(parent); go.transform.localPosition = localPos; go.transform.localScale = scale; go.transform.localRotation = Quaternion.identity;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
        void TileFloor(GameObject go, float t = 0.5f) { Vector3 s = go.transform.localScale; go.GetComponent<Renderer>().material.mainTextureScale = new Vector2(s.x * t, s.z * t); }
        void TileWall(GameObject go, float t = 0.4f) { Vector3 s = go.transform.localScale; go.GetComponent<Renderer>().material.mainTextureScale = new Vector2(Mathf.Max(s.x, s.z) * t, s.y * t); }
        void TileBox(GameObject go, float t = 0.6f) { Vector3 s = go.transform.localScale; go.GetComponent<Renderer>().material.mainTextureScale = new Vector2(s.x * t, s.y * t); }

        // ============================================================ player / camera / luz
        Transform BuildPlayer(Vector3 spawn, Vector3 face)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "M-37"; p.transform.position = spawn;
            if (face.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.LookRotation(face);
            Destroy(p.GetComponent<Collider>());
            p.GetComponent<Renderer>().enabled = false;

            CharacterController cc = p.AddComponent<CharacterController>();
            cc.height = 1.0f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.skinWidth = 0.04f;
            p.AddComponent<BatterySystem>();
            p.AddComponent<RobotController>();

            Transform b = p.transform;
            // base + esteiras
            Part(b, PrimitiveType.Cube, new Vector3(0f, -0.34f, 0f), new Vector3(0.82f, 0.26f, 0.92f), matRoboDark);
            Part(b, PrimitiveType.Cylinder, new Vector3(-0.42f, -0.32f, 0f), new Vector3(0.22f, 0.42f, 0.22f), matRoboDark).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(b, PrimitiveType.Cylinder, new Vector3(0.42f, -0.32f, 0f), new Vector3(0.22f, 0.42f, 0.22f), matRoboDark).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // chassi ENFERRUJADO + emendas (soldas) escuras + rebites
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.68f, 0.55f, 0.68f), matRoboRust);
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.345f), new Vector3(0.7f, 0.045f, 0.02f), matBolt);
            Part(b, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0.345f), new Vector3(0.7f, 0.045f, 0.02f), matBolt);
            Part(b, PrimitiveType.Cube, new Vector3(0.345f, 0.02f, 0f), new Vector3(0.02f, 0.52f, 0.66f), matBolt);
            foreach (Vector3 rv in new[] { new Vector3(-0.28f, 0.27f, 0.35f), new Vector3(0.28f, 0.27f, 0.35f), new Vector3(-0.28f, -0.22f, 0.35f), new Vector3(0.28f, -0.22f, 0.35f) })
                Part(b, PrimitiveType.Sphere, rv, new Vector3(0.07f, 0.07f, 0.05f), matBolt);

            // mochila de bateria (ferrugem) + indicador âmbar
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.42f), new Vector3(0.4f, 0.42f, 0.16f), matCrate);
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.51f), new Vector3(0.12f, 0.22f, 0.04f), matAmber);

            // cabeça enferrujada + viseira escura
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.02f), new Vector3(0.46f, 0.34f, 0.44f), matRoboRust);
            Part(b, PrimitiveType.Cube, new Vector3(0f, 0.47f, 0.22f), new Vector3(0.44f, 0.16f, 0.05f), matBolt);

            // DOIS olhos vermelhos brilhantes (com luz)
            Material eyeMat = NewMat(Hex("#FF2A1E"), true);
            foreach (int sx in new[] { -1, 1 })
            {
                GameObject eye = Sphere("Eye", Vector3.zero, 0.13f, eyeMat);
                Destroy(eye.GetComponent<Collider>()); eye.transform.SetParent(b);
                eye.transform.localPosition = new Vector3(0.12f * sx, 0.47f, 0.25f);
                Light eg = eye.AddComponent<Light>(); eg.type = LightType.Point; eg.range = 3.5f; eg.intensity = 1.1f; eg.color = new Color(1f, 0.2f, 0.12f);
            }

            // antena + ponta âmbar
            Part(b, PrimitiveType.Cylinder, new Vector3(0.14f, 0.74f, -0.1f), new Vector3(0.05f, 0.22f, 0.05f), matRoboDark);
            GameObject tip = Sphere("AntennaTip", Vector3.zero, 0.1f, matAmber); Destroy(tip.GetComponent<Collider>());
            tip.transform.SetParent(b); tip.transform.localPosition = new Vector3(0.14f, 0.95f, -0.1f);
            return p.transform;
        }

        Camera BuildCamera(Transform target)
        {
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 62f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = regFog;
            camGo.AddComponent<AudioListener>();
            CameraFollow follow = camGo.AddComponent<CameraFollow>();
            follow.target = target; camGo.transform.position = target.position + follow.offset;
            return cam;
        }

        void BuildLighting(Vector2Int[] path)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = regFog; RenderSettings.fogDensity = regFogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = regAmbient;

            GameObject sun = new GameObject("Sun");
            Light l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.color = regLampColor; l.intensity = 0.18f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Vector2Int exitDir = path[path.Length - 1] - path[path.Length - 2];
            Vector2Int chamber = path[path.Length - 1] + exitDir;
            foreach (Vector2Int c in path) Lamp(CellCenter(c));
            Lamp(CellCenter(chamber));
        }

        void Lamp(Vector3 c)
        {
            GameObject g = new GameObject("CeilLamp"); g.transform.position = new Vector3(c.x, 4.3f, c.z);
            Light pl = g.AddComponent<Light>(); pl.type = LightType.Point; pl.range = 15f; pl.intensity = regLampIntensity; pl.color = regLampColor;
            // bastões de LED (tubos), em vez de luminária quadrada
            Box("LedHousing", new Vector3(c.x, 4.88f, c.z), new Vector3(1.0f, 0.1f, 4.2f), matRoboDark, false).transform.SetParent(g.transform);
            Box("LedBar", new Vector3(c.x - 0.32f, 4.8f, c.z), new Vector3(0.16f, 0.08f, 3.9f), matLed, false).transform.SetParent(g.transform);
            Box("LedBar", new Vector3(c.x + 0.32f, 4.8f, c.z), new Vector3(0.16f, 0.08f, 3.9f), matLed, false).transform.SetParent(g.transform);
        }

        // Névoa baixa cobrindo o mapa (indústria fechada): partículas grandes, lentas e bem
        // tênues, à deriva perto do chão, somadas ao fog volumétrico da cena.
        void BuildMist(Vector2Int[] path)
        {
            int minX = 9999, maxX = -9999, minZ = 9999, maxZ = -9999;
            Vector2Int exitDir = path[path.Length - 1] - path[path.Length - 2];
            Vector2Int chamber = path[path.Length - 1] + exitDir;
            foreach (Vector2Int c in path) { minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x); minZ = Mathf.Min(minZ, c.y); maxZ = Mathf.Max(maxZ, c.y); }
            minX = Mathf.Min(minX, chamber.x); maxX = Mathf.Max(maxX, chamber.x); minZ = Mathf.Min(minZ, chamber.y); maxZ = Mathf.Max(maxZ, chamber.y);

            float cx = (minX + maxX) * 0.5f * CELL, cz = (minZ + maxZ) * 0.5f * CELL;
            float w = (maxX - minX + 1) * CELL, dpth = (maxZ - minZ + 1) * CELL;

            GameObject g = new GameObject("Mist");
            g.transform.position = new Vector3(cx, 3.0f, cz);   // mais alto, à meia-altura/teto
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 18f; main.startSpeed = 0.2f; main.startSize = 8.5f;
            main.startColor = new Color(Mathf.Clamp01(regFog.r * 1.5f + 0.12f), Mathf.Clamp01(regFog.g * 1.5f + 0.12f), Mathf.Clamp01(regFog.b * 1.6f + 0.14f), 0.16f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.maxParticles = 320; main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule em = ps.emission; em.rateOverTime = 26f;
            ParticleSystem.ShapeModule sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(w, 4.2f, dpth);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime; col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            // os três eixos precisam estar no MESMO modo (TwoConstants), senão o Unity avisa todo frame
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.04f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            ParticleSystemRenderer rend = g.GetComponent<ParticleSystemRenderer>();
            rend.material = matSmoke; rend.renderMode = ParticleSystemRenderMode.Billboard; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        // ============================================================ nível (salas + portas + puzzles)
        void BuildLevel(Vector2Int[] path)
        {
            int N = path.Length;
            Vector2Int exitDir = path[N - 1] - path[N - 2];

            for (int i = 0; i < N; i++)
            {
                Vector2Int extraOpen = (i == N - 1) ? exitDir : Vector2Int.zero;
                BuildRoomShell(path, i, extraOpen);
                RoomProps(path, i);
            }

            var cycle = new[] { PuzzleStation.PuzzleType.Button, PuzzleStation.PuzzleType.Cables, PuzzleStation.PuzzleType.MultiChoice, PuzzleStation.PuzzleType.CleanDust };
            for (int i = 0; i < N - 1; i++)
            {
                SlidingDoor door = BuildDoorBarrier(path[i], path[i + 1]);
                PuzzleStation ps = BuildPuzzleStation(path[i], path[i + 1], cycle[i % cycle.Length], i);
                ps.door = door;
                FloorGuide(path[i], path[i + 1]);
            }

            // sala final: chave + portão (KeyGate) + câmara de saída
            Vector2Int last = path[N - 1];
            Vector2Int chamber = last + exitDir;
            KeyAt(CellCenter(last) + new Vector3(2.6f, 0.8f, -2.6f));
            SlidingDoor finalDoor = BuildDoorBarrier(last, chamber);
            PuzzleStation kg = BuildPuzzleStation(last, chamber, PuzzleStation.PuzzleType.KeyGate, N - 1);
            kg.door = finalDoor;
            FloorGuide(last, chamber);
            BuildChamber(chamber, exitDir);
        }

        void BuildRoomShell(Vector2Int[] path, int i, Vector2Int extraOpen)
        {
            Vector2Int c = path[i];
            Vector3 ctr = CellCenter(c);
            GameObject f = Box("Floor", new Vector3(ctr.x, -0.05f, ctr.z), new Vector3(CELL, 0.1f, CELL), matFloor); TileFloor(f);
            GameObject cl = Box("Ceiling", new Vector3(ctr.x, H + 0.05f, ctr.z), new Vector3(CELL, 0.2f, CELL), matWall); TileFloor(cl, 0.4f);

            foreach (Vector2Int dir in DIRS)
            {
                Vector2Int nb = c + dir;
                bool passage = (i > 0 && path[i - 1] == nb) || (i < path.Length - 1 && path[i + 1] == nb) || (dir == extraOpen);
                if (passage)
                {
                    bool owner = (dir == extraOpen) || Lower(c, nb);
                    if (owner) WallSide(c, dir, true);
                }
                else
                {
                    if (!Contains(path, nb) || Lower(c, nb)) WallSide(c, dir, false);
                }
            }

            // janelas decorativas em uma parede externa sem passagem
            MaybeWindow(path, c);
        }

        void WallSide(Vector2Int c, Vector2Int dir, bool gap)
        {
            Vector3 ctr = CellCenter(c);
            float edgeX = ctr.x + dir.x * (CELL / 2f);
            float edgeZ = ctr.z + dir.y * (CELL / 2f);
            if (dir.y != 0)
            {
                if (!gap) { TileWall(Box("Wall", new Vector3(ctr.x, WY, edgeZ), new Vector3(CELL, H, T), matWall)); }
                else
                {
                    float gl = ctr.x - DW / 2f, gr = ctr.x + DW / 2f, cl = ctr.x - CELL / 2f, cr = ctr.x + CELL / 2f;
                    TileWall(Box("Wall", new Vector3((cl + gl) / 2f, WY, edgeZ), new Vector3(gl - cl, H, T), matWall));
                    TileWall(Box("Wall", new Vector3((gr + cr) / 2f, WY, edgeZ), new Vector3(cr - gr, H, T), matWall));
                    Box("Lintel", new Vector3(ctr.x, H - 0.5f, edgeZ), new Vector3(DW, 1f, T), matWall, false);
                }
            }
            else
            {
                if (!gap) { TileWall(Box("Wall", new Vector3(edgeX, WY, ctr.z), new Vector3(T, H, CELL), matWall)); }
                else
                {
                    float gb = ctr.z - DW / 2f, gt = ctr.z + DW / 2f, cb = ctr.z - CELL / 2f, ct = ctr.z + CELL / 2f;
                    TileWall(Box("Wall", new Vector3(edgeX, WY, (cb + gb) / 2f), new Vector3(T, H, gb - cb), matWall));
                    TileWall(Box("Wall", new Vector3(edgeX, WY, (gt + ct) / 2f), new Vector3(T, H, ct - gt), matWall));
                    Box("Lintel", new Vector3(edgeX, H - 0.5f, ctr.z), new Vector3(T, 1f, DW), matWall, false);
                }
            }
        }

        void MaybeWindow(Vector2Int[] path, Vector2Int c)
        {
            // janela na parede Oeste se não houver passagem por lá (estética)
            Vector2Int dir = new Vector2Int(-1, 0);
            if (Contains(path, c + dir)) return;
            Vector3 ctr = CellCenter(c);
            float edgeX = ctr.x - CELL / 2f + 0.28f;
            GameObject win = Box("Window", new Vector3(edgeX, 2.8f, ctr.z), new Vector3(0.08f, 1.7f, 3.0f), matGlass, false);
            win.GetComponent<Renderer>().material.mainTextureScale = Vector2.one;
            GameObject g = new GameObject("WinGlow"); g.transform.position = new Vector3(edgeX + 0.5f, 2.8f, ctr.z);
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.intensity = 0.5f; l.color = regGlass;
        }

        SlidingDoor BuildDoorBarrier(Vector2Int a, Vector2Int b)
        {
            Vector2Int dir = b - a;
            Vector3 ac = CellCenter(a);
            float edgeX = ac.x + dir.x * (CELL / 2f);
            float edgeZ = ac.z + dir.y * (CELL / 2f);
            bool alongX = dir.x != 0;
            float w = DW - 0.1f;
            Vector3 pos = new Vector3(edgeX, WY, edgeZ);

            GameObject root = new GameObject("RoomDoor");
            root.transform.position = pos;
            BoxCollider bc = root.AddComponent<BoxCollider>();
            bc.size = alongX ? new Vector3(T, H, w) : new Vector3(w, H, T);
            SlidingDoor sd = root.AddComponent<SlidingDoor>(); sd.openOffset = new Vector3(0f, -(H + 0.5f), 0f);

            // PORTÃO DE GRADES PRETAS: moldura + barras verticais.
            Vector3 across = alongX ? Vector3.forward : Vector3.right;
            Vector3 railSize = alongX ? new Vector3(T * 0.7f, 0.22f, w) : new Vector3(w, 0.22f, T * 0.7f);
            GateBar(root.transform, pos + Vector3.up * (H / 2f - 0.12f), railSize);
            GateBar(root.transform, pos - Vector3.up * (H / 2f - 0.12f), railSize);
            Vector3 postSize = alongX ? new Vector3(T * 0.7f, H, 0.14f) : new Vector3(0.14f, H, T * 0.7f);
            GateBar(root.transform, pos + across * (w / 2f - 0.06f), postSize);
            GateBar(root.transform, pos - across * (w / 2f - 0.06f), postSize);
            int bars = 6;
            for (int i = 0; i < bars; i++)
            {
                float u = -w / 2f + (i + 0.5f) / bars * w;
                GateBar(root.transform, pos + across * u, new Vector3(0.1f, H - 0.4f, 0.1f));
            }
            GateBar(root.transform, pos, alongX ? new Vector3(T * 0.6f, 0.14f, w) : new Vector3(w, 0.14f, T * 0.6f));
            return sd;
        }

        void GateBar(Transform parent, Vector3 worldPos, Vector3 size)
        {
            Box("GateBar", worldPos, size, matGate, false).transform.SetParent(parent, true);
        }

        PuzzleStation BuildPuzzleStation(Vector2Int a, Vector2Int b, PuzzleStation.PuzzleType type, int idx)
        {
            Vector2Int dir = b - a;
            Vector3 ac = CellCenter(a);
            Vector3 toDoor = new Vector3(dir.x, 0f, dir.y);
            Vector3 perp = new Vector3(-dir.y, 0f, dir.x);
            Vector3 pos = ac + toDoor * (CELL / 2f - 1.8f) + perp * 2.8f;

            GameObject console = Box("PuzzleConsole", new Vector3(pos.x, 1.0f, pos.z), new Vector3(1.0f, 1.2f, 0.7f), matMetal);
            if (type == PuzzleStation.PuzzleType.Button)
            {
                // painel de controle com botões coloridos (o verde abre a porta), embutido na
                // face do console (sem sobressair acima dele).
                Vector3 face = new Vector3(pos.x, 1.15f, pos.z) - toDoor * 0.37f;
                Vector3 panelSize = toDoor.x != 0 ? new Vector3(0.06f, 0.5f, 0.85f) : new Vector3(0.85f, 0.5f, 0.06f);
                Vector3 btnSize = toDoor.x != 0 ? new Vector3(0.08f, 0.13f, 0.13f) : new Vector3(0.13f, 0.13f, 0.08f);
                Box("PanelFace", face, panelSize, matRoboDark, false);
                Color[] cols = { Hex("#C8352B"), Hex("#2E6FB0"), Hex("#D9A21A"), Hex("#37A24A") };
                for (int i = 0; i < cols.Length; i++)
                    Box("PanelButton", face + perp * ((i - 1.5f) * 0.19f) - toDoor * 0.04f, btnSize, NewMat(cols[i], true), false);
            }
            else
            {
                GameObject screen = Box("PuzzleScreen", new Vector3(pos.x, 1.2f, pos.z) - toDoor * 0.36f, new Vector3(0.78f, 0.46f, 0.06f), matData, false);
                screen.AddComponent<BlinkingLight>().color = regAccentBright;
            }

            PuzzleStation ps = console.AddComponent<PuzzleStation>();
            ps.type = type; ps.radius = 3.6f;
            ConfigurePuzzle(ps, type, idx);
            gm.puzzles.Add(ps);
            gm.AddLabel(console.transform, ps.Hint(), GameColors.Ambar);
            return ps;
        }

        void ConfigurePuzzle(PuzzleStation ps, PuzzleStation.PuzzleType type, int idx)
        {
            if (type == PuzzleStation.PuzzleType.Cables) ps.pairCount = 3 + (idx % 2);
            else if (type == PuzzleStation.PuzzleType.CleanDust) { ps.dustCols = 8; ps.dustRows = 5; }
            else if (type == PuzzleStation.PuzzleType.MultiChoice)
            {
                switch ((idx + level) % 4)
                {
                    case 0: ps.question = "Qual cor indica ENERGIA no painel?"; ps.options = new[] { "Vermelho", "Âmbar", "Verde" }; ps.correctIndex = 1; break;
                    case 1: ps.question = "O que recarrega a bateria do M-37?"; ps.options = new[] { "A poça corrosiva", "A célula de energia", "O drone" }; ps.correctIndex = 1; break;
                    case 2: ps.question = "Para NÃO ser ouvido pelo drone, você deve…"; ps.options = new[] { "Correr", "Agachar", "Empurrar" }; ps.correctIndex = 1; break;
                    default: ps.question = "A bateria do M-37 representa…"; ps.options = new[] { "Tempo", "Vida", "Pontuação" }; ps.correctIndex = 1; break;
                }
            }
        }

        void FloorGuide(Vector2Int a, Vector2Int b)
        {
            Vector2Int dir = b - a;
            Vector3 ac = CellCenter(a);
            Vector3 mid = ac + new Vector3(dir.x, 0f, dir.y) * 3.4f; mid.y = 0.02f;
            Vector3 size = dir.x != 0 ? new Vector3(5f, 0.04f, 1.2f) : new Vector3(1.2f, 0.04f, 5f);
            GameObject g = Box("Guide", mid, size, matPath, false);
            g.GetComponent<Renderer>().material.mainTextureScale = new Vector2(1f, 3f);
        }

        void BuildChamber(Vector2Int chamber, Vector2Int exitDir)
        {
            Vector3 ctr = CellCenter(chamber);
            TileFloor(Box("Floor", new Vector3(ctr.x, -0.05f, ctr.z), new Vector3(CELL, 0.1f, CELL), matFloor));
            TileFloor(Box("Ceiling", new Vector3(ctr.x, H + 0.05f, ctr.z), new Vector3(CELL, 0.2f, CELL), matWall), 0.4f);
            foreach (Vector2Int dir in DIRS)
                if (dir != -exitDir) WallSide(chamber, dir, false); // o lado de volta tem o portão

            GameObject exit = Box("ExitZone", new Vector3(ctr.x, 1f, ctr.z), new Vector3(3.5f, 2f, 3.5f), matAmber, true);
            exit.GetComponent<Collider>().isTrigger = true; exit.GetComponent<Renderer>().enabled = false;
            exit.AddComponent<ExitZone>();
            Box("ExitPad", new Vector3(ctr.x, 0.06f, ctr.z), new Vector3(4f, 0.12f, 4f), matCheckpoint, false);
            Box("ExitMark", new Vector3(ctr.x, 0.13f, ctr.z), new Vector3(2.6f, 0.04f, 2.6f), matAmber, false);
            gm.AddLabel(exit.transform, level == 3 ? "PORTÃO EXTERNO" : "SAÍDA", GameColors.Ambar);

            if (level == 3)
            {
                Vector3 wallC = ctr + new Vector3(exitDir.x, 0f, exitDir.y) * (CELL / 2f - 0.3f);
                Box("Daylight", new Vector3(wallC.x, 2.6f, wallC.z), exitDir.x != 0 ? new Vector3(0.1f, 4f, 8f) : new Vector3(8f, 4f, 0.1f), matData, false);
                GameObject g = new GameObject("DaylightGlow"); g.transform.position = ctr + new Vector3(exitDir.x, 0f, exitDir.y) * 4f + Vector3.up * 2.5f;
                Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.intensity = 2.4f; l.color = new Color(0.85f, 0.93f, 1f);
            }
        }

        // ============================================================ props das salas
        void RoomProps(Vector2Int[] path, int i)
        {
            Vector3 c = CellCenter(path[i]);
            // Slots fixos para não sobrepor: 4 cantos + paredes (quartos de parede, longe das
            // portas que ficam no MEIO de cada parede) + interior reservado para a energia.
            Vector3 T1 = c + new Vector3(4.1f, 0f, 4.1f);   // canto +x,+z
            Vector3 T2 = c + new Vector3(4.1f, 0f, -4.1f);  // canto +x,-z
            bool platRoom = (level == 0 && i == 3) || (level == 1 && i == 3) || (level == 2 && i == 2) || (level == 3 && i == 1);

            Workstation(c + new Vector3(-4.1f, 0f, -4.1f)); // canto -x,-z
            DebrisPile(c + new Vector3(-4.2f, 0f, 4.2f));   // canto -x,+z

            // props de parede (quartos de parede; não no meio, onde há porta)
            if (i == 1 || i == 3) EmergencyLightAt(c + new Vector3(-5.7f, 3.6f, 2.7f), Vector3.right);
            if (i == 0 || i == 2) FireExtinguisher(c + new Vector3(5.45f, 0f, -2.7f));
            if (i == 2 || i == 4) Shelf(c + new Vector3(-2.7f, 0f, 5.45f));
            if (i == 0 || i == 3) DripPipeAt(c + new Vector3(-5.45f, 0f, -2.8f));

            // energia: ponto reservado no interior, longe de qualquer prop (não fica "dentro" de nada)
            if (!platRoom && (i == 0 || i == 2)) EnergyCell(c + new Vector3(2.2f, 0.8f, -2.2f));

            switch (level)
            {
                case 0: // Montagem
                    if (!platRoom) WorkTable(T2);
                    if (i == 0 || i == 2) RoboticArm(T1);
                    if (i == 1) ConveyorBelt(c + new Vector3(-2.4f, 0.25f, 0f), new Vector3(2f, 0.4f, 5.5f), Vector3.forward, 1.5f);
                    if (i == 2) { Puddle(c + new Vector3(0f, 0.2f, 3.0f), new Vector3(3f, 0.4f, 2.6f)); DroneIn(c); }
                    break;

                case 1: // Caldeiras
                    if (!platRoom) BoilerTank(T1);
                    BoilerTank(T2);
                    if (i == 1 || i == 2) { SteamVentAt(c + new Vector3(-1.6f, 0f, 0f), 0f); SteamVentAt(c + new Vector3(1.6f, 0f, 1.6f), 2.5f); }
                    break;

                case 2: // Subsolo Químico
                    if (!platRoom) Cyl("ChemBarrel", T1 + new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0.9f), matAcid, true);
                    Cyl("ChemBarrel", T2 + new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0.9f), matAcid, true);
                    if (i == 1 || i == 3) Puddle(c + new Vector3(0f, 0.2f, -2.8f), new Vector3(3f, 0.4f, 2.6f));
                    CoverAt(c + new Vector3(-2.2f, 0.6f, 2.4f));
                    if (i == 1 || i == 3) DroneIn(c);
                    break;

                default: // Servidores
                    if (!platRoom) ServerRack(T1);
                    ServerRack(T2);
                    Hologram(c + new Vector3(-2.2f, 0f, 2.4f), i == 0 ? "registro corrompido" : null);
                    if (i == 2) DroneIn(c);
                    break;
            }

            if (platRoom) RaisedPlatform(c); // usa o canto +x,+z (T1), que ficou livre
        }

        void DroneIn(Vector3 c)
        {
            BuildDrone(new[]
            {
                new Vector3(c.x - 3f, 2.4f, c.z - 3f), new Vector3(c.x + 3f, 2.4f, c.z + 2f), new Vector3(c.x - 2f, 2.4f, c.z + 3f),
            }, 2.2f);
        }

        void Puddle(Vector3 pos, Vector3 size)
        {
            float d = Mathf.Max(size.x, size.z);   // poça REDONDA (diâmetro)
            // zona de dano (invisível, mais alta para pegar o robô em pé)
            GameObject zone = TriggerZone("CorrosivePuddle", new Vector3(pos.x, 0.3f, pos.z), new Vector3(d * 0.78f, 0.6f, d * 0.78f));
            zone.AddComponent<CorrosivePuddle>();
            // superfície líquida REDONDA (disco), brilhante e translúcida, rente ao chão
            GameObject surf = Cyl("PuddleSurface", new Vector3(pos.x, 0.05f, pos.z), new Vector3(d, 0.04f, d), matPuddle);
            surf.GetComponent<Renderer>().material.mainTextureScale = new Vector2(d * 0.4f, d * 0.4f);
            // brilho esverdeado
            GameObject g = new GameObject("PuddleGlow"); g.transform.position = new Vector3(pos.x, 0.25f, pos.z);
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = GameColors.Acido; l.range = d + 1.5f; l.intensity = 1.0f;
            BubbleFX(pos, d);
        }

        // bolhas verdes subindo de uma área REDONDA (líquido corrosivo borbulhando).
        void BubbleFX(Vector3 pos, float d)
        {
            GameObject p = new GameObject("Bubbles");
            p.transform.position = new Vector3(pos.x, 0.1f, pos.z);
            p.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // cone aponta para cima
            ParticleSystem ps = p.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 1.6f; main.startSpeed = 0.35f; main.startSize = 0.32f;
            main.startColor = new Color(0.55f, 0.85f, 0.42f, 0.85f);
            main.gravityModifier = -0.02f; main.maxParticles = 50; main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule em = ps.emission; em.rateOverTime = 12f;
            ParticleSystem.ShapeModule sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = d * 0.42f;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime; col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ParticleSystemRenderer rend = p.GetComponent<ParticleSystemRenderer>();
            rend.material = matSmoke; rend.renderMode = ParticleSystemRenderMode.Billboard; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        void CoverAt(Vector3 pos) { TileBox(Box("Cover", pos, new Vector3(1.4f, 1.2f, 1.4f), matCrate)); }

        // cano na parede que goteja água numa poça no chão (com som ao pingar).
        void DripPipeAt(Vector3 floorPos)
        {
            float pipeY = 3.4f;
            Cyl("DripPipe", new Vector3(floorPos.x, pipeY, floorPos.z), new Vector3(0.16f, 0.7f, 0.16f), matPipe, false, new Vector3(0f, 0f, 90f));
            Cyl("DripJoint", new Vector3(floorPos.x, pipeY - 0.12f, floorPos.z), new Vector3(0.2f, 0.14f, 0.2f), matMetal);
            GameObject puddle = Cyl("WaterPuddle", new Vector3(floorPos.x, 0.04f, floorPos.z), new Vector3(1.2f, 0.03f, 1.2f), matWater);
            GameObject splash = Cyl("Splash", new Vector3(floorPos.x, 0.07f, floorPos.z), new Vector3(0.3f, 0.02f, 0.3f), matWater);
            GameObject drop = Sphere("Drop", new Vector3(floorPos.x, pipeY - 0.4f, floorPos.z), 0.12f, matWater);
            Destroy(drop.GetComponent<Collider>());

            AudioSource a = puddle.AddComponent<AudioSource>();
            if (SoundManager.Instance != null) a.clip = SoundManager.Instance.Drip;
            a.loop = false; a.playOnAwake = false; a.spatialBlend = 1f; a.minDistance = 2f; a.maxDistance = 12f; a.volume = 0.7f;

            WaterDrip wd = puddle.AddComponent<WaterDrip>();
            wd.drop = drop.transform; wd.topY = pipeY - 0.4f; wd.puddleY = 0.12f; wd.sfx = a; wd.splash = splash.transform;
        }

        // detritos de metal no canto: caixa de madeira, tubos, fios, engrenagem e sucata.
        void DebrisPile(Vector3 p)
        {
            GameObject crate = Box("WoodCrate", new Vector3(p.x, 0.3f, p.z), new Vector3(0.7f, 0.6f, 0.7f), matWood); TileBox(crate, 1f);
            Cyl("DebrisPipe", new Vector3(p.x + 0.6f, 0.12f, p.z + 0.2f), new Vector3(0.12f, 0.6f, 0.12f), matPipe, false, new Vector3(0f, 0f, 90f));
            Cyl("DebrisPipe", new Vector3(p.x + 0.45f, 0.28f, p.z - 0.35f), new Vector3(0.1f, 0.45f, 0.1f), matPipe, false, new Vector3(90f, 25f, 0f));
            Gear(new Vector3(p.x - 0.55f, 0f, p.z + 0.35f), 0.32f);
            Cyl("Wire", new Vector3(p.x - 0.3f, 0.06f, p.z - 0.4f), new Vector3(0.04f, 0.5f, 0.04f), matBolt, false, new Vector3(90f, 40f, 0f));
            Cyl("Wire", new Vector3(p.x - 0.1f, 0.06f, p.z - 0.5f), new Vector3(0.04f, 0.4f, 0.04f), matBolt, false, new Vector3(90f, -30f, 0f));
            Box("Scrap", new Vector3(p.x + 0.2f, 0.1f, p.z + 0.5f), new Vector3(0.4f, 0.2f, 0.3f), matMetal, false);
        }

        void Gear(Vector3 p, float r)
        {
            Cyl("Gear", new Vector3(p.x, 0.05f, p.z), new Vector3(2f * r, 0.05f, 2f * r), matMetal);
            Cyl("GearHub", new Vector3(p.x, 0.07f, p.z), new Vector3(0.5f * r, 0.06f, 0.5f * r), matBolt);
            int teeth = 8;
            for (int i = 0; i < teeth; i++)
            {
                float a = i / (float)teeth * Mathf.PI * 2f;
                GameObject tooth = Box("Tooth", new Vector3(p.x + Mathf.Cos(a) * r, 0.05f, p.z + Mathf.Sin(a) * r), new Vector3(0.09f, 0.06f, 0.09f), matMetal, false);
                tooth.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        void FireExtinguisher(Vector3 p)
        {
            Cyl("ExtBody", new Vector3(p.x, 0.45f, p.z), new Vector3(0.3f, 0.4f, 0.3f), matRed, true);
            Cyl("ExtTop", new Vector3(p.x, 0.9f, p.z), new Vector3(0.18f, 0.08f, 0.18f), matBolt);
            Box("ExtHandle", new Vector3(p.x, 1.0f, p.z), new Vector3(0.22f, 0.07f, 0.12f), matMetal, false);
            Cyl("ExtHose", new Vector3(p.x + 0.16f, 0.62f, p.z + 0.1f), new Vector3(0.04f, 0.26f, 0.04f), matBolt, false, new Vector3(45f, 0f, 0f));
            Box("ExtLabel", new Vector3(p.x, 0.45f, p.z + 0.31f), new Vector3(0.18f, 0.2f, 0.02f), matCheckpoint, false);
            // suporte na parede
            Box("ExtBracket", new Vector3(p.x, 0.45f, p.z - 0.18f), new Vector3(0.34f, 0.1f, 0.06f), matRoboDark, false);
        }

        void Shelf(Vector3 p)
        {
            Box("ShelfPost", new Vector3(p.x - 0.7f, 0.9f, p.z), new Vector3(0.08f, 1.8f, 0.5f), matMetal);
            Box("ShelfPost", new Vector3(p.x + 0.7f, 0.9f, p.z), new Vector3(0.08f, 1.8f, 0.5f), matMetal);
            foreach (float h in new[] { 0.45f, 1.05f, 1.6f }) Box("ShelfPlank", new Vector3(p.x, h, p.z), new Vector3(1.5f, 0.06f, 0.5f), matMetal);
            Box("ShelfCrate", new Vector3(p.x - 0.35f, 0.66f, p.z), new Vector3(0.42f, 0.36f, 0.36f), matWood, false);
            Box("ShelfCrate", new Vector3(p.x + 0.35f, 0.63f, p.z), new Vector3(0.36f, 0.3f, 0.3f), matCrate, false);
            Box("ShelfBox", new Vector3(p.x + 0.1f, 1.26f, p.z), new Vector3(0.5f, 0.3f, 0.35f), matWood, false);
            Box("ShelfBox", new Vector3(p.x - 0.4f, 1.24f, p.z), new Vector3(0.34f, 0.28f, 0.3f), matCrate, false);
        }

        // luz de emergência vermelha pulsante numa parede (lente + luz + EmergencyLight).
        void EmergencyLightAt(Vector3 pos, Vector3 inward)
        {
            Box("EmHousing", pos, new Vector3(0.32f, 0.26f, 0.22f), matRoboDark, false);
            GameObject lens = Box("EmLens", pos + inward * 0.13f, new Vector3(0.22f, 0.18f, 0.08f), matRed, false);
            GameObject lp = new GameObject("EmLight"); lp.transform.SetParent(lens.transform, false); lp.transform.localPosition = inward * 0.3f;
            Light l = lp.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.95f, 0.12f, 0.1f); l.range = 6f; l.intensity = 0.3f;
            lens.AddComponent<EmergencyLight>();
        }

        // ponto de energia: PILHA AMARELA em pé com um RAIO na frente.
        void EnergyCell(Vector3 pos)
        {
            GameObject e = new GameObject("EnergyCell");
            e.transform.position = pos;
            SphereCollider sc = e.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.55f;
            e.AddComponent<EnergyCell>();
            Transform t = e.transform;

            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.4f, 0.34f, 0.4f), matBattery);   // corpo
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f), new Vector3(0.16f, 0.06f, 0.16f), matMetal); // terminal +
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.42f, 0.04f, 0.42f), matBolt);  // faixa
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), new Vector3(0.42f, 0.04f, 0.42f), matBolt);
            // raio (zigue-zague de 3 segmentos na frente)
            Part(t, PrimitiveType.Cube, new Vector3(0.03f, 0.13f, 0.41f), new Vector3(0.05f, 0.15f, 0.03f), matBolt).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            Part(t, PrimitiveType.Cube, new Vector3(-0.02f, 0.0f, 0.41f), new Vector3(0.05f, 0.15f, 0.03f), matBolt).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            Part(t, PrimitiveType.Cube, new Vector3(0.03f, -0.13f, 0.41f), new Vector3(0.05f, 0.15f, 0.03f), matBolt).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);

            GameObject glow = new GameObject("CellGlow"); glow.transform.SetParent(t, false);
            Light l = glow.AddComponent<Light>(); l.type = LightType.Point; l.color = Hex("#FFE24D"); l.range = 3f; l.intensity = 1.1f;
        }

        void KeyAt(Vector3 pos)
        {
            // chave dourada estilizada: cabeça (anel com furo) + haste + palhetão (dentes).
            GameObject key = new GameObject("Key");
            key.transform.position = pos;
            BoxCollider bc = key.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.7f, 1.1f, 0.5f);
            key.AddComponent<IndustrialKey>();
            Transform t = key.transform;

            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), new Vector3(0.36f, 0.07f, 0.36f), matGold).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0.06f), new Vector3(0.17f, 0.06f, 0.17f), matRoboDark).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, -0.04f, 0f), new Vector3(0.08f, 0.28f, 0.08f), matGold);
            Part(t, PrimitiveType.Cube, new Vector3(0.12f, -0.22f, 0f), new Vector3(0.2f, 0.06f, 0.07f), matGold);
            Part(t, PrimitiveType.Cube, new Vector3(0.10f, -0.31f, 0f), new Vector3(0.16f, 0.06f, 0.07f), matGold);

            GameObject glow = new GameObject("KeyGlow"); glow.transform.SetParent(t, false); glow.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            Light l = glow.AddComponent<Light>(); l.type = LightType.Point; l.color = Hex("#FFC94D"); l.range = 3.2f; l.intensity = 1.3f;

            gm.AddLabel(t, "Chave " + (level + 1) + "/" + GameManager.TotalLevels, GameColors.Ambar);
        }

        // mobiliário (mesa, cadeira, computador) — comum a todos os setores
        void Workstation(Vector3 p)
        {
            Box("DeskTop", new Vector3(p.x, 0.75f, p.z), new Vector3(1.7f, 0.1f, 0.85f), matMetal);
            foreach (var o in new[] { new Vector3(-0.7f, 0, -0.3f), new Vector3(0.7f, 0, -0.3f), new Vector3(-0.7f, 0, 0.3f), new Vector3(0.7f, 0, 0.3f) })
                Box("DeskLeg", new Vector3(p.x + o.x, 0.37f, p.z + o.z), new Vector3(0.08f, 0.74f, 0.08f), matMetal, false);
            // computador
            GameObject mon = Box("Monitor", new Vector3(p.x, 1.15f, p.z + 0.2f), new Vector3(0.62f, 0.42f, 0.06f), matData, false);
            mon.AddComponent<BlinkingLight>().color = regAccentBright;
            Box("MonStand", new Vector3(p.x, 0.9f, p.z + 0.2f), new Vector3(0.08f, 0.18f, 0.08f), matMetal, false);
            Box("Keyboard", new Vector3(p.x, 0.83f, p.z - 0.15f), new Vector3(0.5f, 0.04f, 0.2f), matMetal, false);
            // cadeira
            Vector3 ch = new Vector3(p.x, 0f, p.z - 1.0f);
            Box("ChairSeat", new Vector3(ch.x, 0.5f, ch.z), new Vector3(0.5f, 0.08f, 0.5f), matCrate, false);
            Box("ChairBack", new Vector3(ch.x, 0.85f, ch.z - 0.21f), new Vector3(0.5f, 0.6f, 0.08f), matCrate, false);
            foreach (var o in new[] { new Vector3(-0.2f, 0, -0.2f), new Vector3(0.2f, 0, -0.2f), new Vector3(-0.2f, 0, 0.2f), new Vector3(0.2f, 0, 0.2f) })
                Box("ChairLeg", new Vector3(ch.x + o.x, 0.25f, ch.z + o.z), new Vector3(0.06f, 0.5f, 0.06f), matMetal, false);
        }

        void WorkTable(Vector3 p)
        {
            Box("TableTop", new Vector3(p.x, 0.78f, p.z), new Vector3(1.9f, 0.12f, 1.0f), matMetal);
            foreach (var o in new[] { new Vector3(-0.8f, 0, -0.4f), new Vector3(0.8f, 0, -0.4f), new Vector3(-0.8f, 0, 0.4f), new Vector3(0.8f, 0, 0.4f) })
                Box("Leg", new Vector3(p.x + o.x, 0.39f, p.z + o.z), new Vector3(0.1f, 0.78f, 0.1f), matMetal, false);
            Box("Part", new Vector3(p.x + 0.3f, 0.95f, p.z), new Vector3(0.3f, 0.2f, 0.3f), matCrate, false);
        }

        void RoboticArm(Vector3 p)
        {
            Cyl("ArmBase", new Vector3(p.x, 0.15f, p.z), new Vector3(0.5f, 0.15f, 0.5f), matMetal);
            Box("ArmSeg1", new Vector3(p.x, 0.7f, p.z), new Vector3(0.18f, 1.0f, 0.18f), matMetal, false);
            GameObject seg2 = Box("ArmSeg2", new Vector3(p.x, 1.2f, p.z + 0.4f), new Vector3(0.16f, 0.16f, 1.0f), matMetal, false);
            seg2.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            Box("Claw", new Vector3(p.x, 1.0f, p.z + 0.85f), new Vector3(0.22f, 0.12f, 0.22f), matCrate, false);
        }

        void ConveyorBelt(Vector3 center, Vector3 size, Vector3 dir, float speed)
        {
            GameObject belt = Box("Belt", center, size, matBelt);
            belt.GetComponent<Renderer>().material.mainTextureScale = new Vector2(size.x * 0.6f, size.z * 0.6f);
            for (float ez = -size.z / 2f + 0.3f; ez <= size.z / 2f; ez += 1.2f)
                Cyl("Roller", center + new Vector3(0f, size.y / 2f, ez), new Vector3(size.x, 0.12f, 0.12f), matMetal, false, new Vector3(0f, 0f, 90f));
            GameObject zone = TriggerZone("ConveyorZone", center + new Vector3(0f, size.y / 2f + 0.5f, 0f), new Vector3(size.x, 1.0f, size.z));
            Conveyor cv = zone.AddComponent<Conveyor>(); cv.worldDir = dir; cv.speed = speed; cv.beltRenderer = belt.GetComponent<Renderer>();
        }

        void BoilerTank(Vector3 p)
        {
            Cyl("Boiler", new Vector3(p.x, 1.6f, p.z), new Vector3(2.0f, 1.6f, 2.0f), matCrate, true);
            Cyl("BoilerCap", new Vector3(p.x, 3.3f, p.z), new Vector3(2.2f, 0.2f, 2.2f), matMetal);
            Box("HeatBand", new Vector3(p.x, 0.9f, p.z), new Vector3(2.15f, 0.4f, 2.15f), matHeat, false);
            GameObject g = new GameObject("HeatGlow"); g.transform.position = new Vector3(p.x, 1f, p.z);
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = 1.1f; l.color = Hex("#E0631A");
        }

        void SteamVentAt(Vector3 pos, float phase)
        {
            GameObject zone = TriggerZone("SteamZone", pos + new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 3.0f, 2.0f));
            GameObject psGo = new GameObject("SteamFX");
            psGo.transform.position = pos + new Vector3(0f, 0.2f, 0f);
            psGo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 1.9f; main.startSpeed = 1.3f; main.startSize = 1.3f;
            main.startColor = new Color(0.92f, 0.94f, 0.97f, 0.5f);
            main.gravityModifier = -0.04f; main.maxParticles = 140; main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule em = ps.emission; em.rateOverTime = 0f;
            ParticleSystem.ShapeModule sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.45f;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.5f));
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime; col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ParticleSystemRenderer rend = psGo.GetComponent<ParticleSystemRenderer>();
            rend.material = matSmoke; rend.renderMode = ParticleSystemRenderMode.Billboard; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject g = new GameObject("SteamLight"); g.transform.position = pos + new Vector3(0f, 1.3f, 0f);
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.intensity = 0.05f; l.color = Color.white;

            // som de fumaça saindo (3D), volume controlado pelo SteamVent
            AudioSource hissSrc = psGo.AddComponent<AudioSource>();
            if (SoundManager.Instance != null) hissSrc.clip = SoundManager.Instance.Hiss;
            hissSrc.loop = true; hissSrc.playOnAwake = false; hissSrc.spatialBlend = 1f;
            hissSrc.minDistance = 2f; hissSrc.maxDistance = 14f; hissSrc.volume = 0f;
            if (hissSrc.clip != null) hissSrc.Play();

            SteamVent sv = zone.AddComponent<SteamVent>(); sv.phase = phase; sv.steam = ps; sv.steamLight = l; sv.hiss = hissSrc;
            Cyl("Nozzle", new Vector3(pos.x, 0.1f, pos.z), new Vector3(0.5f, 0.1f, 0.5f), matMetal);
        }

        void ServerRack(Vector3 p)
        {
            Box("Rack", new Vector3(p.x, 1.5f, p.z), new Vector3(1.2f, 3.0f, 0.9f), matMetal);
            for (int i = 0; i < 6; i++)
            {
                GameObject led = Box("LED", new Vector3(p.x + 0.45f, 0.6f + i * 0.4f, p.z + 0.46f), new Vector3(0.12f, 0.12f, 0.05f), matData, false);
                led.AddComponent<BlinkingLight>().color = (i % 2 == 0) ? regAccentBright : GameColors.Ambar;
            }
        }

        void Hologram(Vector3 p, string label)
        {
            Cyl("HoloEmitter", new Vector3(p.x, 0.1f, p.z), new Vector3(0.5f, 0.1f, 0.5f), matMetal);
            GameObject holo = Box("Holo", new Vector3(p.x, 1.1f, p.z), new Vector3(0.7f, 1.6f, 0.05f), matData, false);
            holo.AddComponent<BlinkingLight>().color = regAccentBright;
            if (!string.IsNullOrEmpty(label)) gm.AddLabel(holo.transform, label, GameColors.Papel);
        }

        // rampa para uma plataforma elevada num CANTO (verticalidade — "subir"), sem bloquear
        // as portas (que ficam no meio das paredes). Célula de energia como recompensa no topo.
        void RaisedPlatform(Vector3 c)
        {
            Vector3 baseP = c + new Vector3(3.4f, 0f, 3.4f); // canto +x,+z
            float rise = 1.0f;
            Box("Platform", new Vector3(baseP.x, rise / 2f, baseP.z), new Vector3(2.8f, rise, 2.8f), matMetal);
            // rampa descendo da frente da plataforma (-z) até o chão
            float run = 3.2f;
            float len = Mathf.Sqrt(run * run + rise * rise);
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float frontZ = baseP.z - 1.4f;                  // borda frontal da plataforma
            GameObject ramp = Box("Ramp", new Vector3(baseP.x, rise / 2f, frontZ - run / 2f), new Vector3(2.2f, 0.2f, len), matWood);
            ramp.GetComponent<Renderer>().material.mainTextureScale = new Vector2(2f, 3f);
            ramp.transform.rotation = Quaternion.Euler(-ang, 0f, 0f);
            EnergyCell(new Vector3(baseP.x, rise + 0.8f, baseP.z));
        }

        void BuildDrone(Vector3[] wps, float speed)
        {
            GameObject drone = new GameObject("Drone");
            drone.transform.position = wps[0];

            // som de rotor (helicóptero), 3D — mais alto perto do drone
            AudioSource rotorSrc = drone.AddComponent<AudioSource>();
            if (SoundManager.Instance != null) rotorSrc.clip = SoundManager.Instance.Rotor;
            rotorSrc.loop = true; rotorSrc.playOnAwake = false; rotorSrc.spatialBlend = 1f;
            rotorSrc.rolloffMode = AudioRolloffMode.Linear; rotorSrc.minDistance = 2.5f; rotorSrc.maxDistance = 22f; rotorSrc.volume = 0.4f;
            if (rotorSrc.clip != null) rotorSrc.Play();

            // QUADRICÓPTERO: corpo + domo + braços em "+" + 4 motores com hélices girando + skids.
            Part(drone.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.26f, 0.7f), matMetal);
            Part(drone.transform, PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0f), new Vector3(0.52f, 0.3f, 0.52f), matRoboDark);
            Part(drone.transform, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(1.5f, 0.07f, 0.12f), matRoboDark);
            Part(drone.transform, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.07f, 1.5f), matRoboDark);
            Vector3[] ends = { new Vector3(0.7f, 0.1f, 0f), new Vector3(-0.7f, 0.1f, 0f), new Vector3(0f, 0.1f, 0.7f), new Vector3(0f, 0.1f, -0.7f) };
            foreach (Vector3 e in ends)
            {
                Part(drone.transform, PrimitiveType.Cylinder, e, new Vector3(0.2f, 0.05f, 0.2f), matMetal);
                GameObject rotorGo = new GameObject("Rotor");
                rotorGo.transform.SetParent(drone.transform, false);
                rotorGo.transform.localPosition = e + new Vector3(0f, 0.07f, 0f);
                rotorGo.AddComponent<Spinner>();
                Part(rotorGo.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 0.015f, 0.06f), matBolt);
                Part(rotorGo.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.015f, 0.6f), matBolt);
            }
            Part(drone.transform, PrimitiveType.Cube, new Vector3(0.28f, -0.2f, 0f), new Vector3(0.05f, 0.25f, 0.6f), matRoboDark);
            Part(drone.transform, PrimitiveType.Cube, new Vector3(-0.28f, -0.2f, 0f), new Vector3(0.05f, 0.25f, 0.6f), matRoboDark);

            // olho/câmera (esfera âmbar) embaixo, à frente + luz
            GameObject dEye = Sphere("DroneEye", Vector3.zero, 0.26f, matAmber);
            Destroy(dEye.GetComponent<Collider>()); dEye.transform.SetParent(drone.transform); dEye.transform.localPosition = new Vector3(0f, -0.16f, 0.28f);
            Light dl = dEye.AddComponent<Light>(); dl.type = LightType.Point; dl.range = 7f; dl.intensity = 2.2f; dl.color = GameColors.Ambar;

            PatrolDrone pd = drone.AddComponent<PatrolDrone>(); pd.waypoints = wps; pd.speed = speed;
            pd.eyeOverride = dEye.GetComponent<Renderer>(); pd.eyeLightOverride = dl;
            pd.Init(player, battery, player.GetComponent<RobotController>());

            GameObject coneGo = new GameObject("VisionCone"); coneGo.transform.position = new Vector3(wps[0].x, 0.12f, wps[0].z);
            coneGo.AddComponent<VisionCone>().Init(pd, pd.visionRange, pd.visionAngle);
        }
    }
}
