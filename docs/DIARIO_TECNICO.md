# Diário Técnico - Robô Perdido (Etapa 7)

Registro honesto dos problemas técnicos encontrados durante a construção do vertical slice e
de como foram resolvidos. Material de base para o post-mortem da Etapa 8.

---

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
A caixa do Puzzle 1 simplesmente travava o jogador.

**Solução:** capturamos `OnControllerColliderHit`, identificamos o `PushBox` e aplicamos
`AddForceAtPosition` na direção do movimento. Travamos a rotação e o eixo Y do Rigidbody
(`FreezeRotation | FreezePositionY`) e subimos o `linearDamping` para a caixa parar de deslizar
quando o jogador solta. Empurrar custa bateria extra, como manda a Etapa 6.

### 3. Detecção do drone previsível (e que coberturas bloqueiem)
**Problema:** detecção só por distância ignorava as caixas — o jogador "se escondia" e mesmo
assim era visto, o que mata a mecânica de furtividade.

**Solução:** detecção em três testes — **distância**, **ângulo do cone** e **linha de visada**
(`Physics.Linecast`). Se uma cobertura está no caminho, o drone não vê. A linha de visada
começa 0,7 m à frente do drone para ele não bater no próprio colisor (bug que dava "falso
positivo"). Som: correr perto (`IsNoisy`) também denuncia, mesmo escondido — coerente com a
tabela de mecânicas.

### 4. Bateria sumindo sem o jogador perceber (Ajuste 1 do teste em papel)
**Problema:** no teste em papel, ninguém percebia que andar gastava energia até quase "morrer".

**Solução:** a HUD mostra um indicador "▼ gastando" sempre que há dreno recente e uma dica de
tutorial nos primeiros 9 segundos. O `BatterySystem` guarda `lastDrainTime`/`lastDrainAmount`
só para alimentar esse feedback.

### 5. HUD sem dependências
**Problema:** montar Canvas + TextMeshPro em runtime é verboso e adiciona dependências de
pacote que poluem o `manifest.json` nesta fase.

**Solução:** HUD inteira via **IMGUI (`OnGUI`)** — barra de bateria, chaves, setor, tempo,
banners e telas de vitória/derrota, além de "letreiros" projetados sobre os objetos para o
tutorial implícito. É placeholder assumido; será trocado por UI real numa etapa futura.

### 6. Eventos de gatilho com CharacterController
**Dúvida/risco:** células, chave, poça e saída usam `OnTrigger*`. O `CharacterController`
dispara esses eventos sem precisar de Rigidbody no jogador? **Sim** — confirmado. Mantivemos os
itens como `isTrigger` e lemos `GetComponentInParent<BatterySystem>()` para achar o jogador.

### 7. Pipeline de render
**Decisão:** ficamos no **Built-in Render Pipeline** (sem URP/HDRP) para usar `Shader.Find("Standard")`
e materiais com emissão simples, sem configurar um asset de pipeline. Suficiente para placeholders;
a migração para URP, se desejada, fica para a fase de arte.

---

## Pendências / riscos técnicos em aberto
- **Poça corrosiva:** checar colisores duplicados e reduzir o dreno para um valor que avise antes
  de matar (hoje funciona como morte quase instantânea).
- **Onboarding (playtest simulado):** comunicar "bateria = vida" no HUD (rótulo + tela de morte
  com a causa) e tornar claros o objetivo e a ordem Chave → Saída nos primeiros segundos.
- **Controles (playtest simulado):** revisar andar/correr (Shift) para evitar corrida acidental
  que dispara o drone — avaliar "manter pressionado para correr" e feedback visual do estado.
- **Célula de energia:** reposicionar para longe da poça e na altura do robô.
- **Drone:** desenhar um cone de visão visível para a furtividade ficar legível.
- **Câmera:** a colisão por raycast não cobre corredores apertados — avaliar fade de parede.
- **HUD:** resolver a sobreposição de rótulos projetados.