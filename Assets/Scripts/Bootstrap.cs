using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Constroi TODO o vertical slice (Setor Montagem) em tempo de execucao, so com
    /// placeholders geometricos. A cena guardada no projeto contem apenas este objeto:
    /// abrir a cena e dar Play ja monta a fase inteira. Isso mantem o repositorio limpo
    /// (sem assets binarios pesados) e o protótipo 100% reproduzivel.
    ///
    /// Layout (vista de cima, Z = profundidade):
    ///   z -3..9   Corredor inicial curto (Ajuste 3) + Puzzle 1 (empurrar caixa no portao)
    ///   z 9..20   Antessala com caixa-cobertura obvia + tutorial de furtividade (Ajuste 2)
    ///   z 20..40  Sala do drone: patrulha lenta, coberturas, Chave 1/3, celula de risco, poca
    ///   z 40+     Puzzle 2 (painel de cabo) abre a porta -> Saida -> Vitoria
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        Material matFloor, matWall, matCrate, matRobo, matAmber, matAcid, matDoor, matCheckpoint;

        void Start()
        {
            BuildMaterials();

            GameManager gm = gameObject.AddComponent<GameManager>();

            BuildEnvironment();
            Transform player = BuildPlayer();
            Camera cam = BuildCamera(player);
            BuildLighting();

            RobotController ctrl = player.GetComponent<RobotController>();
            ctrl.SetCamera(cam.transform);
            gm.battery = player.GetComponent<BatterySystem>();

            BuildProps(player, gm);
        }

        // ---------------------------------------------------------------- materials
        Material NewMat(Color c, bool emissive = false)
        {
            // Fallback de shader: na build, Shader.Find pode retornar null se o shader for
            // removido (stripping). O Standard esta nos "Always Included Shaders"; mesmo assim
            // garantimos um shader valido para o jogo nunca quebrar ao iniciar.
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");

            Material m = new Material(sh);
            m.color = c;
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.6f);
            }
            return m;
        }

        void BuildMaterials()
        {
            matFloor = NewMat(GameColors.AcoBase);
            matWall = NewMat(GameColors.Parede);
            matCrate = NewMat(GameColors.Ferrugem);
            matRobo = NewMat(GameColors.Robo);
            matAmber = NewMat(GameColors.Ambar, true);
            matAcid = NewMat(GameColors.Acido, true);
            matDoor = NewMat(new Color(0.25f, 0.3f, 0.35f));
            matCheckpoint = NewMat(GameColors.Ambar, true);
        }

        // ---------------------------------------------------------------- primitives
        GameObject Box(string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        GameObject Sphere(string name, Vector3 center, float diameter, Material mat, bool trigger = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().material = mat;
            if (trigger) go.GetComponent<Collider>().isTrigger = true;
            return go;
        }

        // ---------------------------------------------------------------- environment
        void BuildEnvironment()
        {
            // Piso estendido para tras (z -6) e para a frente (z 43), para a camera proxima
            // caber dentro do ambiente sem atravessar a parede de tras.
            Box("Floor", new Vector3(0f, -0.05f, 18.5f), new Vector3(17f, 0.1f, 49f), matFloor);

            const float h = 5f, y = 2.5f, t = 0.5f; // paredes mais altas

            // Perimetro
            Box("Wall_Back", new Vector3(0f, y, -6f), new Vector3(17f, h, t), matWall);
            Box("Wall_Left", new Vector3(-8f, y, 18.5f), new Vector3(t, h, 49f), matWall);
            Box("Wall_Right", new Vector3(8f, y, 18.5f), new Vector3(t, h, 49f), matWall);

            // Divisoria z=9 com vao central (Puzzle 1 — a caixa bloqueia o vao)
            Box("Wall_z9_L", new Vector3(-4.5f, y, 9f), new Vector3(7f, h, t), matWall);
            Box("Wall_z9_R", new Vector3(4.5f, y, 9f), new Vector3(7f, h, t), matWall);

            // Divisoria z=20 com porta aberta (entrada da sala do drone)
            Box("Wall_z20_L", new Vector3(-4.5f, y, 20f), new Vector3(7f, h, t), matWall);
            Box("Wall_z20_R", new Vector3(4.5f, y, 20f), new Vector3(7f, h, t), matWall);

            // Parede frontal z=40 com vao da saida
            Box("Wall_Front_L", new Vector3(-4.5f, y, 40f), new Vector3(7f, h, t), matWall);
            Box("Wall_Front_R", new Vector3(4.5f, y, 40f), new Vector3(7f, h, t), matWall);

            // Teto (fecha o ambiente; a camera fica abaixo dele)
            Box("Ceiling", new Vector3(0f, h + 0.05f, 18.5f), new Vector3(17f, 0.2f, 49f), matWall);
        }

        // ---------------------------------------------------------------- player
        Transform BuildPlayer()
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "M-37";
            p.transform.position = new Vector3(0f, 1f, -1f);
            p.GetComponent<Renderer>().material = matRobo;
            Destroy(p.GetComponent<Collider>()); // o CharacterController e o colisor

            CharacterController cc = p.AddComponent<CharacterController>();
            cc.height = 1.0f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;
            cc.skinWidth = 0.04f;

            p.AddComponent<BatterySystem>();
            p.AddComponent<RobotController>();

            // "olho" do robo (placeholder de identidade + indica a frente)
            GameObject eye = Sphere("Eye", Vector3.zero, 0.28f, matAmber);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(p.transform);
            eye.transform.localPosition = new Vector3(0f, 0.18f, 0.42f);

            return p.transform;
        }

        Camera BuildCamera(Transform target)
        {
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameColors.AcoBase * 0.6f;
            camGo.AddComponent<AudioListener>();

            CameraFollow follow = camGo.AddComponent<CameraFollow>();
            follow.target = target;
            camGo.transform.position = target.position + follow.offset;
            return cam;
        }

        void BuildLighting()
        {
            GameObject sun = new GameObject("Sun");
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.93f, 0.82f);
            l.intensity = 0.7f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // Luzes internas: com o teto fechado, o ambiente precisa de iluminacao propria
            // (lampadas industriais ao longo do trajeto).
            float[] zs = { -3f, 6f, 15f, 24f, 33f, 41f };
            foreach (float z in zs)
            {
                GameObject lampGo = new GameObject("CeilLamp");
                lampGo.transform.position = new Vector3(0f, 4.3f, z);
                Light pl = lampGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.range = 16f;
                pl.intensity = 1.5f;
                pl.color = new Color(1f, 0.88f, 0.72f);
            }
        }

        // ---------------------------------------------------------------- props / puzzles
        void BuildProps(Transform player, GameManager gm)
        {
            BatterySystem battery = player.GetComponent<BatterySystem>();

            // Detalhe cosmetico no corredor inicial (Ajuste 3: nao comecar parado demais)
            GameObject holo = Box("Holo", new Vector3(3f, 0.7f, 3.5f), new Vector3(0.4f, 1.2f, 0.4f), matAmber, false);
            gm.AddLabel(holo.transform, "holograma quebrado", GameColors.Papel);

            // Celula 1 (pequeno desvio no corredor)
            Sphere("EnergyCell_1", new Vector3(-4.5f, 0.8f, 6f), 0.6f, matAmber, true).AddComponent<EnergyCell>();

            // PUZZLE 1: caixa movel bloqueando o vao em z=9
            GameObject box = Box("PushBox", new Vector3(0f, 0.65f, 9f), new Vector3(1.6f, 1.3f, 1.6f), matCrate);
            box.AddComponent<PushBox>();
            Rigidbody rb = box.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 6f;   // segura a caixa quando o robo solta, sem travar o empurrao
            rb.angularDamping = 8f;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            gm.AddLabel(box.transform, "ande contra a caixa para empurrar", GameColors.Papel);

            // Caixa-cobertura obvia + tutorial de furtividade (Ajuste 2), antes da sala do drone
            GameObject cover0 = Box("Cover_Tutorial", new Vector3(0f, 0.6f, 16f), new Vector3(2f, 1.2f, 1.2f), matCrate);
            gm.AddLabel(cover0.transform, "agache (C) atras das caixas para se esconder", GameColors.Ambar);

            // Checkpoint visual (logo apos a parte difícil)
            GameObject cp = Box("Checkpoint", new Vector3(0f, 0.02f, 21.5f), new Vector3(3f, 0.04f, 1f), matCheckpoint, false);
            gm.AddLabel(cp.transform, "checkpoint", GameColors.Papel);

            // SALA DO DRONE: coberturas
            Box("Cover_A", new Vector3(-3f, 0.6f, 26f), new Vector3(1.4f, 1.2f, 1.4f), matCrate);
            Box("Cover_B", new Vector3(3f, 0.6f, 30f), new Vector3(1.4f, 1.2f, 1.4f), matCrate);
            Box("Cover_C", new Vector3(0f, 0.6f, 33f), new Vector3(1.4f, 1.2f, 1.4f), matCrate);

            // Poca corrosiva (verde) — ensina perigo de area
            GameObject puddle = Box("CorrosivePuddle", new Vector3(-4f, 0.2f, 31f), new Vector3(3.5f, 0.4f, 4f), matAcid, true);
            puddle.GetComponent<Collider>().isTrigger = true;
            puddle.AddComponent<CorrosivePuddle>();
            gm.AddLabel(puddle.transform, "poça corrosiva — evite", GameColors.Acido);

            // Celula 2 (risco: perto do drone)
            Sphere("EnergyCell_2", new Vector3(-5f, 0.8f, 35f), 0.6f, matAmber, true).AddComponent<EnergyCell>();

            // Chave 1/3 (no canto, perto da rota do drone)
            GameObject key = Box("Key_1of3", new Vector3(5f, 0.8f, 35f), new Vector3(0.5f, 0.7f, 0.3f), matAmber);
            key.GetComponent<Collider>().isTrigger = true;
            key.AddComponent<IndustrialKey>();
            gm.AddLabel(key.transform, "Chave 1/3", GameColors.Ambar);

            // DRONE (lento no primeiro encontro — Ajuste 2)
            GameObject drone = Box("Drone", new Vector3(-4f, 1.6f, 27f), new Vector3(0.9f, 0.45f, 0.9f), matWall);
            GameObject dEye = Sphere("DroneEye", Vector3.zero, 0.3f, matAmber);
            Destroy(dEye.GetComponent<Collider>());
            dEye.transform.SetParent(drone.transform);
            dEye.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            Light dLight = dEye.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.range = 7f;
            dLight.intensity = 2f;
            dLight.color = GameColors.Ambar;
            PatrolDrone pd = drone.AddComponent<PatrolDrone>();
            pd.waypoints = new Vector3[]
            {
                new Vector3(-4f, 1.6f, 26f),
                new Vector3(4f, 1.6f, 30f),
                new Vector3(-3f, 1.6f, 34f),
            };
            pd.speed = 2.0f; // proposital mente lento
            pd.Init(player, battery, player.GetComponent<RobotController>());

            // PUZZLE 2: painel + porta da saida
            GameObject door = Box("ExitDoor", new Vector3(0f, 2.5f, 40f), new Vector3(2f, 5f, 0.5f), matDoor);
            // Painel perto do caminho central da saida, com raio folgado, para o E sempre pegar.
            GameObject panelGo = Box("InteractPanel", new Vector3(1.7f, 1.1f, 38.5f), new Vector3(0.4f, 1.0f, 0.8f), matCrate);
            InteractPanel panel = panelGo.AddComponent<InteractPanel>();
            panel.door = door.transform;
            panel.doorOpenOffset = new Vector3(0f, -5.6f, 0f); // some toda abaixo do piso
            panel.interactRadius = 3.2f;
            gm.panels.Add(panel);
            gm.AddLabel(panelGo.transform, "conectar cabo (E) — abre a saida", GameColors.Ambar);

            // SAIDA
            GameObject exit = Box("ExitZone", new Vector3(0f, 1f, 42.5f), new Vector3(3f, 2f, 1.5f), matAmber, true);
            exit.GetComponent<Collider>().isTrigger = true;
            exit.GetComponent<Renderer>().enabled = false; // gatilho invisivel
            exit.AddComponent<ExitZone>();
            GameObject exitMark = Box("ExitMark", new Vector3(0f, 0.02f, 42.5f), new Vector3(2.5f, 0.04f, 2f), matAmber, false);
            gm.AddLabel(exitMark.transform, "SAIDA", GameColors.Ambar);
        }
    }
}
