# Robô Perdido - Etapa 7: Vertical Slice

Protótipo jogável do core gameplay loop de _Robô Perdido_, um puzzle-aventura 3D em
terceira pessoa. Você controla a Unidade M-37, um pequeno robô de manutenção preso na
fábrica Ferralis-9. A bateria funciona como vida: cada ação gasta energia, então cada
passo é uma decisão.

> Esta é uma prova de conceito, não uma demo. Tudo é placeholder geométrico: sem arte,
> som ou narrativa final. 

- **Engine:** Unity 6 (6000.4.11f1) · Render Pipeline Built-in · C#
- **Setor implementado:** Montagem (Setor 1)
- **Autores:** Emily Frade dos Santos (23.1.8001) · Luís Eduardo Bastos Rocha (23.1.8095)
- **Disciplina:** Design e Desenvolvimento de Jogos — UFOP

---

## Vídeo de demonstração

Gameplay do protótipo de ponta a ponta no Setor Montagem:

▶️ **[Assistir no YouTube](https://youtu.be/_a3xo_OouDw)**

## Como rodar

Há duas formas de jogar: baixar o executável pronto (mais rápido) ou abrir o projeto no Unity.

### Opção 1 — Baixar o executável (recomendado, apenas Windows)

1. Acesse a página no itch.io: **[emilyfrade.itch.io/robo-perdido](https://emilyfrade.itch.io/robo-perdido)**.
2. Baixe o pacote (`.zip`).
3. Extraia a pasta.
4. Execute o arquivo `RoboPerdido.exe`.

> Não precisa instalar o Unity nem clonar o repositório.

### Opção 2 — Abrir no Unity (a partir do código-fonte)

1. Instale o **Unity Hub** e o editor **Unity 6000.4.11f1** (ou compatível 6000.x).
2. Clone este repositório.
3. No Unity Hub: **Add → From disk** e selecione a pasta `RoboPerdido/`.
4. Abra a cena `Assets/Scenes/SectorMontagem.unity`.
5. Pressione **Play**.

> A fase inteira é montada em tempo de execução pelo script `Bootstrap.cs`. A cena salva
> contém apenas um objeto (`GameBootstrap`); por isso o repositório não guarda assets
> binários pesados e o protótipo é totalmente reproduzível.

## Controles

| Tecla               | Ação                                              |
| ------------------- | ------------------------------------------------- |
| `WASD` / setas      | Mover (anda devagar, gasta pouca bateria)         |
| `Shift`             | Correr (mais rápido, gasta mais, faz **barulho**) |
| `C` / `Ctrl`        | Agachar (lento, porém **silencioso**)             |
| `E`                 | Interagir / conectar painel                       |
| `R`                 | Reiniciar o setor                                 |
| `F11` / `Alt+Enter` | Alternar tela cheia / janela                      |

## Objetivo da demo

Atravessar o Setor Montagem economizando bateria: empurrar a caixa para abrir o portão,
passar pela sala do drone usando as coberturas, pegar a **Chave 1/3**, conectar o cabo no
painel (`E`) para abrir a porta e chegar à **Saída**. Bateria a **0% = derrota**.

## Mecânicas implementadas (core loop)

- Sistema de **bateria como vida** com custos crescentes (parado < andar < correr < empurrar).
- **Mover / correr / agachar** com feedback de velocidade, ruído e gasto.
- **Empurrar/puxar** caixas físicas (Puzzle 1).
- **Interagir/conectar** painel que abre a saída (Puzzle 2).
- **Coletar células de energia** (recarga / alívio do loop).
- **Drone de patrulha** com rota fixa, detecção por **visão (cone + linha de visada)** e
  **audição** (se você correr por perto). Coberturas bloqueiam a visão.
- **Poça corrosiva** que drena bateria (ensino visual de perigo).
- **HUD** com bateria, contador de chaves, setor e tempo.
- **Vitória / Derrota** e reinício.