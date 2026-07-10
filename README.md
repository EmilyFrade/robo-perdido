# Robô Perdido — Demo

Demo jogável de _Robô Perdido_, um puzzle-aventura 3D em terceira pessoa. Você controla a
Unidade M-37, um pequeno robô de manutenção preso na fábrica Ferralis-9. A bateria funciona
como vida: cada ação gasta energia, então cada passo é uma decisão.

> Jogo de ponta a ponta: **classificação indicativa → menu → 4 fases → portão final → fim**.
> Cada fase é um setor: pegue a chave, saia pela porta e a próxima fase começa. A arte
> (texturas, props e o robô), o áudio e os 4 mapas são **gerados por código** (sem assets
> binários no repositório).

- **Engine:** Unity 6 (6000.4.11f1) · Render Pipeline Built-in · C#
- **Setores (fases):** 1) Montagem · 2) Caldeiras · 3) Subsolo Químico · 4) Servidores
- **Classificação indicativa:** **Livre** (tensão/suspense leves; sem violência explícita)
- **Autores:** Emily Frade dos Santos (23.1.8001) · Luís Eduardo Bastos Rocha (23.1.8095)
- **Disciplina:** Design e Desenvolvimento de Jogos — UFOP

---

## Vídeo de demonstração

▶️ **[YouTube](https://youtu.be/qfNR4nrBj64)** _(gameplay da demo)_

## Como rodar

Há duas formas de jogar: no navegador (mais rápido) ou abrindo o projeto no Unity.

### Opção 1 — Jogar no navegador (recomendado)

1. Acesse a página no itch.io: **[emilyfrade.itch.io/robo-perdido-demo](https://emilyfrade.itch.io/robo-perdido-demo)**.
2. Clique em **Run game** e aguarde o carregamento (build WebGL).
3. Clique na tela para o mouse ser capturado (mouse-look) e use o botão de tela cheia.

> Não precisa instalar o Unity, baixar nada nem clonar o repositório. Requer um navegador com suporte a WebGL 2.0.

### Opção 2 — Abrir no Unity (a partir do código-fonte)

1. Instale o **Unity Hub** e o editor **Unity 6000.4.11f1** (ou compatível 6000.x).
2. Clone este repositório e, no Unity Hub, **Add → From disk**, selecionando a pasta do projeto.
3. Abra a cena `Assets/Scenes/SectorMontagem.unity` e pressione **Play**.

> Cada fase é montada em tempo de execução pelo `Bootstrap.cs` conforme `GameManager.currentLevel`.
> A cena salva contém apenas o objeto `GameBootstrap`; ao concluir um setor, a cena é recarregada
> e o próximo é montado. Por isso o repositório não guarda assets binários pesados.

## Controles

| Tecla               | Ação                                           |
|---------------------|------------------------------------------------|
| Mouse               | Girar a câmera / olhar ao redor do robô        |
| `WASD` / setas      | Mover (relativo à câmera; gasta pouca bateria) |
| `Shift`             | Correr (mais rápido, gasta mais, faz barulho)  |
| `C` / `Ctrl`        | Agachar (lento, porém silencioso)              |
| `E`                 | Interagir (resolver o puzzle da sala)          |
| `ESC`               | Pausar / retomar                               |
| `Espaço`            | Avançar (na tela de "setor concluído")         |
| `R`                 | Reiniciar o setor atual                        |
| `M`                 | Voltar ao menu (nas telas de fim)              |
| `F11` / `Alt+Enter` | Alternar tela cheia / janela                   |

## Objetivo

Atravessar os **4 setores** economizando bateria. Cada setor é um **mapa de salas conectadas
por portas**: em cada sala, **resolva o puzzle** para liberar a porta e seguir. Na sala final,
pegue a **Chave** do setor para abrir a **porta final** (ela não abre sem a chave) e chegar à
**Saída** — a próxima fase começa. No último setor (Servidores), com as 4 chaves, o **Portão
Externo** se abre e você escapa de Ferralis-9. Bateria a **0% = derrota** (reinicia o setor).

## Os quatro setores

Cada nível é uma **grade de 5 salas** com caminho que vira (formas diferentes por nível) e uma
**rampa** para uma plataforma elevada. Cada sala tem um puzzle e props temáticos.

| Fase | Setor | Tema / perigos | Props característicos |
| --- | --- | --- | --- |
| 1 | **Montagem** | drone (furtividade), poça corrosiva | mesas, cadeiras, computadores, braços robóticos, **esteira** |
| 2 | **Caldeiras** | **jatos de vapor** (fumaça real, temporizados) | tanques de caldeira, bocais |
| 3 | **Subsolo Químico** | escuro, poças corrosivas, **drones** (furtividade) | tonéis de químico, coberturas |
| 4 | **Servidores** | portão final, drone, narrativa por hologramas | **racks com LEDs**, hologramas, estações |

### Puzzles das salas

Cada porta de sala abre ao resolver um desafio: **apertar botão**, **ligar cabos** (popup),
**responder múltipla escolha** (popup) e **limpar a poeira dos contatos** (popup). A porta
final de cada setor é um **portão de chave** — só abre depois de coletar a chave do setor.