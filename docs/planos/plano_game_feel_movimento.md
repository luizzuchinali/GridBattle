---
tags:
  - plano
  - game-feel
created: 2026-10-01
status: implementado (hit stop fora do escopo)
---

# Plano — Game feel: movimento, ritmo do turno e combate

> Plano diretivo para agente de IA. Leia `AGENTS.md` antes (estrutura, convenções, seção **Cuidados** e **Verificação**) e siga-o: toda mudança estrutural deve ser refletida lá, com entrada no Histórico de refatorações.

## Contexto (estado atual, verificado no código)

- Jogo tático por turnos, grid 6×6, PPU 100, Pixel Perfect Camera, LitMotion. Células de 32×46 px com 2 px de gap (pitch 34×48 px).
- **Lógica imediata, visual atrasado**: `GridController.Move` troca a ocupação e `CurrentGridPos` na hora e chama `GridMovementAnimator.Animate(entity, fromWorld, fromCell, toCell, settings)`. O entity já está parentado na célula nova; o animator só o mantém visualmente no `start` e o leva ao `end`. Regras e IA nunca dependem da posição visual.
- `GridMovementSettings` (SO em `Settings/Movement/`): `hopsPerCell=2`, `hopHeight=0.04`, `hopDuration=0.12` → 240 ms por célula. O trajeto é Lerp linear + arco `|sin|`, sem ease, sem squash.
- `TurnManager`: `EEnemyTurnPacing.Sequential` espera `WaitForMovementsAsync` entre inimigos. A ordem dos inimigos vem de `FindObjectsByType<EnemyController>` **sem ordenação** (muda entre runs). Durante a vez dos inimigos os toques são ignorados e não há como acelerar.
- `Cell.OnPointerClick` faz squash na célula inteira (escala −4 px, 2×0,1 s, `InOutBounce`) a cada toque, o que também escala o conteúdo e acontece enquanto o personagem entra nessa célula.
- Ataque (`Character.Attack` → `ReceiveDamage`) só dispara o texto de dano flutuante; não há animação de ataque, reação de acerto nem de morte. `Character.Die()` faz `Destroy` imediato.

## Objetivo

Melhorar o game feel sem mexer na lógica de jogo: (1) poder escolher entre dois estilos de movimento, **Hop** (ajustado) e **Flip** (célula "virando"), comparando jogando; (2) tornar o turno dos inimigos mais ágil e legível; (3) dar impacto a ataque, acerto e morte.

Princípio de tempo: o turno inteiro deve caber em ~1,2 s com 5 inimigos. Player: 100–150 ms por movimento. Inimigo: 80–120 ms por movimento. Ataque: 150–250 ms.

## Decisões já tomadas

- Os dois estilos coexistem e são escolhidos em `GridMovementSettings`; não remover o hop.
- Pixel art: **nunca** escalar com valores contínuos que produzam colunas de pixel de larguras irregulares; quantizar o scale em passos (ver Fase 1).
- Manter a regra "lógica imediata, visual atrasado". Quem precisa esperar animações usa `GridController.WaitForMovementsAsync` (que deve passar a cobrir também ataque e morte).
- Tudo ajustável por ScriptableObject/Inspector, sem números mágicos no código.

---

## Fase 1 — Estilo de movimento (Hop / Flip) e feedback de chegada

**Arquivos:** `Gameplay/Movement/GridMovementSettings.cs`, `GridMovementAnimator.cs`, `Gameplay/Cell.cs`, `Settings/Movement/GridMovementSettings.asset`.

1. **Settings.** Adicionar `enum EMovementStyle { Hop, Flip }` e o campo `style` (default `Flip` para teste). Agrupar os campos com `[Header]`:
   - Hop: `hopsPerCell` (novo default **1**), `hopHeight`, `hopDuration`, `hopEase`, `landingSquash` (em pixels, ex.: 2 px de achatamento vertical na chegada) e `landingSquashDuration` (~0,06 s).
   - Flip: `flipCloseDuration` (0,07 s), `flipOpenDuration` (0,09 s), `flipOvershoot` (0,1), `scaleSteps` (default 4; 0 = contínuo) e ease de fechar/abrir.
   - Comum: `arrivalPulsePixels` (default 2) e `arrivalPulseDuration` (0,08 s).
   - Expor um `SpeedMultiplier` não serializado no animator (usado na Fase 2), dividindo todas as durações.
2. **Animator.** `Animate` passa a despachar por `style`. Preservar o contrato atual: `Stop(entity)` deve deixar o entity em estado final válido (posição `end`, `localScale = Vector3.one`) mesmo que a animação seja interrompida no meio.
   - **Hop:** 1 hop por célula (arredondar `distance * hopsPerCell`, mínimo 1), progresso com ease (não linear) e squash na aterrissagem.
   - **Flip:** manter o entity em `start`; `scaleX` 1→0 em `flipCloseDuration`; teleportar para `end`; `scaleX` 0→(1+overshoot)→1 em `flipOpenDuration`. O flip é aplicado **apenas ao entity (sprite + barra de vida, se for filho)**, não à moldura da célula. Quantizar o scale no `Bind`: `Mathf.Round(x * steps) / steps` quando `scaleSteps > 0`.
   - Garantir que o entity seja desenhado **acima de todas as células** durante o movimento (conferir `sortingOrder` do `SpriteRenderer` do entity vs. o da célula de destino; se necessário, elevar temporariamente e restaurar ao final).
   - Verificar a API do `LSequence` na versão instalada do LitMotion e usar um callback final explícito para normalizar posição/scale (não depender só de `handle.Complete()`).
3. **Chegada.** Ao concluir o movimento, chamar `Cell.PlayArrivalPulse(pixels, duration)` na célula de destino (novo método em `Cell`). Usar a mesma abordagem em pixels do tap atual (`4 / Ppu`), nunca fatores fracionários.
4. **Conflito com o tap.** Em `Cell.OnPointerClick`, o squash de 0,2 s sobre a célula inteira colide com a animação de chegada. Mudar para: toque **válido** (que vira ação) → sem squash (o pulse de chegada cobre); toque **inválido** → manter um feedback curto (squash ou shake horizontal de 2 px). Isso exige que o `PlayerCharacterController` sinalize à célula se o toque foi aceito; manter a mudança mínima (ex.: o controller chama um método de feedback na célula em vez de a célula decidir sozinha).
5. **Asset.** Atualizar `GridMovementSettings.asset` com os novos defaults.

**Critérios de aceite:** alternar `style` no Inspector muda o movimento de player e inimigos sem erros no console; interromper um movimento no meio (novo movimento do mesmo entity) não deixa scale ou posição quebrados; nenhum sprite aparece "achatado em larguras irregulares" no flip.

## Fase 2 — Ritmo do turno

**Arquivos:** `Managers/TurnManager.cs`, `GridMovementAnimator.cs` (SpeedMultiplier), `GridController.cs`.

1. **Ordem determinística dos inimigos.** Ordenar por distância ao player (menor primeiro), desempate por `y` e depois `x` de `CurrentGridPos`. Remover a dependência da ordem não ordenada de `FindObjectsByType`. Atualizar o comentário da classe, que hoje documenta essa instabilidade.
2. **Novo modo `Staggered`** em `EEnemyTurnPacing`: cada inimigo chama `Act()` com intervalo `enemyStagger` (campo serializado, default 0,08 s) em vez de esperar o movimento anterior terminar; ao final espera todos os movimentos. Como a lógica é imediata, a decisão de cada inimigo já enxerga o estado atualizado do anterior, então isso é seguro. Tornar `Staggered` o default. Manter `Sequential` e `Simultaneous`.
3. **Acelerar a vez dos inimigos.** Durante `ETurnOwner.Enemies`, um toque em qualquer lugar (o `TurnManager` pode assinar `CellTapEvent`) define `SpeedMultiplier = 2` até o fim do turno; resetar para 1 ao devolver a vez ao player e em `GridInitializedEvent`. O `enemyStagger` do `TurnManager` também é dividido pelo multiplicador. Não usar `Time.timeScale`.
4. **Highlights escalonados.** Ao devolver a vez ao player, os highlights da `Cell` aparecem com 20–30 ms de atraso entre cada um (fade/scale-in curto) em vez de ligar de uma vez. Cuidado: `Cell.Update` e `PlayerCharacterController.Update` reaplicam o highlight todo frame; o escalonamento deve ser um efeito visual na `Cell` (ex.: animação do `highlightRenderer`), não uma mudança nas regras.

**Critérios de aceite:** com 5 inimigos andando, o turno dos inimigos leva ~1 s no 1x; a ordem de ação é a mesma entre runs com o mesmo estado; tocar durante a vez dos inimigos acelera sem pular lógica nem quebrar estado.

## Fase 3 — Ataque, acerto e morte

**Arquivos:** `Entities/Character.cs`, `Entities/CharacterView.cs`, `GridMovementAnimator.cs` (ou novo `GridEffectsAnimator`), `GridController.cs`, `Cell.cs`.

1. **Registrar efeitos no mesmo rastreador de animações** para que `GridController.IsAnimatingMovement`/`WaitForMovementsAsync` cubram ataque e morte (considerar renomear internamente para algo como "animations"; manter a API pública ou atualizar todos os chamadores).
2. **Lunge de ataque.** `GridController.PlayAttackAnimation(GridEntity attacker, Vector2Int targetPos)`: o atacante avança ~4–6 px em direção ao alvo e volta (~150 ms total, ease out/in). Chamar a partir dos pontos de ataque existentes (`PlayerCharacterController`, `BasicAttackAction`, `UseSkillsAction`/skills). A lógica de dano continua imediata.
3. **Reação de acerto.** No `OnHpChanged` do alvo: flash e pequeno recuo de 1–2 px com retorno. Atrasar a reação visual para o ponto de impacto do lunge (metade da duração). Versão 1: flash por tint do `SpriteRenderer` (branco/vermelho por 2–3 frames); se o material atual permitir, usar um material de flash.
4. **Morte de inimigo.** `Die()` continua desocupando a célula e levantando `CharacterDiedEvent` na hora (lógica imediata), mas o `Destroy` passa a acontecer depois de um efeito curto (flash + fade ou flip fechando sem reabrir, ~150 ms). Garantir que o inimigo morto não seja alvo de nada nesse intervalo (`EnemyController.Act` já checa `IsDead`; conferir `XpRewardSystem` e os orbs de XP, que usam a posição do morto). Para o **player**, manter o `Destroy` imediato (a tela é trocada no `GameFlowController`).
5. **Hit stop (opcional, deixar por último).** ~50 ms de pausa no impacto. Não usar `Time.timeScale` se isso congelar o `TurnManager`/UI; preferir pausar só os motions dos envolvidos. Se ficar invasivo, não implementar e registrar como pendência.

**Critérios de aceite:** o turno só passa ao próximo ator quando o lunge terminou; matar um inimigo não gera exceções (`MissingReferenceException`) nos orbs de XP nem no turno; morte do player continua levando ao menu.

## Fase 4 — Documentação e verificação

1. Atualizar `AGENTS.md`: tabela de scripts (`Gameplay/Movement/`), a linha "Ajustar pulos do movimento" (agora estilo, flip e chegada), o fluxo em "Fluxos principais" (novo pacing `Staggered`, speed-up, ataque/morte animados), a seção **Cuidados** (flip quantizado, `Stop` deve normalizar scale/posição, efeitos registrados no rastreador) e uma entrada em **Histórico de refatorações**.
2. Sincronizar `docs/design/sistema_combate.md` com o ritmo do turno e os efeitos de combate.
3. Adicionar este plano ao índice em `docs/_index_.md`.
4. **Verificar** conforme a seção Verificação do `AGENTS.md`: `unity command recompile` + `get_console_logs` sem erros; script de Play Mode que joga alguns turnos (andar, atacar, matar um inimigo, morrer) nos dois estilos e confirma que não há exceções e que `WaitForMovementsAsync` sempre completa. Registrar no PR o que foi e o que não foi verificado visualmente (o game feel em si precisa ser avaliado jogando no Editor).

## Ordem sugerida de entrega

Uma fase por commit/PR: Fase 1 → Fase 2 → Fase 3 → Fase 4 (docs podem acompanhar cada fase). Ao fim da Fase 1, parar e deixar o autor avaliar Hop vs. Flip jogando antes de seguir.

## Pontos para confirmar com o autor antes de implementar

- O flip deve afetar só o sprite (padrão deste plano) ou a célula inteira?
- O hit stop vale o risco de complexidade, ou fica fora do escopo?
- Há áudio no projeto? Se sim, indicar onde plugar sons de passo/impacto; se não, deixar apenas pontos de gancho (eventos) sem implementar.
