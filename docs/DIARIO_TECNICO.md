# Diário Técnico - Robô Perdido

Registro honesto dos problemas técnicos encontrados do *vertical slice* (Etapa 7) à demo
completa (Etapa 8) e de como foram resolvidos. Material de base para o post-mortem.

---

## Etapa 7 — Vertical Slice

### 1. "Abrir e jogar" sem quebrar o repositório
**Problema:** cenas `.unity` do Unity são arquivos grandes e cheios de GUIDs; commits de cena
geram conflitos horríveis em trabalho de dupla, e assets binários incham o histórico.

**Solução:** a cena salva contém **um único objeto** (`GameBootstrap`). Toda a fase é montada
em runtime por `Bootstrap.cs` com primitivas (`GameObject.CreatePrimitive`). Resultado: o
repositório só versiona texto (scripts), o diff é legível, e qualquer integrante abre a cena e
dá Play sem configurar nada na hierarquia.

**Custo aceito:** não dá para "arrastar e posicionar" no editor; o level design vive em código.
Para um vertical slice de uma fase, valeu a pena.

### 2. Empurrar a caixa — CharacterController × Rigidbody
**Problema:** o `CharacterController` do Unity **não empurra** Rigidbodies automaticamente.
A caixa do Puzzle 1 travava o jogador.

**Solução (Etapa 7):** capturamos `OnControllerColliderHit`, identificamos o `PushBox` e
aplicamos força na direção do movimento, travando a rotação e o eixo Y do Rigidbody e subindo o
`linearDamping` para a caixa parar quando o jogador solta.

**Atualização (Etapa 8):** a caixa empurrável foi **cortada** na reformulação dos puzzles (as
salas passaram a usar `PuzzleStation`); o `PushBox` saiu do projeto — por isso a mecânica não
aparece na demo.

### 3. Detecção do drone previsível (e que coberturas bloqueiem)
**Problema:** detecção só por distância ignorava as caixas — o jogador "se escondia" e mesmo
assim era visto, o que mata a mecânica de furtividade.

**Solução:** detecção em três testes — **distância**, **ângulo do cone** e **linha de visada**
(`Physics.Linecast`). Se uma cobertura está no caminho, o drone não vê. A linha de visada
começa 0,7 m à frente do drone para ele não bater no próprio colisor (bug que dava "falso
positivo"). Som: correr perto (`IsNoisy`) também denuncia, mesmo escondido.

### 4. Bateria sumindo sem o jogador perceber (Ajuste 1 do teste em papel)
**Problema:** no teste em papel, ninguém percebia que andar gastava energia até quase "morrer".

**Solução:** a HUD mostra um indicador "▼ gastando" sempre que há dreno recente e uma dica de
tutorial nos primeiros 9 segundos. O `BatterySystem` guarda `lastDrainTime`/`lastDrainAmount`
só para alimentar esse feedback.

### 5. HUD sem dependências
**Problema:** montar Canvas + TextMeshPro em runtime é verboso e adiciona dependências de
pacote que poluem o `manifest.json` nesta fase.

**Solução:** HUD inteira via **IMGUI (`OnGUI`)** — barra de bateria, chaves, setor, tempo,
banners e telas de vitória/derrota, além de "letreiros" projetados sobre os objetos. É
placeholder assumido; será trocado por UI real numa etapa futura.

### 6. Eventos de gatilho com CharacterController
**Dúvida/risco:** células, chave, poça e saída usam `OnTrigger*`. O `CharacterController`
dispara esses eventos sem precisar de Rigidbody no jogador? **Sim** — confirmado. Mantivemos os
itens como `isTrigger` e lemos `GetComponentInParent<BatterySystem>()` para achar o jogador.

### 7. Pipeline de render
**Decisão:** ficamos no **Built-in Render Pipeline** (sem URP/HDRP) para usar `Shader.Find("Standard")`
e materiais com emissão simples, sem configurar um asset de pipeline. Suficiente para placeholders;
a migração para URP, se desejada, fica para a fase de arte.

### Pendências / riscos identificados na Etapa 7 (a maioria endereçada na Etapa 8)
- **Poça corrosiva:** checar colisores duplicados e reduzir o dreno para um valor que avise antes
  de matar (funcionava como morte quase instantânea).
- **Onboarding:** comunicar "bateria = vida" no HUD (rótulo + tela de morte com a causa).
- **Controles:** revisar andar/correr (Shift) para evitar corrida acidental que dispara o drone.
- **Drone:** desenhar um cone de visão visível para a furtividade ficar legível.
- **HUD:** resolver a sobreposição de rótulos projetados.

---

## Etapa 8 — Demo completa (4 setores)

Expansão do *vertical slice* para uma demo de ponta a ponta (classificação → menu → 4 fases →
portão final → fim), com arte, áudio e os ajustes vindos do playtest. Tudo continua **gerado por
código**: nenhum asset binário novo entrou no repositório.

### 8. Máquina de estados sem trocar de cena
**Problema:** a demo precisa de menu, classificação indicativa, pausa e telas de fim, mas o
projeto inteiro vive numa cena só (`SectorMontagem`, montada por `Bootstrap`).

**Solução:** `GameManager` virou uma máquina de estados (`Classind → Menu → Controls/Credits
→ Playing → Paused → Win/GameOver`) desenhada via IMGUI. As transições que precisam de mundo
limpo (Jogar, Reiniciar, Menu) **recarregam a cena** e um campo `static bootState` diz em que
estado a cena deve subir. Isso reaproveita o `Bootstrap` como "reset" e evita estado sujo.
A pausa usa `Time.timeScale = 0`.

### 9. Arte por código: texturas e robô multi-parte
**Decisão:** em vez de importar texturas, `TextureFactory` pinta `Texture2D` em runtime
(placas de piso, painéis de parede, ferrugem, ácido, faixa de caminho e janelas) com ruído +
manchas **determinísticas** (semente fixa → resultado igual em qualquer máquina). O tiling é
aplicado por objeto via `material.mainTextureScale`. O M-37 deixou de ser um cubo e passou a ser
montado com várias primitivas (chassi, cabeça, olho luminoso, esteiras, antena, mochila).

### 10. Áudio sintetizado em runtime
**Decisão:** `SoundManager` gera todos os clips por osciladores (seno/quadrada/dente) + ruído
e `AudioClip.SetData`, sem arquivos `.wav`. O ambiente é um zumbido em loop com frequências
inteiras no comprimento (loop sem clique).

### 11. Cone de visão e a escala não-uniforme do drone
**Problema:** o cubo do drone tem escala `(0.9, 0.45, 0.9)`. Um cone (malha) **filho** herdaria
essa escala, distorcendo o leque e tirando-o do chão.

**Solução:** `VisionCone` constrói a malha do leque e é um objeto **independente** que segue a
posição/yaw do drone em `LateUpdate`, mantendo escala 1. Fica âmbar na patrulha e vermelho ao
detectar — tornando a furtividade legível (pedido do playtest).

### 12. Cache de build "fantasma" (Bee) após mover o projeto
**Problema:** a primeira build em batchmode falhou com "Scripts have compiler errors", mas
**nenhum** `error CS` aparecia. O log apontava um caminho antigo: o projeto havia sido copiado
para outra pasta e o cache incremental `Library/Bee` guardava caminhos **absolutos** da
localização anterior.

**Solução:** apagar `Library/Bee` e `Library/ScriptAssemblies` (regeneráveis, ignorados pelo
git) e rebuildar. Lição para o post-mortem: caches do Unity (`Library/`) não devem ser copiados
junto ao mover o projeto — só o que está versionado.

### 13. Progressão entre fases sem perder estado ao recarregar a cena
**Problema:** cada fase recarrega a cena (que o `Bootstrap` remonta), mas o progresso (setor
atual, chaves) não pode zerar nessa recarga.

**Solução:** o progresso vive em campos **estáticos** do `GameManager` (`currentLevel`,
`keysBanked`); a chave do setor atual é um bool de instância que só é "bancado" ao concluir o
setor — assim, morrer e reiniciar a fase não duplica a chave. `Bootstrap` lê `currentLevel` e
monta o setor correspondente.

### 14. Quatro setores sobre um mesmo esqueleto
**Decisão:** um `BuildShell` comum (corredor, divisórias, faixa de caminho, janelas, porta,
chave e saída) + um método por setor que adiciona paleta, perigos e props. Reaproveita o máximo
e isola o que muda. Componentes por setor: `Conveyor` (esteira), `SteamVent` (vapor temporizado),
`BlinkingLight` (LEDs/holograma).

### 15. Mapas em salas, puzzles, mouse-look e fumaça
- **Mapas não-lineares:** cada setor virou uma **grade de 5 salas** com caminho que vira (forma
  distinta por nível) e uma **rampa**. A grade garante geometria correta por construção (pisos e
  paredes alinham nos múltiplos da célula); só o "dono" (célula menor) constrói a parede
  compartilhada, evitando *z-fighting*.
- **Portas com puzzle:** `SlidingDoor` (desce ao abrir) + `PuzzleStation` (botão, ligar cabos,
  múltipla escolha, limpar poeira — popups IMGUI — e KeyGate). O `GameManager` ganhou o estado
  `Puzzle` (pausa com `timeScale=0`). A **porta final só abre com a chave** do setor.
- **Mouse-look:** `CameraFollow` orbita o robô (yaw/pitch) e trava o cursor só durante o jogo; o
  movimento (WASD) é relativo à câmera.
- **Fumaça real:** `SteamVent` passou de um retângulo translúcido para um `ParticleSystem`,
  emitindo enquanto o jato está aberto.

### 16. Polimento e ambientação (resumo)
Muitas iterações de acabamento, condensadas aqui:
- **Áudio:** trilha dinâmica em *crossfade* pela bateria (2 camadas — arpejo melódico calmo e
  dissonante tenso; loops sem clique); rotor 3D dos drones com *pitch* subindo na detecção; chiado
  do vapor, gotejar dos canos (`WaterDrip`) e rumor do portão (`GateClip`), todos 3D. *Ajuste:* a
  música ficava inaudível no início (volume baixo vs. ambiente/rotor) — subimos a música e baixamos
  o zumbido; os passos deixaram de ter som (era cansativo).
- **Atmosfera:** ambiente mais escuro por setor (`ambientLight`/sol reduzidos) para as luzes se
  destacarem; fog volumétrico + névoa (`ParticleSystem`) de indústria fechada; luzes de
  emergência e bastões de LED no teto.
- **Props e modelos:** detritos nos cantos (`DebrisPile`), prateleiras, extintores, pilha de
  energia (cilindro + raio), câmara de saída fechando o fundo atrás da porta; chave dourada
  montada; **M-37 enferrujado** (ferrugem, soldas, rebites, dois olhos vermelhos); **poças
  corrosivas redondas** com bolhas; **portão de grades pretas** que acende em verde ao abrir;
  **drone-quadricóptero** com 4 hélices girando (`Spinner`). Props em slots fixos para nada
  sobrepor.
- **Narrativa e finais:** história da Fase 1 com efeito **máquina de escrever**; no **Portão
  Externo**, **três finais** digitados e clicáveis (teclas 1/2/3), cada um com seu texto de
  desfecho.

### Ajustes do playtest da Etapa 7 aplicados
- "BATERIA = VIDA" explícito na HUD + **tela de morte que nomeia a causa** (dreno/poça/drone).
- Poça corrosiva: dreno de `14/s → 6/s`, com aviso antes de matar.
- **Cone de visão visível** do drone.
- Feedback claro de **correndo** (faz barulho) e **agachado** (silencioso).
- **Declutter** dos letreiros: só aparecem os próximos do robô (some o empilhamento inicial).
