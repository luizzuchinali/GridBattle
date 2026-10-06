---
tags:
  - design
  - combate
  - sistemas
created: 2026-09-29
updated: 2026-10-06
aliases:
  - Sistema de Combate
  - Turnos
---

# Sistema de Combate

Documento de referência do combate em grid do GridBattle **como está
implementado**: configuração de personagens, atributos e estados, cálculo de
dano, skills, consumíveis, ações do jogador, sistema de turnos, IA dos
inimigos, terreno, morte, XP e talentos, e os pontos de extensão. As regras de
design vivem no [[GDD]] e nos subdocumentos; este documento descreve como o
código as realiza e quais são os valores atuais dos assets.

Os valores moram nos assets em `Assets/Application/Settings/`. Ao alterar um
asset, atualize a seção [Valores atuais dos assets](#valores-atuais-dos-assets).
Os valores iniciais das perguntas em aberto estão em [[valores_padrao_em_aberto]].

Ver também: [[GDD]], [[estados_e_atributos]], [[classes_e_skills]],
[[inimigos]], [[grid_e_terreno]], [[consumiveis]], [[xp_e_niveis]],
[[talentos_e_oferta]], [[balanceamento_e_geracao]], [[mapa_e_nos]],
[[interface]].

## Índice

1. [Visão geral](#visão-geral)
2. [Grid e células](#grid-e-células)
3. [Regras centrais (GridRules)](#regras-centrais-gridrules)
4. [Configuração de personagens](#configuração-de-personagens)
5. [Atributos e estados](#atributos-e-estados)
6. [Cálculo de dano](#cálculo-de-dano)
7. [Skills](#skills)
   - [Empurrar e puxar (deslocamento)](#empurrar-e-puxar-deslocamento)
8. [Consumíveis em batalha](#consumíveis-em-batalha)
9. [Ações do jogador e entrada](#ações-do-jogador-e-entrada)
10. [Sistema de turnos](#sistema-de-turnos)
11. [IA dos inimigos](#ia-dos-inimigos)
12. [Terreno](#terreno)
13. [Morte e fim da batalha](#morte-e-fim-da-batalha)
14. [XP, nível e talentos](#xp-nível-e-talentos)
15. [Highlights visuais](#highlights-visuais)
16. [Pontos de extensão](#pontos-de-extensão)
17. [Valores atuais dos assets](#valores-atuais-dos-assets)

---

## Visão geral

O combate acontece em um **grid 2D de células** (6×6 por padrão). Cada célula
contém no máximo um entity (`GridEntity` → `Character` → `PlayerCharacter` ou
`Enemy`) e pode ter um terreno. Uma batalha é um nó do mapa da run (ver
[[mapa_e_nos]]): vence-se eliminando todos os inimigos, sem limite de turnos, e
perde-se ao morrer.

O jogo é **por turnos**. Em cada **turno global** o jogador joga primeiro, com
**uma ação**, e depois cada inimigo joga a sua. Cada entidade tem o seu próprio
turno dentro do turno global.

| Ação do jogador | Consome a ação do turno? |
|---|---|
| Andar | sim |
| Atacar (ataque básico) | sim |
| Usar skill | sim |
| Usar consumível | não (no máximo um por turno) |

Uma ação inválida não consome o turno.

> [!important]
> Jogador e inimigos seguem **exatamente as mesmas regras**: alcance,
> ocupação, dano, estados, skills e terreno passam pelas mesmas classes de
> regra (`GridRules`, `CombatResolver`, `StateContainer`, `SkillTargeting`).
> Nenhuma checagem é duplicada nos controllers.

> [!important]
> **Lógica não vive em controllers.** Os controllers (`PlayerCharacterController`,
> `EnemyController`, base `CharacterControllerBase<T>`) são apenas
> **roteadores**: leem o input (ou a IA configurada), perguntam ao
> character/grid se a ação é válida e executam via `Character` e
> `GridController`. Atributos, skills, estados, IA, terreno e consumíveis são
> **dados** (ScriptableObjects).

> [!important]
> **Lógica imediata, visual atrasado.** Ocupação das células, posição lógica,
> dano e morte mudam na hora; só o transform, os flashes e os efeitos animam.
> Regras e IA nunca dependem da posição visual.

Quem faz o quê:

| Peça | Responsabilidade |
|---|---|
| `GridController` | tabuleiro, spawn, ocupação, terreno, movimento, captura e restauração da batalha |
| `GridRules` | cálculos entre characters e grid (alcance, alvo válido, passo, highlights) |
| `CombatResolver` / `DamageCalculator` | dano, cura, escudo, crítico, defesa, roubo de vida e espinhos |
| `CharacterStats` / `StateContainer` | atributos em jogo e estados ativos de um personagem |
| `SkillDefinition` / `EffectiveSkill` / `SkillTargeting` / `SkillArea` / `SkillCooldowns` | skills: dados, valores efetivos com os talentos, mira, áreas e cooldowns |
| `ConsumableRules` / `ConsumableExecutor` / `ConsumableInventory` | consumíveis: uso, mira e inventário |
| `TurnManager` / `BattleController` / `TurnBlockers` | fluxo de turnos, fim da batalha e pausas |
| `EnemyBehavior` / `EnemyAction` / `EnemyMemory` | IA dos inimigos |
| `TerrainEffects` / `TerrainGenerator` | efeitos e geração do terreno |
| `XpRewardSystem` / `ProgressionSettings` / `TalentSession` | XP, nível e escolha de talentos |

## Grid e células

O `GridController` é dono do estado: matriz `Cell[,]`, tamanho (`Size`) e as
formas de montar o tabuleiro:

- **Batalha de uma run** (`InitializeBattle(spec, playerConfig, playerState)`):
  células do tamanho do `BattleSpec` (6×6 em `BattleGenerationSettings`), terreno
  do spec, jogador na célula do spec ((2, 2) por padrão) com o estado da run
  (nível, XP, skills, consumíveis, estados e vida que persistem) e os inimigos do
  spec com escala por profundidade e XP. O `RunManager` gera o spec (ver
  [[balanceamento_e_geracao]]).
- **Retomada** (`RestoreBattle(snapshot, ...)`): reconstrói exatamente a batalha
  salva (posições, vida, estados com os turnos restantes, cooldowns, memória da
  IA, invocados e se um consumível já foi usado no turno).
- **Editor** (`InitializeGrid(playerConfig)`, no `Awake` em edit mode e no botão
  **Reset grid**): jogador em (2, 2) a partir do `debugPlayerConfig`, inimigos do
  `EncounterConfig` (`DefaultEncounter`, os cinco inimigos básicos) cada um na
  primeira célula livre da varredura, e a lista `debugTerrain`. Serve só para
  testar; as runs nunca usam isso.
- `ClearBoard()` limpa tudo (o mapa é mostrado sobre um mundo vazio).

A criação de personagens é centralizada em `CharacterFactory.Spawn(config
[, escala])`; inimigos em runtime também nascem por `GridController.SpawnEnemy`
(célula válida, caminhável e livre; usado pelo gerador de batalhas e pela
invocação).

Cada `Cell` tem:

- `GridPosition` — coordenada lógica;
- `HasContent` / `GetContent()` / `SetContent()` / `RemoveContent()` — o entity que
  a ocupa;
- `Terrain` e `IsBlocked` — o terreno e se ele bloqueia a célula;
- highlight visual (`Highlighted` + `HighlightType`), flash de área e pulsos
  (chegada, rejeição, terreno), apenas visuais;
- entrada: clique/toque (`CellTapEvent`), toque longo e clique direito
  (`CellLongPressEvent`).

O `GridController` expõe consultas e execuções de estado:

| Membro | O que faz |
|---|---|
| `IsValidPosition(pos)` | dentro dos limites |
| `IsFreePosition(pos)` | sem conteúdo e sem obstáculo |
| `IsWalkable(pos)` | dentro dos limites e sem obstáculo |
| `GetContent(pos)` / `GetCell(pos)` / `GetTerrain(pos)` | entity, célula ou terreno da posição |
| `HasBlockedCells`, `BlocksSkillArea(pos)` | há obstáculos; o terreno da célula bloqueia áreas de skill |
| `MoveEntity(entity, pos)` | **execução centralizada de movimento**: move o conteúdo, atualiza `CurrentGridPos`, levanta `EntityEnteredCellEvent` e anima |
| `SetTerrain` / `ApplyTerrain` / `ClearTerrain` | define o terreno (recusa obstáculo sobre célula ocupada) |
| `CaptureBattle()` | foto exata da batalha (salva no início de cada vez do jogador) |
| `WaitForMovementsAsync`, `AnimationSpeed` | espera as animações e as acelera |

> [!note]
> No editor (fora do Play Mode), o `CharacterFactory` instancia os entities
> **linkados ao prefab-template** (`PrefabUtility.InstantiatePrefab`) e
> registra o config aplicado como override, então os clones na cena refletem
> mudanças no template. Em runtime, usa `Instantiate` normal.

> [!note]
> **Distâncias.** A métrica de cada alcance é configurável em `CombatSettings`
> (euclidiana, Manhattan ou Chebyshev), sem mudar código. Hoje o alcance de
> **andar e o de ataque básico** são **euclidianos**; o de **skills e
> consumíveis** é **Manhattan** (como o GDD define). Com alcance 1 só os 4
> vizinhos ortogonais são alcançáveis (a diagonal vale ≈ 1,41). Com alcance 2
> entram também as 4 diagonais adjacentes e as células a 2 passos em linha reta,
> mas não a posição (2, 1) (≈ 2,24).

## Regras centrais (GridRules)

`Gameplay/Rules/GridRules.cs` é o **único lugar** com cálculos entre
characters e grid: classe estática, funções puras. As distâncias vêm de
`GridDistance` e da `CombatSettings`.

| Função | Responsabilidade |
|---|---|
| `IsInWalkRange(from, to, walkDistance)` | alcance de movimento (métrica de movimento) |
| `IsInAttackRange(from, to, attackDistance)` | alcance do ataque básico (métrica de ataque) |
| `CanWalkTo(grid, character, target)` | posição válida, caminhável, livre e em alcance; havendo obstáculos, também alcançável por um caminho sem cruzá-los |
| `HasWalkPath(grid, from, to, walkDistance)` | caminho por células ortogonais não bloqueadas, todas dentro do alcance de `from`; personagens não bloqueiam o caminho, só o destino precisa estar livre |
| `IsAttackTarget(grid, attacker, target)` | alvo válido do ataque básico: célula válida e caminhável, o atacante pode atacar (sem `PreventBasicAttack`), em alcance, com um `IDamageReceiver` que **não** é o próprio atacante |
| `FindStepToward(grid, character, target)` | passo guloso de um turno: a célula alcançável mais perto do alvo (ou null se nenhuma aproxima); com obstáculos, segue a menor rota ao redor deles |
| `AreAllies(a, b)` | jogador × inimigos: os inimigos são aliados entre si |
| `GetHighlightInfos(grid, character)` | dicionário de highlights Walk/Attack |
| `GetSkillHighlightInfos(grid, caster, skill)` | highlights `SkillRange` das células onde a skill pode ser mirada |

Regras de negócio derivadas:

- **Um character nunca anda para uma célula ocupada ou bloqueada.**
- **Um character nunca ataca a si mesmo**: o ataque básico exclui o atacante por
  identidade (`content != attacker`). O filtro de aliados (`AreAllies`) vale para
  as áreas de skills e consumíveis, e a IA só mira o jogador.
- A área avaliada para os highlights de andar e atacar é o quadrado de raio
  `max(alcance de andar, alcance de ataque)`; a IA de movimento avalia o quadrado
  de raio igual ao alcance de andar.
- O passo guloso mede a distância ao alvo em linha reta. Só quando há
  obstáculos ele passa a seguir a menor rota (vizinhança ortogonal) ao redor
  deles, para ninguém ficar preso atrás de uma rocha.

## Configuração de personagens

Cada character é definido por um **objeto de configuração** (ScriptableObject,
um `DisplayableDefinition` com `id`, nome e descrição localizados e ícone) que
dita **tudo** o que o diferencia. Não existe prefab por personagem: todos
compartilham um **prefab-template** e recebem o config no spawn
(`CharacterFactory`).

```
CharacterConfig (SO, abstrato)
├── visual: sprite, animatorController, tint
├── atributos base: maxHp, walkDistance, attackDistance, basicAttackDamage
├── ofensivo: critChance, critMultiplier, defensePenetration
├── defensivo: defense
├── modificadores de skill: skillDamageBonus, skillRange, cooldownReduction
├── skills: List<SkillDefinition>        ← skills iniciais
└── initialStates: List<StateGrant>      ← estados aplicados no spawn

PlayerCharacterConfig : CharacterConfig    (Create > GridBattle > Characters)
├── prefab: PlayerCharacter               ← template compartilhado
├── characterClass: ECharacter            ← Warrior / Mage / Rogue
├── battlesToUnlock                        ← batalhas vencidas (no perfil) para liberar
└── talentPool: List<TalentDefinition>    ← talentos próprios da classe

EnemyConfig : CharacterConfig              (Create > GridBattle > Characters)
├── prefab: Enemy                         ← template compartilhado
├── role: EnemyRoleDefinition             ← papel (ícone, nome, fator de ameaça)
├── isBoss                                 ← chefe do nó final
├── behavior: EnemyBehavior               ← IA (ver "IA dos inimigos")
├── behaviorDescription                    ← texto localizado do comportamento
└── xpReward                               ← exclusivo do inimigo
```

A curva de XP e o nível máximo **não** ficam nos configs de classe: são globais
(`ProgressionSettings`; ver [XP, nível e talentos](#xp-nível-e-talentos)).

Prefabs-template em `Assets/Application/Prefabs/Entities/`:

```
Character.prefab          SpriteRenderer + Animator + CharacterView (base)
├── Enemy.prefab          (variant) + Enemy + EnemyController + EnemyIconsView
└── PlayerCharacter.prefab (variant) + PlayerCharacter + PlayerCharacterController
```

- `Character.Initialize(config, escala)` aplica o config: atributos base (com a
  escala), estados iniciais, vida cheia, nome do GameObject, lista de skills
  iniciais, cooldowns zerados e visual (`CharacterView.Apply` define sprite,
  **tint** e `RuntimeAnimatorController`). O `Awake` continua inicializando a
  vida de characters colocados à mão na cena com um config atribuído.
- **`PlayerCharacter`** tem o que `Enemy` não tem: `PlayerConfig`, `Class`
  (somente jogadores têm classe), `Level` / `CurrentXp`, o inventário de
  consumíveis e o contador de consumíveis usados no turno. Nível e XP vêm do
  estado da run no início de cada batalha (`SetProgress`).
- **`Enemy`** expõe `EnemyConfig`, `Role`, `XpReward` (o valor do config, a menos
  que o gerador de batalhas defina outro), `IsSummoned` e `Summoner`.
- A classe escolhida no menu é resolvida pelo `GameStateManager`
  (`playableCharacters` e `CharacterClass`).
- `EnemyIconsView` mostra sobre o inimigo o ícone do papel e até 2 ícones de
  estados permanentes (blindado, regenerante, espinhoso: Mecânica 6). Os ícones
  são 8×8 nativos, ampliados só por fator inteiro (pixel perfect).

### Escala por profundidade

Os inimigos de uma batalha nascem com um `CharacterScaling` (multiplicador de
vida e de dano) que multiplica a **vida máxima base**, o **dano básico base** e
o **dano das skills** do personagem. Não é um estado: não aparece nem pode ser
removido. Fórmula linear, por profundidade `p`:
`multiplicador = 1 + taxa × (p − 1)`, com taxa de vida **6%** e de dano **3%**
(`BattleGenerationSettings`; há opção de crescimento composto). Invocados herdam
a escala de quem os invocou. O XP escala à parte (ver [XP](#xp-nível-e-talentos)).

### Criando um personagem novo

- **Inimigo:** `Create > GridBattle > Characters > Enemy Config`; preencher
  sprite, animator, tint, atributos, `prefab = Enemy.prefab`, `role`, `behavior`,
  `xpReward` e, se for o caso, `initialStates`. Para aparecer nas batalhas, entra
  no pool de `BattleGenerationSettings` (com profundidade mínima/máxima e peso).
  Nenhum prefab novo.
- **Personagem jogável:** `Create > GridBattle > Characters > Player Character
  Config`; `prefab = PlayerCharacter.prefab`, `characterClass`, `battlesToUnlock`
  e `talentPool`; adicionar à lista `playableCharacters` do `GameStateManager`.
- Só crie um prefab próprio (variant do template) se o personagem precisar de
  componentes extras.

> [!note]
> `ECharacter` vive em `GridBattle.Gameplay.Entities` (domínio de gameplay).
> A UI apenas referencia: o enum não pertence à camada de UI.

## Atributos e estados

Duas camadas descrevem e alteram um personagem (ver [[estados_e_atributos]]):
os **atributos** (valores base do config) e os **estados** (efeitos com nome e
duração). O valor em jogo é o base somado aos estados ativos.

### Atributos

`EAttribute` e `CharacterStats`. Frações são guardadas de 0 a 1 (0,1 = 10%).

| Atributo | Descrição | Base no config |
|---|---|---|
| Vida máxima (`MaxHp`) | quantidade máxima de vida (inteiro) | `maxHp` × escala |
| Alcance de movimento (`WalkRange`) | distância que anda por ação (inteiro); vale 0 enquanto um estado impede o movimento | `walkDistance` |
| Alcance de ataque (`AttackRange`) | distância do ataque básico (inteiro) | `attackDistance` |
| Dano básico (`BasicDamage`) | dano do ataque básico (inteiro) | `basicAttackDamage` × escala |
| Chance de crítico (`CritChance`) | probabilidade (0 a 1) de um golpe ser crítico | `critChance` |
| Multiplicador de crítico (`CritMultiplier`) | quanto o crítico multiplica o dano (mínimo 1) | `critMultiplier` |
| Penetração de defesa (`DefensePenetration`) | fração (0 a 1) da defesa do alvo que é ignorada | `defensePenetration` |
| Defesa (`Defense`) | reduz o dano recebido, fixa ou em porcentagem (inteiro) | `defense` |
| Bônus de dano de skills (`SkillDamageBonus`) | fração extra de dano das skills | `skillDamageBonus` |
| Alcance de skills (`SkillRange`) | soma à distância máxima de mira das skills (inteiro) | `skillRange` |
| Redução de cooldown (`CooldownReduction`) | ações removidas do cooldown das skills (inteiro) | `cooldownReduction` |
| Dano causado (`DamageDealt`) | ajuste (fração) de todo o dano causado: −0,5 = causa 50% menos | só por estados (base 0) |
| Dano recebido (`DamageTaken`) | ajuste (fração) de todo o dano recebido: 0,2 = recebe 20% mais | só por estados (base 0) |

Fórmula: `valor = (base + Σ modificadores fixos) × (1 + Σ modificadores
percentuais)`. Nenhum atributo fica negativo, exceto `DamageDealt` e
`DamageTaken`. Os modificadores de um estado escalam com as suas pilhas. Quando um
estado muda a vida máxima, a vida atual nunca fica acima do novo máximo.

Esquiva e tamanho da área das skills continuam fora do conjunto
([[estados_e_atributos]]).

### Estados

Um `StateDefinition` (asset, `Create > GridBattle > States > State Definition`)
tem um **nome**, um tipo (`Buff`, `Debuff` ou `Neutral`), uma **política de
reaplicação**, o máximo de pilhas, se aparece nos detalhes e uma lista de
**efeitos** escolhidos no Inspector (`[SerializeReference]`). A **duração não
faz parte do estado**: quem o aplica decide (talento, skill, terreno, consumível
ou inimigo), com um `StateGrant` (estado + permanente ou N turnos do portador +
pilhas). Cada estado ativo é um `StateInstance` (turnos restantes, pilhas, fonte
e, para escudos, os pontos restantes).

| Efeito | O que faz |
|---|---|
| `AttributeModifierEffect` | altera atributos (valor fixo ou percentual); escala com as pilhas |
| `PeriodicDamageEffect` | dano fixo (+ fração da vida máxima) no início ou no fim de cada turno do portador |
| `PeriodicHealEffect` | cura fixa (+ fração da vida máxima) no início ou no fim de cada turno do portador |
| `ThornsEffect` | ao ser atingido por ataque, skill ou consumível, devolve dano fixo (+ fração do dano recebido) ao atacante |
| `LifeStealEffect` | ao causar dano com ataque, skill ou consumível, recupera uma fração dele como vida |
| `ShieldEffect` | vida temporária (valor fixo + fração da vida máxima) que absorve dano antes da vida real; o estado some quando ela esgota (configurável) |
| `BehaviorRestrictionEffect` | impede mover, atacar com o ataque básico e/ou usar skills (nenhum estado atual usa) |
| `RunModifierEffect` | altera as opções de talento, rerolls, banimentos ou pulos da run (só faz sentido no jogador) |

Os efeitos têm ganchos opcionais: aplicar, reaplicar, remover, início do turno,
fim do turno, causar dano, receber dano e absorver dano.

> [!important]
> Os estados valem para **jogador e inimigos**, com o mesmo modelo. Inimigos
> podem nascer com estados permanentes (`initialStates`), que é como se
> constroem os inimigos que cobram a build (Mecânica 6): Blindado, Regeneração e
> Armadura de Espinhos.

#### Duração

A duração conta em **turnos da entidade portadora** (regra do GDD 3.1), seja
buff ou debuff, e não importa quem aplicou. No código:

- no **início** do turno da portadora (`Character.BeginTurn`), os efeitos de
  início de turno rodam (veneno e regeneração, por exemplo);
- no **fim** do turno da portadora (`EntityTurnEndedEvent` e depois
  `Character.EndTurn`), as recargas das skills diminuem, os efeitos de fim de
  turno rodam e **por último** as durações diminuem em 1; o estado termina ao
  chegar a zero. Estados permanentes nunca diminuem;
- um estado aplicado **antes** de a portadora jogar no turno global atual conta
  esse turno como o primeiro; aplicado **depois**, o primeiro é o turno seguinte
  dela. Isso sai direto de descontar só no fim do turno da portadora;
- os estados que o terreno aplica no fim do turno são aplicados **antes** do
  desconto, então o turno corrente conta. Por isso pântano e santuário duram 3
  turnos e a pedra protetora 2: o primeiro desconto acontece no mesmo instante.

#### Reaplicação e acúmulo

Cada estado define a sua política (`EStackPolicy`). A padrão é renovar a
duração.

| Política | Reaplicar um estado que já está ativo |
|---|---|
| `RefreshDuration` (padrão) | mantém uma instância; a duração passa a ser a maior das duas (permanente vence) |
| `AddDuration` | mantém uma instância; as durações se somam |
| `AddStacks` | soma pilhas (até o máximo do estado) e renova a duração; os efeitos escalam com as pilhas |
| `IgnoreIfPresent` | a nova aplicação é ignorada |

Instâncias com **fontes diferentes** ficam separadas (por exemplo, uma por
talento: `sourceId` é o id do talento). Estados do terreno, de skills e da IA não
têm fonte.

#### Remoção

- por duração (acima) ou por efeito (escudo esgotado);
- `Remove`, `RemoveBySource` (os estados de um talento), `RemoveTemporary` e
  `Clear` no `StateContainer`;
- **ao mudar de batalha**, os estados com duração do jogador são removidos e os
  permanentes continuam (`RunSettings.temporaryStates`); os estados do config e os
  de talentos são reaplicados em cada batalha, e não viajam como estados comuns.

#### Catálogo atual (`Settings/States`)

| Estado | Tipo | Efeito | Onde aparece |
|---|---|---|---|
| Fraquejado | debuff | dano causado −50% | aplicado pelo Olho Maldito (2 turnos) |
| Armadura de Espinhos | buff | devolve 3 de dano a quem o atinge | Rato Espinhoso (permanente) |
| Roubo de Vida | buff | recupera 15% do dano causado | nenhum asset o aplica ainda (o talento Roubo de Vida usa um estado próprio, 5% por pilha) |
| Regeneração | buff | recupera 3 de vida no início de cada turno do portador | Slime Regenerante (permanente), Santuário (3 turnos), Tônico de Regeneração (3 turnos) |
| Escudo | buff | 15 pontos que absorvem dano antes da vida; some ao esgotar | Xamã Goblin (3 turnos), Pedra Protetora (2 turnos) |
| Envenenado | debuff | 3 de dano no início de cada turno do portador | Pântano Venenoso (3 turnos) |
| Blindado | buff | defesa +4 | Goblin Blindado e Guarda Goblin (permanente) |
| Queimando (G3) | debuff | 2 de dano no início de cada turno do portador, por pilha (até 3 pilhas) | Bola de Fogo com o talento Ignição (3 turnos) |
| Veneno letal (G3) | debuff | 1 de dano mais 5% da vida máxima no início de cada turno do portador e −6% de dano causado, por pilha (até 5 pilhas) | habilidades e ataques básicos do Ladino com os talentos Lâminas Envenenadas e Arma Envenenada (4 turnos) |
| Congelado (G3) | debuff | não anda (alcance de movimento −2) e causa 20% menos dano | Nova de Gelo com o talento Toque Gélido (1 turno) |
| Atordoado (G3) | debuff | não anda, não ataca básico e não usa skills | Golpe de Escudo com Concussão e Gancho com Gancho Atordoante (1 turno) |
| Exposto (G3) | debuff | dano recebido +25% | Golpe de Escudo com Golpe Abalador (2 turnos) |

Todos usam a política `RefreshDuration`, exceto Queimando e Veneno letal
(`AddStacks`). Os 15 estados de talentos genéricos usam `AddStacks` com até 5
pilhas, e os 43 estados dos talentos de classe (G3, um por talento, pilha =
posto; ver [Skills](#valores-efetivos-e-modificadores-effectiveskill) e
[Bônus condicionais](#bônus-condicionais-dos-estados-passivas)) têm tantas
pilhas quantos postos; ver [XP, nível e talentos](#xp-nível-e-talentos).

## Cálculo de dano

Todo dano e toda cura passam pelo `CombatResolver`: o ataque básico
(`BasicAttack`), as skills, os consumíveis, os estados e o terreno. O cálculo em
si (`DamageCalculator`) é puro e sem efeitos colaterais. O dano tem um **tipo**
(`EDamageKind`):

| Tipo | Origem | Pode ser crítico | Defesa | Dispara espinhos e roubo de vida |
|---|---|---|---|---|
| `BasicAttack` | ataque básico (dano = atributo Dano básico) | sim | sim | sim |
| `Skill` | skill (dano da skill escalado pelo atacante) | sim | sim | sim |
| `Consumable` | consumível (bomba) | sim | sim | sim |
| `Periodic` | dano ao longo do tempo (veneno) | não | ignora (configurável) | não |
| `Thorns` | espinhos | não | ignora (configurável) | não |
| `Terrain` | célula de perigo | não | ignora (configurável) | não |
| `Collision` | empurrado ou puxado contra obstáculo, borda ou personagem | não | ignora (configurável) | não |
| `Pure` | dano exato de efeitos roteirizados | não | não | não (nenhum modificador vale) |

Ordem do cálculo (dano base ≤ 0 resulta em 0):

0. **Bônus condicionais dos estados** (G3, ver [abaixo](#bônus-condicionais-dos-estados-passivas)):
   antes do cálculo, o `CombatResolver` pergunta aos estados do atacante e do
   alvo (`CombatResolver.Adjust`) e passa o resultado ao `DamageCalculator`
   (que continua puro). Sem passivas, nada muda e a conta é a de sempre.
1. **Skill:** `(dano + bônus fixo condicional) × (1 + bônus de dano de skills)`
   do atacante.
2. `× (1 + Dano causado)` do atacante, se houver atacante (mínimo 0), e
   `× (1 + bônus condicional de dano causado)`.
3. **Crítico** (ataque básico, skill e consumível): a chance é a do atacante,
   limitada pelo teto (`critChanceCap`). O sorteio só acontece se a chance for
   maior que 0, e usa o fluxo `Combat` da seed: personagens sem crítico não
   consomem sorteios. Em crítico, `× max(1, multiplicador de crítico)`.
4. **Defesa** (exceto `Periodic`, `Thorns`, `Terrain` e `Collision`, configurável): a
   penetração do atacante (fração) reduz a defesa efetiva,
   `defesa efetiva = defesa × (1 − penetração)`.
   - **Fixa** (padrão): `dano − defesa efetiva`.
   - **Percentual:** `dano × (1 − min(defesa efetiva / 100, teto))`, com teto de
     80%.
5. `× (1 + Dano recebido)` do alvo (mínimo 0) e `× (1 + redução condicional)`
   (passivas defensivas; negativa = menos dano).
6. **Arredondamento** (padrão: ao mais próximo) e **dano mínimo** de 1:
   `max(dano mínimo, arredondado)`.

Depois do cálculo (`Character.ApplyHit`):

7. Os **escudos** do alvo absorvem primeiro (na ordem dos estados); o resto sai
   da vida. O `HitResult` guarda o dano total, o absorvido, o que foi à vida,
   se foi crítico e se matou.
8. A vida muda e o texto de dano flutua sobre a célula. Se a vida chegou a 0,
   `Die()`; senão a reação de acerto anima.
9. `DamageDealtEvent` (áudio, métricas), depois os estados do **atacante**
   reagem (roubo de vida: cura `fração × dano total × pilhas`) e os do **alvo**
   reagem (espinhos: devolve o dano fixo, mais a fração do recebido, ao atacante
   vivo). O dano devolvido por espinhos é do tipo `Thorns`, então não dispara
   espinhos nem roubo de vida de volta.

> [!note]
> Os espinhos reagem a todo golpe direto recebido, inclusive o que mata o
> portador. O dano de espinhos também é afetado pelo Dano causado do portador
> (um portador Fraquejado devolve metade) e pelo Dano recebido do atacante.

**Cura** (`CombatResolver.Heal` / `HealFraction`): recupera até a vida máxima
e levanta `HealedEvent`. Cura em personagem morto não faz nada.

### Bônus condicionais dos estados (passivas)

Balanceamento G3 (2026-10-06). Um estado pode mudar o dano de um golpe conforme a
situação, sem sorteios e sem mexer no resto: o `CombatResolver` chama, antes do
cálculo, `StateEffect.ModifyOutgoingDamage` (estados do atacante) e
`ModifyIncomingDamage` (estados do alvo), que preenchem um `DamageAdjustment`
(bônus fixo, bônus percentual de quem causa e percentual de quem recebe). O gancho
é **puro** (sem sorteio, sem mudar o jogo), então a IA e o bot do simulador usam
a mesma previsão sem crítico: `CombatResolver.PredictDamage` (também usada pela
previsão das colisões e do terreno).

**Quais tipos de dano.** Cada efeito tem uma máscara (`EDamageKindMask`); o
padrão são os **golpes mirados** (ataque básico e skill). Dano periódico, espinhos
e terreno só mudam se o efeito pedir, e dano exato (`Pure`) nunca muda. As
colisões (`Collision`, atribuídas a quem empurrou) podem ser incluídas, como faz
Estilhaçar.

**Efeitos disponíveis** (`Settings/States`, lista de efeitos de um estado):

| Efeito | O que faz |
|---|---|
| `ConditionalDamageEffect` | bônus (causado) ou redução (recebido) percentual, e fixo no causado, por contagem e por pilha, quando a **condição** vale |
| `OnKillEffect` | ao matar com os tipos escolhidos: cura fixa ou fração da vida máxima, reduz os cooldowns em andamento (aplicado no fim do turno, então inclui a skill usada no golpe) e/ou dá estados |
| `ApplyStateOnHitEffect` | ao acertar com os tipos escolhidos, aplica estados ao alvo vivo (pilhas multiplicadas pelas do estado: o posto) |

**Condições** de `ConditionalDamageEffect` ("oponente" = o alvo no dano causado e
o atacante no dano recebido; as condições sobre o oponente não valem quando não há
atacante, como no dano periódico):

| Condição | Vale quando |
|---|---|
| `Always` | sempre |
| `PerAdjacentEnemy` | uma vez por inimigo do portador adjacente a ele, até o teto (`maxCount`, padrão 3) |
| `NoAdjacentEnemy` | nenhum inimigo do portador está adjacente a ele |
| `OpponentHasNegativeState` | o oponente tem algum estado nocivo (debuff) |
| `OpponentHasState` | o oponente tem algum dos estados listados (por exemplo Envenenado, Veneno letal) |
| `HolderMovedLastTurn` | o portador andou ou se teletransportou no turno anterior ("bate e corre") |
| `HolderDidNotMoveLastTurn` | o portador não andou no turno anterior (parado) |
| `OpponentIsolated` | nenhum aliado do oponente está adjacente a ele |
| `OpponentAtFullHp` | o oponente está com a vida cheia (golpe de abertura) |

- **Adjacência:** distância 1 pela métrica `CombatSettings.adjacencyMetric`
  (padrão Chebyshev: os 8 vizinhos; euclidiana ou Manhattan: os 4 ortogonais),
  contando só personagens vivos (`GridRules.CountAdjacentOpponents` e
  `HasAdjacentAlly`).
- **Moveu-se:** `Character.MovedThisTurn` / `MovedLastTurn`. `GridController.MoveEntity`
  (andar, teletransporte) marca o movimento; ser empurrado ou puxado não conta.
  A bandeira vira "turno anterior" quando o turno do personagem começa
  (`Character.BeginTurn`). Como um turno é uma ação, o turno em que o portador anda
  nunca é o turno em que ataca: o bônus vale no turno **seguinte**. Para o dano
  **recebido**, "turno anterior" é o turno anterior ao atual (os inimigos agem
  depois do jogador), então evite essa condição em redução de dano.
  A bandeira entra no salvamento (`EntitySnapshot.MovedLastTurn`, capturada no início
  do turno do jogador, antes de `BeginTurn`), então carregar repete o mesmo dano.

Assets de exemplo: os 13 talentos de passiva e de dano condicional de
[Valores atuais](#talentos) (Frenesi Corpo a Corpo, Segurar a Linha, Sede de
Sangue, Estilhaçar, Quebradiço, Foco Arcano, Colheita de Almas, Arma Envenenada,
Depredador, Bate e Corre, Assassino, Banquete das Sombras e Veneno Debilitante).

Exemplos com os valores atuais: um ataque básico de 15 contra um Goblin
Blindado (defesa 4) causa `15 − 4 = 11`; um golpe de 5 contra defesa 10 causa 1
(dano mínimo).

Configuração: `CombatSettings` (ver [valores](#configurações)). Uma `CombatSettings`
ausente cai nos padrões acima.

## Skills

Skills são **dados** (`SkillDefinition`, `Create > GridBattle > Skills > Skill
Definition`), nunca código dentro de controllers. Jogador e inimigos usam o mesmo
caminho: `Character.TryUseSkill(grid, skill, célula)`.

Uma skill tem:

| Campo | Significado |
|---|---|
| Tipo | ofensiva, defensiva ou utilitária (`ESkillType`) |
| Dano | dano fixo a cada afetado, antes da escala do atacante, do bônus de skills, do crítico e da defesa |
| Forma da área | `Circle`, `Cross`, `Linear`, `Cone`, `Arc` ou `Perpendicular` |
| Tamanho da área | ímpar, de 1 a 9 |
| Alcance | distância (Manhattan) do lançador à célula alvo, mais o atributo Alcance de skills; 0 = centrada no próprio lançador; ou ilimitado (qualquer célula) |
| Afeta | inimigos, aliados, aliados e o próprio, só o próprio ou todos (`ESkillTargetFilter`) |
| Exige alvo | só pode ser usada se atingir ao menos um personagem (desligue para skills que funcionam em célula vazia) |
| Cooldown | ações entre usos (0 = sem cooldown) |
| Efeitos | resultados que não são dano, aplicados depois do dano: aplicar estados, curar, teleportar ou empurrar/puxar (`SkillEffect`) |

As skills de deslocamento (Golpe de Escudo, Rajada de Vento, Gancho, o Gancho do
Goblin Arpoador e a Onda de Choque do chefe) usam o efeito `Displace`, descrito
em [Empurrar e puxar](#empurrar-e-puxar-deslocamento); as demais são ofensivas só
com dano.

### Valores efetivos e modificadores (`EffectiveSkill`)

Balanceamento G3 (2026-10-06). Os números de uma skill para um lançador (dano,
tamanho da área, alcance, cooldown, distância de empurrões e puxões, duração dos
estados e efeitos) **vêm sempre do `EffectiveSkill`**
(`EffectiveSkill.Resolve(lançador, skill)`): a skill do asset mais o que os
estados do lançador mudam (`SkillModifierEffect`). Sem modificadores, todo número
é exatamente o do asset (conferido em `TalentChecks` para as 14 skills). O asset
nunca guarda estado de runtime.

Um `SkillModifierEffect` vale para as skills listadas (lista vazia = todas as
skills do portador) e cada valor é **por pilha** (um talento com postos concede
uma pilha por posto; vários modificadores somam):

| Campo | Efeito |
|---|---|
| `damageFlat`, `damagePercent` | `(dano + fixo) × (1 + percentual)`, antes da escala do lançador; só skills que já causam dano (uma skill utilitária nunca vira ataque) |
| `areaSteps` | cada passo soma 2 ao tamanho ímpar (um anel a mais; uma linha fica 2 células mais longa), no máximo 9 |
| `range` | soma ao alcance (skills centradas no lançador e de alcance ilimitado não mudam) |
| `cooldownReduction` | tira do cooldown; o cooldown mínimo de `SkillSettings` continua valendo junto com a Redução de cooldown do atributo |
| `displacement` | soma à distância de todo empurrão ou puxão da skill (inclusive os anexados) |
| `stateDuration` | soma aos turnos de todo estado temporário que a skill aplica (os permanentes não mudam) |
| `attachedEffects` | `SkillEffect`s acrescentados à lista de efeitos da skill (aplicar estado, empurrar, curar...); `repeatAttachedPerStack` repete a lista uma vez por pilha (posto 2 de um estado que acumula = 2 pilhas) |

**Quem lê o `EffectiveSkill`:** `SkillTargeting` (alcance máximo, células de mira,
área, afetados, `CanUse`, `IsValidTarget`, células mirável: tudo o que o jogador
vê no destaque de alcance da barra de skills), `SkillDefinition.Execute` e
`GetDamageFor`, `SkillCooldowns.Trigger`, `DisplaceSkillEffect` e
`ApplyStatesSkillEffect` (via `SkillContext.Effective`), `UseSkills` e
`DisplacementScoring` (IA dos inimigos) e `BattleBot` (valoriza dano, área,
empurrão e o dano ao longo do tempo dos estados que a skill aplica). O destaque
mostra o **alcance** modificado; a janela de detalhes só mostra nome e cooldown das
skills (sem números), e a barra não tem pré-visualização da área.

### Áreas

`SkillArea` é pura e estática. A **direção** `d` é a direção cardinal do
lançador à célula alvo, pelo eixo dominante (empate vai na horizontal; alvo na
própria célula usa "para cima"). Seja `r = (tamanho − 1) / 2`. As células fora do
grid e as de terreno que bloqueia áreas de skill são descartadas.

| Forma | Células atingidas | Exemplos |
|---|---|---|
| `Circle` | células a distância euclidiana ≤ `r + 0,5` do alvo | tamanho 1 = só o alvo; 3 = quadrado 3×3; 5 = 5×5 sem os cantos |
| `Cross` | o alvo e um braço de comprimento `r` em cada eixo | tamanho 1 = só o alvo; 3 = um "+" de 5 células |
| `Linear` | `tamanho` células a partir do alvo, afastando-se do lançador | tamanho 3 = o alvo e as 2 seguintes |
| `Cone` | linhas `k = 0..r` a partir do alvo, cada uma com `2k + 1` células de largura | tamanho 3 = 4 células; 5 = 9 células |
| `Arc` | uma fileira de `tamanho` células centrada no alvo, perpendicular a `d`, com cada célula puxada de volta ao lançador | um "V" que envolve o lançador |
| `Perpendicular` | `tamanho` células centradas no alvo, perpendiculares a `d` | tamanho 3 = o alvo e as duas células ao lado |

O resultado não tem duplicatas e sai ordenado por linha e depois coluna, uma
ordem canônica que mantém determinísticos os acertos (e os sorteios de crítico).

### Uso

Uma skill pode ser usada (`SkillTargeting.CanUse`) quando o lançador está vivo,
nenhum estado impede skills, a skill está pronta (sem cooldown) **e** a célula
alvo é válida: dentro do grid, no alcance, com ao menos um afetado (se a skill
exige) e aceita pelos efeitos (um teleporte precisa de uma célula livre). Um uso
inválido não faz nada e **não consome o turno**.

`SkillDefinition.Execute`: animação de ataque (skills ofensivas, quando o alvo
não é a própria célula; antes do dano, para a reação do alvo cair no impacto),
dano a cada afetado (tipo `Skill`, na ordem das células), efeitos, e
`SkillUsedEvent` (a área pisca). Depois, `Character.TryUseSkill` inicia o
cooldown.

O dano da skill usa `(dano + modificadores de talentos) × multiplicador de dano
do lançador` (escala por profundidade dos inimigos), arredondado
(`EffectiveSkill.Damage`).

### Cooldown

- Conta em **turnos do dono** da skill: para o jogador, uma ação consumida; para
  inimigos, os turnos dele. "Cooldown N" significa **N ações entre usos**.
- Ao usar: `cooldown (com os modificadores de talentos) − Redução de cooldown`,
  nunca abaixo do mínimo (`SkillSettings.minimumCooldown`, 1) para skills que têm
  cooldown. Skills com cooldown 0 nunca esperam. Um efeito de abate pode reduzir
  os cooldowns em andamento (`SkillCooldowns.QueueReduction`): a redução entra no
  fim do turno, antes de os cooldowns caírem 1.
- No fim de cada turno do dono (`Character.EndTurn`), todo cooldown em andamento
  diminui em 1, **exceto o das skills usadas naquele turno**. Por isso uma skill
  de cooldown 1 volta a ficar disponível depois de exatamente uma ação no meio.
- Os cooldowns são **zerados a cada batalha** (`RunSettings`) e entram na captura
  e na restauração da batalha.
- Os assets são compartilhados e não guardam estado: os cooldowns vivem em
  `SkillCooldowns`, por personagem.

### Slots

O personagem carrega no máximo **6 skills** (`SkillSettings.maxSkillSlots`), uma
por botão da barra de skills. A skill inicial da classe **ocupa um dos 6
espaços** (`startingSkillsUseSlots`). Os talentos que liberam skills deixam de
ser oferecidos quando os 6 espaços acabam ou depois do sexto talento desse tipo
(`TalentOfferSettings.maxSkillTalents`). A lista de skills de uma run vive em
`RunState`; `Character.SetSkills` a aplica ao personagem da batalha.

### Entrada do jogador

Barra de skills (6 botões) → `PlayerCharacterController.SelectSkill`. Tocar o
botão seleciona a skill (as células onde ela pode ser mirada ganham highlight
`SkillRange`), tocar de novo cancela, e tocar outra troca. Um toque numa célula
válida usa a skill, consome a ação e limpa a seleção; numa célula inválida o
toque é rejeitado (pulso na célula) e a seleção continua. A seleção cai sozinha
ao mudar a vez, e quando a skill deixa de poder ser usada (cooldown, silêncio).

### Uso pelos inimigos

A ação de IA `UseSkillsAction` percorre as skills do config, na ordem. Para cada
skill pronta, `SkillTargeting.TryFindAimCell` escolhe a célula da mira: a do
próprio jogador se for válida; senão a célula válida mais próxima do inimigo
(Manhattan; empate por linha e coluna) cuja área ainda atinge o jogador. A
primeira skill que funciona consome o turno. Skills com o efeito `Displace` têm
tratamento próprio: a célula da mira é a de melhor pontuação (veja
[Empurrar e puxar](#empurrar-e-puxar-deslocamento)) e a skill só é usada se essa
pontuação chegar ao mínimo. Só o Goblin Arpoador e o chefe têm
`SkillDefinition`; as outras habilidades dos inimigos são ações de IA (cura,
escudo, enfraquecer, invocar) descritas em [IA dos inimigos](#ia-dos-inimigos).

> [!important]
> Criar uma skill nova = criar um asset de `SkillDefinition` (ou uma subclasse,
> se a regra for especial) + referenciar no config do personagem ou num talento.
> **Zero mudanças em controllers.**

## Empurrar e puxar (deslocamento)

Decisão de balanceamento de 2026-10-06 (G4): skills e inimigos podem **empurrar
e puxar** personagens. O efeito `Displace` (`DisplaceSkillEffect`) entra na
lista de efeitos de uma skill, depois do dano, e tem dois campos: o **modo** e a
**distância** em células. As regras são puras e vivem em `Displacement`
(`Gameplay/Rules`); `DisplacementResolver` as aplica (e prevê, para a IA e o
simulador).

| Modo | Para onde vai o personagem |
|---|---|
| `AwayFromCaster` | para longe do lançador |
| `TowardCaster` | em direção ao lançador (puxar); para ao ficar ao lado dele e nunca cai em cima |
| `AwayFromAreaCenter` | para longe do centro da área (a célula mirada); quem está no centro vai para longe do lançador |

**Direção.** É a de 8 vizinhos mais próxima do vetor da célula de referência até
o alvo (ângulo arredondado a 45°). Para células vizinhas é simplesmente o sinal
de cada componente; de longe, um alvo quase na mesma linha anda reto, não em
diagonal.

**Movimento.** Célula a célula em linha reta. Para antes de entrar em: fora do
grid (borda), terreno que bloqueia movimento, ou célula ocupada por outro
personagem. Uma diagonal entre dois obstáculos também para (a mesma regra de
andar: basta um dos lados livre). Passar por células não tem efeito. Um
personagem pode ficar parado (distância percorrida 0) e ainda assim colidir.
Sem sorteios.

**Colisão.** Parar contra obstáculo, personagem ou (se `edgeCountsAsCollision`)
a borda causa dano `Collision` ao deslocado: `collisionDamage` (6) mais
`collisionDamagePerUnspentCell` (2) por cada célula que não percorreu. O
personagem atingido leva `collisionHitCharacterDamage` (6), fixo. O lançador
nunca leva dano do próprio deslocamento (se algo é empurrado contra ele, só o
deslocado leva). O dano passa pelo `CombatResolver` (escudos, mortes, eventos,
sons), é atribuído ao lançador (então abates dão XP normalmente), ignora
defesa (`collisionIgnoresDefense`), não é crítico e não dispara espinhos nem
roubo de vida.

**Terreno.** A célula onde o personagem para reage **na hora** (entrada
forçada, `TerrainEffects.ApplyForcedEntry`): dano e estados do perigo ou do
bônus, qualquer que seja o gatilho do terreno (o gatilho "ao entrar" já foi
aplicado pelo próprio movimento). Os gatilhos normais de início e fim de turno
continuam valendo depois. `TerrainTriggeredEvent.IsForced` marca esses casos.

**Imóveis.** `CharacterConfig.canBeDisplaced = false` (o Rei Goblin): empurrões
e puxões não o movem e não causam dano a ele por isso, mas ele continua
bloqueando e leva o dano de quem for lançado contra ele.

**Vários alvos.** Numa área, os alvos são movidos do mais longe ao mais perto da
referência (do centro da área, ou do lançador), e nos puxões do mais perto ao
mais longe, com desempate por linha e coluna, para atrapalharem-se o mínimo.
`Displacement.Plan` prevê o conjunto na mesma ordem.

**Visual.** Lógica imediata: a posição muda na hora. O personagem desliza até a
célula (`GridMovementSettings.slideSecondsPerCell`, sem pulo), e a reação de
acerto (flash e recuo) das colisões espera o fim do deslizamento
(`collisionBumpDelay`). Tudo isso é pulado no simulador (`SimMode`). O fluxo
de turnos espera pelas animações como sempre.

**IA.** `UseSkillsAction` pontua cada uso possível de uma skill de deslocamento
(`DisplacementScoring`, pesos em `DisplacementAiWeights` no asset `UseSkills`):
dano de colisão e de terreno no jogador, dano nos aliados do inimigo (custo),
abate (+50), aliados que alcançam o jogador com o ataque básico depois menos
antes (+5 cada), células de aproximação do lançador (+1 cada) e estados do
terreno de destino. A skill só é usada se a melhor mira chegar a **1 ponto**.
Na prática o Arpoador sempre puxa (aproxima do grupo) e o Rei Goblin só usa a
Onda de Choque quando o jogador está contra a borda ou um obstáculo.

| Valor (`CombatSettings`) | Padrão |
|---|---|
| Dano de colisão | 6 |
| Extra por célula não percorrida | 2 |
| Dano ao personagem atingido | 6 |
| Colisão ignora defesa | sim |
| A borda do grid conta como colisão | sim |

## Consumíveis em batalha

Um consumível (`ConsumableDefinition`, `Create > GridBattle > Consumables >
Consumable Definition`) é um item de uso único, obtido no nó de consumível do
mapa, que só pode ser usado **em batalha** e **não consome a ação do turno**.
Tudo é dado:

| Campo | Significado |
|---|---|
| Mira | `Self` (usado na hora, sem alvo) ou `Cell` (precisa de uma célula alvo) |
| Alcance, alcance ilimitado | mira `Cell`: distância (Manhattan, como as skills) da célula alvo; 0 = centrado em quem usa |
| Forma e tamanho da área, afeta | a área em torno da célula alvo e quem ela atinge (as mesmas formas das skills) |
| Efeitos | curar quem usa, causar dano aos afetados, aplicar estados (a quem usa ou aos afetados) |
| Peso de sorteio | chance relativa de sair no nó de consumível |
| Aparece no glossário | desligado |

- **Inventário** (`ConsumableInventory`): 3 espaços (`ConsumableSettings.slots`,
  no máximo 6), guardados em `RunState.Player.ConsumableIds`. Com o inventário
  cheio, a política padrão é perguntar ao jogador qual item descartar ou recusar
  o novo (`AskPlayer`; as outras são descartar o novo ou o mais antigo).
- **Regras de uso** (`ConsumableRules`): o item existe, o jogador está vivo, é a
  vez dele, a batalha não foi decidida, o limite de usos do turno (1) não foi
  atingido, a célula alvo é válida e **ao menos um efeito mudaria algo** (a cura
  recusa vida cheia; o dano exige um inimigo na área). Um item nunca é gasto à
  toa.
- **Uso** (`ConsumableExecutor`): tira o item do inventário, conta o uso no turno
  (o contador zera a cada turno global), aplica os efeitos e levanta
  `ConsumableUsedEvent`. **Nunca levanta `PlayerActionEvent`**, então o jogador
  ainda pode andar, atacar ou usar uma skill no mesmo turno.
- **Dano de consumível** (`EDamageKind.Consumable`) passa pelo crítico e pela
  defesa do alvo, mas não escala com a profundidade.
- **Entrada:** barra de itens (um botão por espaço). Item sem alvo é usado ao
  tocar o botão; item com alvo entra em modo de mira (highlight `SkillRange` nas
  células válidas) e um toque numa célula válida o usa. Tocar o mesmo botão
  cancela. Mirar um item cancela a skill selecionada, e vice-versa.
- **Obtenção:** o nó de consumível sorteia do pool, pelo peso, usando o fluxo
  `Consumables` da seed (1 item por padrão; com `choiceCount` maior o jogador
  escolhe entre opções).

Itens atuais: ver [Valores atuais](#consumíveis).

## Ações do jogador e entrada

O `PlayerCharacterController` escuta `CellTapEvent` (clique ou toque curto numa
célula) e **roteia** a ação:

```
tap em célula
├── entrada bloqueada (TurnBlockers)?  → ignora
├── não é a vez do jogador?            → pulso de rejeição (na vez dos inimigos, também acelera as animações)
├── consumível selecionado?            → TapWithConsumable (não consome a ação)
├── skill selecionada?                 → TapWithSkill (consome a ação)
├── célula sem conteúdo                → ANDAR  (GridRules.CanWalkTo)
│     └── válido → GridController.MoveEntity
└── célula com conteúdo                → ATACAR (GridRules.IsAttackTarget)
      └── válido → animação + Character.Attack
→ ação consumida: PlayerActionEvent → termina a vez do jogador
```

- Ação inválida (fora de alcance, célula ocupada ou bloqueada ao andar, alvo
  inválido) mostra o pulso de rejeição na célula e **não consome o turno**.
- O ataque básico passa pelo `CombatResolver` (tipo `BasicAttack`); o ataque do
  inimigo também.
- `PlayerCharacterController.TryUseSkill` e `TryUseConsumable` usam skills e
  itens direto numa célula, sem seleção (atalhos de UI e testes).

### Entrada bloqueada

`GameplayInput.IsBlocked` vale enquanto algo segura o fluxo do turno
(`TurnBlockers`): o toque nas células, os botões de skill e de item e o toque
longo são ignorados. Quem segura:

| Bloqueio | Quando |
|---|---|
| "XP delivery" | orbs de XP em voo que vão subir de nível, ou que são do último inimigo da batalha |
| "Talent choice" | a escolha de talento está aberta |
| "Entity details" | a janela de detalhes está aberta |
| "Pause menu" | o menu de pausa está aberto |
| "Tutorial tip" | uma dica de tutorial está aberta |

Depois que a batalha é decidida (`ETurnOwner.None`) ninguém age. Fora da vez do
jogador, ou com a entrada bloqueada, as barras de skill e de item aparecem
inativas e os toques nelas não fazem nada.

### Detalhes de uma entidade

**Toque longo** em dispositivos de toque (o toque precisa ficar na célula por
`GameplayInputSettings.longPressSeconds`, 0,45 s) ou **clique direito** do mouse
abre a janela de detalhes da entidade na célula e/ou do terreno dela
(`CellLongPressEvent`). O toque curto e o clique esquerdo continuam andando ou
atacando; o fim de um toque longo é descartado (não anda nem ataca) e deslizar
para fora da célula cancela o toque.

A janela mostra: título e retrato; vida; para inimigos, o **papel** e o texto do
comportamento; os **atributos** (os básicos sempre: andar, alcance de ataque,
dano básico e defesa; os demais só quando diferentes de zero, com o multiplicador
de crítico acompanhando a chance); as **skills** com o cooldown restante ou
"pronta"; os **estados ativos** com o nome, as pilhas e os turnos restantes ou
"permanente"; e o **terreno** da célula (uma célula vazia com terreno mostra só
o terreno). Enquanto está aberta, o jogo não aceita ações (`TurnBlockers`). Ela
só fecha pelo **X** (a ação de voltar não fecha); abrir ou fechar não consome o
turno nem faz nada no jogo, e um novo grid a fecha.

## Sistema de turnos

`TurnManager` (`GridBattle.Managers`, no GameObject do Grid, com o
`BattleController`) conduz o **turno global**. Cada turno global:

```
GlobalTurnStartedEvent(n)        ← zera o contador de consumíveis do turno
  jogador: BeginTurn            ← efeitos de início de turno dos estados (veneno, regeneração)
           EntityTurnStartedEvent
  espera as animações e os TurnBlockers
  TurnChangedEvent(Player)       ← o jogador pode agir (salva a batalha)
  ... uma ação consumida (PlayerActionEvent) ...
  TurnChangedEvent(Enemies)
  jogador: EntityTurnEndedEvent  ← gatilhos de fim de turno do terreno
           Character.EndTurn     ← cooldowns e durações diminuem
  espera o movimento do jogador e os TurnBlockers (orbs que sobem de nível, escolha de talento)
  para cada inimigo, na ordem:
      BeginTurn → Act (IA) → EndTurn   (cada passo com os mesmos ganchos)
      ritmo (pacing) e espera dos TurnBlockers
  espera as animações e os TurnBlockers → próximo turno global
```

- **Cada ação consumida do jogador = 1 rodada completa dos inimigos.** Uma ação
  inválida não levanta o evento, então não há turno inimigo.
- **A vez é explícita** (`ETurnOwner`: `Player`, `Enemies`, `None`;
  `TurnChangedEvent`): o jogador não age nem vê highlights fora da vez dele.
- **Início e fim do turno de cada entidade** (`EntityTurnStartedEvent`,
  `EntityTurnEndedEvent`, chamados também para inimigos): os estados rodam os
  efeitos de início e de fim, as durações diminuem, o terreno dispara os gatilhos
  de início e de fim de turno, as recargas das skills e a memória da IA do
  inimigo diminuem. Uma entidade que morre no meio pula o resto do turno.
- **Ordem dos inimigos determinística:** distância ao jogador (euclidiana, a
  menor primeiro), desempate por `y` e depois `x`. A lista é tirada no início da
  rodada, então um inimigo invocado não age no turno em que aparece.
- **Ritmo** (`EEnemyTurnPacing`): `Staggered` (padrão: cada inimigo age
  `enemyStagger` = 0,08 s depois do anterior, sem esperar a animação),
  `Sequential` (cada um espera a animação do anterior) e `Simultaneous` (todos
  agem de uma vez). Em todos, espera-se o fim das animações ao final.
- **Acelerar:** um toque durante a vez dos inimigos aplica `speedUpMultiplier`
  (2x) às animações e ao intervalo até a vez voltar ao jogador (sem
  `Time.timeScale`).
- **Movimento:** a ocupação das células e o `CurrentGridPos` mudam na hora; só o
  visual anima (`GridMovementSettings`: estilo `Hop` ou `Flip`, pulso na célula de
  chegada).
- **Ataque:** avanço do atacante em direção ao alvo (~150 ms); o dano é
  imediato, mas a reação do alvo (flash e recuo) cai no ponto de impacto.
- **Nova batalha** (`GridInitializedEvent`) limpa os bloqueios, reinicia o
  `BattleController` e volta para a vez do jogador, abandonando qualquer vez
  inimiga pendente. Numa batalha restaurada o turno global continua do número
  salvo.
- **Salvamento:** a batalha é capturada no início de cada turno global, antes dos
  efeitos de início de turno, e gravada quando a vez passa ao jogador. A retomada
  repete o início do turno (ver [[GDD]] 8.1).

### TurnBlockers

`TurnBlockers` (estático) reúne o que precisa terminar antes de o fluxo
continuar. Quem precisa segurar o jogo chama `Acquire(motivo)` e descarta o
`IDisposable` retornado para liberar. O `TurnManager` e o `BattleController`
esperam `WaitAsync` em cada passo. Os bloqueios que existem estão na tabela da
seção [Entrada bloqueada](#entrada-bloqueada). Uma nova batalha limpa todos.

### Fim da batalha

O `BattleController` decide o resultado quando um personagem morre:

- **o jogador morre:** `BattleDecidedEvent(false)` e `BattleEndedEvent(false)` na
  hora;
- **o último inimigo morre:** `BattleDecidedEvent(true)` na hora (a vez passa a
  `None`: ninguém age) e `BattleEndedEvent(true)` só depois de terminar as
  animações e os `TurnBlockers` (os orbs de XP do último abate, a subida de nível
  e a escolha de talento que ele causar). O último abate é resolvido por inteiro
  antes de a run seguir.

## IA dos inimigos

A IA é **dado, não código de controller**. O `EnemyController` (componente do
`Enemy.prefab`) só resolve o alvo e delega o turno ao `EnemyBehavior` do config:

```
EnemyController.Act()
  → guard: inimigo morto não age; sem jogador, não age
  → config.Behavior.TakeTurn(EnemyTurnContext { Self, Grid, Target, Memory })
      → tenta cada EnemyAction em ordem; a primeira que retornar true
        consome o turno
```

- `EnemyBehavior` (SO, `Create > GridBattle > AI > Enemy Behavior`) — lista
  ordenada de `EnemyAction`. Pode ser compartilhado por vários inimigos.
  Sem nenhuma ação que consuma o turno, o inimigo pula a vez. `EnemyConfig` sem
  `behavior` = inimigo que não age.
- `EnemyAction` (SO abstrato, `TryExecute(in EnemyTurnContext)`). Ações são
  **assets sem estado de runtime**: o estado de cada inimigo (cooldowns e
  contadores) vive na `EnemyMemory`, guardada pelo id da ação.
- Os inimigos são **aliados entre si** (`GetAllies`, em ordem de linha e coluna).
- As habilidades com cooldown (`EnemyAbilityAction`) ficam bloqueadas quando um
  estado impede skills.

Ações disponíveis (`Create > GridBattle > AI > Actions`):

| Ação | Assets | Comportamento |
|---|---|---|
| `UseSkillsAction` | `UseSkills` | tenta as skills do config contra o jogador (ver [Skills](#uso-pelos-inimigos)) |
| `BasicAttackAction` | `BasicAttack` | se `GridRules.IsAttackTarget`, ataca o jogador |
| `ChaseTargetAction` | `ChaseTarget` | anda o passo de `GridRules.FindStepToward`; sem passo que aproxime, não consome o turno |
| `KeepDistanceAction` | `KeepDistance` (distância preferida 2), `KeepDistanceBackline` (3, sem preferir o alcance de ataque) | entre as células para onde pode andar e a própria, escolhe a de distância (métrica de ataque) mais próxima da preferida; com a opção ligada, as células de onde o jogador está ao alcance de ataque vencem primeiro; empate prefere ficar parado, depois a menor linha e coluna. Só consome o turno se o inimigo se mover |
| `ApplyStateToTargetAction` | `ApplyWeakened` | com o jogador no alcance (3), a habilidade pronta e o jogador sem o estado, anima o ataque e aplica Fraquejado (2 turnos) |
| `HealAllyAction` | `HealAlly` | cura 6 de vida o aliado mais ferido (vida abaixo de 75%) no alcance 3; só consome o turno se curou algo |
| `BuffAllyAction` | `BuffAllyShield` | dá Escudo (3 turnos) ao aliado que ainda não o tem, no alcance 3, escolhendo o mais próximo do jogador |
| `SummonAction` | `SummonRat` | invoca um Rato na célula livre adjacente mais próxima do jogador, com a escala do invocador, se houver menos de 2 vivos dele; o invocado não age no turno em que aparece e **não dá XP** |

Comportamentos atuais (`Settings/AI`):

| Comportamento | Ações, em ordem | Usado por |
|---|---|---|
| `ChaseAndAttack` | `UseSkills` → `BasicAttack` → `ChaseTarget` | Goblin, Rat, Slime, EyeBat, os corpo a corpo provisórios, o Goblin Arpoador (skill Gancho) e o chefe (skill Onda de Choque) |
| `Ranged` | `UseSkills` → `KeepDistance` → `BasicAttack` → `ChaseTarget` | FireSkull |
| `Support` | `HealAlly` → `BuffAllyShield` → `UseSkills` → `BasicAttack` → `KeepDistanceBackline` | Xamã Goblin |
| `Summoner` | `SummonRat` → `UseSkills` → `BasicAttack` → `KeepDistanceBackline` | Rainha dos Ratos |
| `Controller` | `ApplyWeakened` → `UseSkills` → `BasicAttack` → `ChaseTarget` | Olho Maldito |

### Cooldown das habilidades

A `EnemyMemory` conta os cooldowns em **turnos do próprio inimigo**, com a mesma
regra das skills: usada no turno N, a habilidade volta no turno N + cooldown + 1
(cooldown 0 = todo turno). O controller desconta ao fim de cada turno do
inimigo, pulando o que começou naquele turno. Cooldown e contadores entram na
captura e na restauração da batalha.

### Papéis

Cada inimigo tem um **papel** (`EnemyRoleDefinition`: ícone, nome, descrição,
fator de ameaça e se conta como linha de frente): corpo a corpo, enxame,
atirador, suporte, invocador e controlador (Mecânica 4). O papel aparece sobre o
inimigo, na janela de detalhes, no glossário e na prévia do nó, e entra na
composição das batalhas; **quem decide como o inimigo age é o `behavior`**, não
o papel.

| Papel | Linha de frente | Fator de ameaça |
|---|---|---|
| Corpo a corpo (`Melee`) | sim | 1 |
| Enxame (`Swarm`) | sim | 1 |
| Atirador (`Ranged`) | não | 1 |
| Suporte (`Support`) | não | 1 |
| Invocador (`Summoner`) | não | 1 |
| Controlador (`Controller`) | não | 1 |

> [!tip]
> A IA de passo é **gananciosa por um turno** (one-step greedy): não planeja
> rotas por pathfinding; só contorna obstáculos pela menor rota. Comportamentos
> novos (fugir, curar, patrulhar) entram como novas subclasses de `EnemyAction`
> combinadas em outros `EnemyBehavior`, sem tocar no `EnemyController`.

## Terreno

Cada célula pode ter um `TerrainDefinition` (`Create > GridBattle > Terrain >
Terrain Definition`), que é só dado:

| Campo | Significado |
|---|---|
| Tipo | `Obstacle` (célula bloqueada), `Hazard` (perigo) ou `Bonus` |
| Bloqueia movimento | ninguém anda para dentro, ocupa, nasce nem é teleportado para a célula; sempre ligado nos obstáculos |
| Bloqueia área de skill | as áreas de skill pulam a célula, então ninguém sobre ela é atingido (desligado) |
| Gatilho | quando o efeito se aplica: ao entrar, no início ou no fim do turno de quem está sobre ela (`ETerrainTrigger`) |
| Dano | dano do tipo `Terrain` à entidade sobre a célula |
| Estados | estados aplicados à entidade, sem fonte |
| Afeta jogador, afeta inimigos | os dois ligados: o terreno afeta jogador e inimigos igualmente |
| Visual | sprite sobreposto, cor do sprite e tinta do piso (o tipo é legível antes de entrar) |

- **Obstáculos:** quem anda contorna (`HasWalkPath`, `FindStepToward`), e o
  gerador garante que nenhum conjunto de obstáculos isole células do grid.
- **Efeitos** (`TerrainEffects`, ligados ao `GridController`): a cada
  `EntityTurnStartedEvent`, `EntityTurnEndedEvent` e `EntityEnteredCellEvent`, se a
  célula sob a entidade tem o gatilho correspondente e afeta o lado dela, aplica
  o dano (primeiro) e depois os estados. Levanta `TerrainTriggeredEvent`, pulsa a
  célula e toca o som de perigo ou de bônus. Jogador e inimigos usam o mesmo
  caminho. Hoje todo terreno usa o gatilho **fim do turno**.
- **Entrada forçada:** quem é empurrado ou puxado e para sobre perigo ou bônus
  recebe o efeito na hora, além do gatilho normal (veja
  [Empurrar e puxar](#empurrar-e-puxar-deslocamento)).
- **Geração** (`TerrainGenerator` e `TerrainGenerationSettings`): determinística
  pela seed (um fluxo derivado da seed e do nó) e pela profundidade. Faixas:
  profundidade 1 sem terreno; de 2 a 4, 0 a 2 obstáculos, 0 a 1 perigo e 0 a 1
  bônus; de 5 em diante, 1 a 3, 1 a 2 e 0 a 1. Cada tipo sorteia do seu pool (pesos
  iguais). **Segurança:** as células a distância (Chebyshev) 1 do ponto de partida
  do jogador nunca recebem terreno, e os obstáculos não podem dividir o grid
  (preenchimento por vizinhança ortogonal); depois de 20 tentativas sem um
  layout inteiro, a batalha fica sem obstáculos. Os inimigos não começam sobre
  células bloqueadas nem de perigo.
- **Interface:** a prévia do nó mostra o terreno (mini-grid colorido por tipo e
  legenda), e a janela de detalhes mostra o terreno da célula, mesmo vazia.

Terrenos atuais (`Settings/Terrain`):

| Terreno | Tipo | Efeito |
|---|---|---|
| Rocha | obstáculo | bloqueia a célula |
| Fogo | perigo | 4 de dano ao fim do turno de quem está sobre ela |
| Pântano Venenoso | perigo | aplica Envenenado por 3 turnos |
| Santuário de Cura | bônus | aplica Regeneração por 3 turnos |
| Pedra Protetora | bônus | aplica Escudo por 2 turnos |

## Morte e fim da batalha

A lógica é genérica em `Character` (`Die()`, `protected virtual`):

1. HP chega a 0 em `ApplyHit` → `IsDead` → `Die()`.
2. `Die()`:
   - desocupa a célula;
   - levanta `CharacterDiedEvent(this)`;
   - inimigo: efeito curto de flash e fade (~150 ms) e então `Destroy`; jogador:
     `Destroy` imediato.
3. Reações assinam `CharacterDiedEvent`:
   - **`BattleController`:** decide a vitória ou a derrota (ver [Fim da
     batalha](#fim-da-batalha));
   - **`XpRewardSystem`:** se morreu um inimigo, concede o XP dele (ver [XP e
     nível](#xp-nível-e-talentos)); qualquer morte de inimigo conta, inclusive por
     veneno, espinhos ou terreno;
   - **jogador morto** → `RunManager` encerra a run (derrota), apaga o
     salvamento da run e a tela de fim de run mostra o resumo da build, o nível e
     a profundidade alcançados, de onde se volta ao menu;
   - **último inimigo morto** → `RunManager` guarda o estado do jogador (vida ao
     menos 1, nível, XP, estados permanentes), conta a batalha vencida e abre o
     mapa (ou termina a run em vitória, se foi o chefe).
- `ReceiveDamage` é **idempotente pós-morte**: personagem morto não toma dano
  adicional, não usa skill (`TryUseSkill` guarda), não age no turno e não é
  curado.
- **Desistir** (menu de pausa) encerra a run pelo `RunManager` e, por padrão,
  conta como derrota no registro (`MetaSettings.giveUpCountsAsDefeat`).

## XP, nível e talentos

O `PlayerCharacter` acumula XP ao longo da run; nível e XP persistem entre as
batalhas (`RunState.Player`).

### Fluxo

A regra (quanto XP, para quem, em quantos pacotes) e a apresentação (orbs
voando até a barra) são componentes separados, cada um no seu GameObject.
Nenhum deles vive no Grid.

```
inimigo morre (Character.Die)
  → desocupa a célula
  → CharacterDiedEvent
      → XpRewardSystem (GameObject próprio)          ← regra
          → lê Enemy.XpReward (config, escalado pela profundidade; 0 em invocados)
          → resolve o PlayerCharacter atual
          → XpPacket.Split: N = teto(XP / xpPerPacket) pacotes,
            cada um com o progresso da barra após creditá-lo
          → se o XP vai subir de nível, ou é o do último inimigo vivo:
            TurnBlockers.Acquire("XP delivery")     ← segura o fluxo do turno
          → EventBus.Raise(XpRewardDroppedEvent { Origin, Packets, Collect })
              → XpOrbsVfx (no GameObject GameScreenLayer)  ← apresentação
                  → MarkPresented(): assume a entrega
                  → cada pacote vira um orb (UI Toolkit) que voa em arco
                    (LitMotion) da posição do inimigo até o ponto da barra
                    (XpBarView.TryGetTrack), com stagger
                  → ao chegar: remove o orb e chama Collect(pacote)
          → se ninguém assumiu a entrega: Collect de todos na hora
  Collect(pacote) → PlayerCharacter.GainXp(pacote.Amount)
      → PlayerXpChangedEvent { Level, CurrentXp, XpToNextLevel }
          → GameScreenController.OnXpChanged → XpBarView.SetState
      → se cruzou o limiar: PlayerLeveledUpEvent (um por nível ganho)
          → TalentService: abre a escolha de talento (jogo pausado)
  quando o último pacote chega: o bloqueio "XP delivery" é liberado
```

- **O XP só entra na conta quando os orbs chegam à barra**: a barra cresce orb
  a orb. A tela de escolha de talento só abre quando o orb leva a barra ao
  limiar (o evento de nível vem depois do `PlayerXpChangedEvent`, então a barra
  já mostra o nível novo). Há um atraso de ~0,6 s (mais stagger) entre a morte e o
  XP efetivo.
- Divisão dos pacotes (`XpPacket.Split`): `N = ceil(XP / xpPerPacket)` (padrão 5,
  no `XpRewardSystem`); o resto da divisão é distribuído 1 a 1 nos primeiros
  pacotes (12 XP → 3 pacotes de 4; 15 XP → 3 pacotes de 5). O jogador nunca
  perde XP no arredondamento.
- Os orbs são elementos da **UI** (UI Toolkit, posicionados em absoluto na raiz do
  painel da GameScreen), convertidos do espaço do mundo por
  `RuntimePanelUtils.CameraTransformWorldToPanel`.
- Ajustes visuais (sprite, tamanho, duração, stagger, arco, easing) ficam no
  asset `XpOrbsVfxSettings` e podem ser alterados em Play Mode.
- `XpBarView` é o único lugar que conhece os elementos e as medidas da barra
  (`xp-bar-progress`, `xp-bar-detail-2`, `current-level`, 110 px / 112 px).
- Gameplay não depende da UI: sem apresentação disponível, o `XpRewardSystem`
  credita o XP na hora. A comunicação **jogo → UI é sempre via EventBus**. Um
  `PlayerCharacter` recém-criado emite `PlayerXpChangedEvent`, então a barra
  começa cada batalha com o estado da run.
- O inimigo morto já desocupou a célula antes de o XP ser concedido, e jogador
  morto não ganha XP.

### Curva e nível máximo

Configurados **globalmente** em `ProgressionSettings` (`Settings/Progression`),
iguais para todas as classes:

- `baseXpToLevelUp` = **50**: XP para ir do nível 1 ao 2.
- `xpToLevelUpGrowthPerLevel` = **25**: acréscimo a cada nível atingido.
- `maxLevel` = **30**.
- `carryOverLeftoverXp` = ligado: a sobra de XP passa para o limiar seguinte
  (com subidas de nível em cadeia quando um abate valioso fecha mais de uma conta).

Fórmula: `XpToNextLevel(nível) = base + (nível − 1) × acréscimo`.

| Nível atual | XP para o próximo |
|---|---|
| 1 | 50 |
| 2 | 75 |
| 3 | 100 |
| 4 | 125 |
| 29 | 750 |

No nível máximo o XP não gera mais níveis (e portanto mais talentos) e a barra
fica cheia. **Cada nível ganho recupera vida:** `ProgressionSettings.levelUpHealFraction`
× vida máxima (padrão 1, vida cheia), pelo `CombatResolver.HealFraction`, logo antes
do `PlayerLeveledUpEvent` de cada nível. Vale também no meio da batalha.

### XP por inimigo

O XP vem da batalha, não de cada inimigo (`BattleGenerationSettings.xpSource =
ThreatBudget`). O gerador calcula o XP total pelo orçamento de ameaça da profundidade
`p` e da dificuldade:
`XP = orçamento(p, dificuldade) × 8,5 × (1 + 0,07 × (p − 1)) × multiplicador de XP da
dificuldade (hoje 1)`, arredondado, e o divide entre os inimigos na proporção da ameaça
de cada um (as sobras do arredondamento vão para as maiores frações). Por isso uma
batalha fácil sempre dá menos XP que uma normal, e uma normal menos que uma difícil, no
mesmo andar. A profundidade usada é a de balanceamento (ver abaixo). Invocados não dão
XP. As fontes antigas continuam como opção: `EnemyReward` (`xpReward` do config × o
crescimento por profundidade) e `EnemyThreat` (ameaça de cada inimigo × 8,5). O XP
total da batalha aparece na prévia do nó.

**Profundidade de balanceamento.** Os valores por profundidade dos assets de
balanceamento (orçamento, escala de vida e dano, pool de inimigos, XP, faixas de
terreno) são escritos para um mapa de `balanceFloorCount` andares (30). Num mapa de
`floorCount` andares, o andar `d` é lido como `1 + (d − 1) × (30 − 1) / (floorCount − 1)`
(a pool e as faixas de terreno usam o andar inteiro mais próximo), e o XP de cada
batalha é multiplicado por `(30 − 1) / (floorCount − 1)`. Assim o primeiro andar e o
chefe têm a mesma força em qualquer tamanho de mapa, e a run rende mais ou menos os
mesmos níveis.

### Talentos oferecidos ao subir de nível

Cada nível ganho abre uma oferta (`TalentService`, `TalentSession`); vários níveis
de uma vez geram ofertas em sequência, uma por nível. O talento escolhido vale já
para o restante do turno.

- **Pausa:** enquanto a oferta está aberta o jogo fica pausado (`TurnBlockers`,
  "Talent choice"; a música continua) e só a tela de escolha responde. As ofertas
  pendentes e as opções sorteadas entram no salvamento da run: reabrir o app
  mostra a mesma oferta. Uma oferta causada pelo último abate do chefe final é
  pulada (a run está acabando).
- **Pool e filtros:** a pool é a da classe (`talentPool`) mais a compartilhada
  (`TalentOfferSettings.sharedPool`). Entra na oferta o talento que a classe
  pode pegar, com nível mínimo atingido (o nível que acabou de ser alcançado),
  pré-requisitos cumpridos, posto abaixo do máximo, não banido, e, se libera skill,
  com espaço nos 6 slots, abaixo do limite de talentos de skill e sem a skill já
  liberada.
- **Opções:** 3 por oferta (`optionsPerOffer`), até 5 com talentos que somam
  opções.
- **Sorteio:** ponderado por sinergia, sem repetição:
  `peso = peso base × (1 + 0,5 × tags em comum com os talentos já escolhidos)`.
  Determinístico: depende só da seed da run, do nível e do reroll.
- **Ferramentas do jogador** por run: **2 rerolls**, **1 banimento** e **1 pulo**
  (mais os de talentos). Rerrolar sorteia de novo, preferindo talentos que não
  estão na tela. Banir tira o talento da run e sorteia outro para a mesma
  posição. Pular perde o talento daquele nível.
- **Efeito imediato:** o talento sobe 1 posto, seus estados vão para o jogador
  (permanentes, com o id do talento como fonte e uma pilha por posto), a skill que ele
  libera entra na barra, e um bônus de vida máxima recupera a mesma vida
  (`maxHpGainHealsSameAmount`).
- **Nó de talento:** funciona como um nível a mais (a oferta usa o nível do jogador
  mais 1), custa vida (15% da vida máxima, sem nunca matar) e não concede XP.

Os talentos atuais são **provisórios** (ver [Valores atuais](#talentos)).

## Highlights visuais

Os highlights são recalculados **por evento** (`PlayerCharacterController`): tudo
que pode mudá-los (movimento, morte, invocação, estados, seleção de skill ou
item, cooldowns, início de turno global, batalha decidida) marca-os como sujos, e
eles são refeitos no máximo uma vez por quadro, e só enquanto há algo mudado.
Só aparecem na vez do jogador.

| Highlight | Condição | Cor |
|---|---|---|
| **Walk** (azul) | célula livre, em alcance de movimento | `walkHighlightColor` (prefab Cell) |
| **Attack** (vermelho) | célula com `IDamageReceiver` (não o próprio jogador) em alcance de ataque | `attackHighlightColor` (prefab Cell) |
| **SkillRange** (roxo) | com uma skill ou um item com alvo selecionado: as células onde ele pode ser mirado | `skillRangeHighlightColor` (prefab Cell) |

- Com uma skill ou item com alvo selecionado, só o `SkillRange` aparece.
- Highlights que aparecem juntos entram em fade escalonado (visual).
- Quando uma skill ou uma bomba é usada, as células da área piscam
  (`skillAreaFlashColor`).
- Cores e alpha ficam no prefab `Cell.prefab` (alpha > 0 obrigatório).
- Inimigos não pintam highlights (decisão de design: o campo tático é
  apresentado do ponto de vista do jogador).

## Pontos de extensão

| Extensão | Onde plugar |
|---|---|
| **Skill nova (jogador)** | asset `SkillDefinition` (ou subclasse para regra especial) + talento que a libera (`TalentDefinition.unlockedSkill`) ou skill inicial no `CharacterConfig.skills` |
| **Efeito novo de skill** | subclasse de `SkillEffect` (ela aparece no seletor do Inspector) |
| **Skill que empurra ou puxa** | efeito `Displace` (modo + distância) na lista de efeitos da skill; valores da colisão em `CombatSettings`; personagem imóvel: `canBeDisplaced` desligado no config |
| **Novo inimigo** | asset `EnemyConfig` (sprite, animator, tint, atributos, `role`, `behavior`, `xpReward`, `initialStates`, `prefab = Enemy.prefab`) + entrada no pool de `BattleGenerationSettings` — sem prefab novo |
| **Novo papel de inimigo** | asset `EnemyRoleDefinition` (ícone, nome, fator de ameaça, linha de frente) |
| **Novo personagem jogável** | asset `PlayerCharacterConfig` (`prefab = PlayerCharacter.prefab`, `battlesToUnlock`, `talentPool`) + entrada em `GameStateManager.playableCharacters` |
| **Comportamento de inimigo novo** | subclasse de `EnemyAction` (de `EnemyAbilityAction` se tiver cooldown) + asset; combinar ações num `EnemyBehavior` e referenciar no `EnemyConfig` |
| **Estado novo** | asset `StateDefinition` com efeitos; efeito novo = subclasse de `StateEffect` |
| **Talento que modifica uma skill** | estado com `SkillModifierEffect` (skills, dano, área, alcance, cooldown, empurrão, duração, efeitos anexados) concedido por um talento com `maxRank` |
| **Passiva condicional** | estado com `ConditionalDamageEffect` (condição, tipos de dano, percentual), `OnKillEffect` ou `ApplyStateOnHitEffect`; condição nova = valor em `EDamageCondition` e um caso em `ConditionalDamageEffect.Evaluate` |
| **Talento novo** | asset `TalentDefinition` (classes, nível, pré-requisitos, tags de sinergia, estados, skill) + entrada na `talentPool` da classe ou no `sharedPool` do `TalentOfferSettings` |
| **Consumível novo** | asset `ConsumableDefinition` + entrada no pool do `ConsumableSettings`; efeito novo = subclasse de `ConsumableEffect` |
| **Terreno novo** | asset `TerrainDefinition` + entrada no pool do `TerrainGenerationSettings` |
| **Quantidade de terreno por profundidade** | faixas do `TerrainGenerationSettings` |
| **Mapa e batalhas** | `MapGenerationSettings` (andares, andares de referência do balanceamento, nós, tipos, dificuldade), `BattleGenerationSettings` (pool por profundidade, orçamento de ameaça, escala, XP, limites) e `RunSettings` (cura, custo do nó de talento, estados entre batalhas) |
| **Fórmulas de dano** | `CombatSettings` (defesa fixa ou percentual, dano mínimo, arredondamento, teto do crítico, o que ignora defesa, colisão de empurrões e puxões, métricas de distância) |
| **Hook do jogador na run** (talentos que mexem na vida máxima ou em estados) | `IRunPlayerModifier` registrado em `RunPlayerHooks` |
| **Segurar o fluxo do turno** (janela, escolha, animação longa) | `TurnBlockers.Acquire(motivo)` e descartar o handle ao terminar; a entrada do jogo fica bloqueada junto |
| **Facções/alianças** | `GridRules.AreAllies` e a checagem do ataque básico em `GridRules.IsAttackTarget` |
| **Distância em diagonais** | trocar a métrica em `CombatSettings` (Chebyshev) |
| **Reações visuais** (dano, ataque, morte) | `CharacterView` e `GridEffectsAnimator` |
| **Animações compartilhadas** | `AnimatorOverrideController` no `animatorController` do config (sobre um controller base comum) |
| **Pacing e animação dos turnos** | `TurnManager.enemyPacing` e `GridMovementSettings`; animações que devam segurar o turno entram no esquema de espera do `GridController.WaitForMovementsAsync` |
| **Curva de XP e nível máximo** | `ProgressionSettings` |
| **Oferta de talentos** (opções, rerolls, banimentos, pesos) | `TalentOfferSettings` |
| **Efeitos de nível** | assinar `PlayerLeveledUpEvent` |
| **XP por fonte extra** (itens, eventos) | chamar `PlayerCharacter.GainXp`; para entrega animada, emitir `XpRewardDroppedEvent` com os pacotes |
| **Ajustar o VFX de XP** | asset `XpOrbsVfxSettings`; XP por orb em `XpRewardSystem.xpPerPacket` |
| **Dicas de tutorial** | asset `TutorialTipDefinition` + gatilho em `TutorialService.Notify` |
| **Áudio** | clipes no `AudioLibrary` (um cue por `ESfx`, uma faixa por `EMusicContext`) |
| **Status/DoTs** (veneno etc.) | estados com `PeriodicDamageEffect`, que rodam nos turnos do portador |

## Valores atuais dos assets

Espelho de `Assets/Application/Settings/`. Todos os valores de conteúdo são
provisórios e serão balanceados; a lista das escolhas para as perguntas em
aberto está em [[valores_padrao_em_aberto]].

> [!note] Balanceamento inicial
> Para dar vantagem ao jogador no início da run, os **jogadores têm ataque
> básico 15** e os **inimigos comuns 3 a 7**. Vida e mobilidade variam por
> personagem conforme as tabelas abaixo.

### Jogadores

`PlayerCharacterConfig` em `Characters/Players`. Alcance de ataque 1 e de andar 1
para todos; chance de crítico 0, multiplicador de crítico 2, defesa 0, penetração
0 e modificadores de skill 0.

| Personagem | Classe | Vida | Ataque básico | Skill inicial | Liberação (batalhas vencidas) | Talentos de skill da classe |
|---|---|---|---|---|---|---|
| Knight | Guerreiro (Warrior) | 120 | 15 | Golpe | 0 (desde o início) | Muralha |
| Mage | Mago (Mage) | 80 | 15 | Nova de Gelo | 15 | Grimório, Conjurador de Tempestades |
| Rogue | Ladino (Rogue) | 90 | 15 | Adaga Envenenada | 30 | Arsenal Oculto, Mestre das Sombras, Mestre do Veneno |

Diretriz: o **Mago** é a classe mais frágil (menos vida); começa com a Nova de
Gelo, e as outras skills de área chegam por talentos.

### Inimigos

`EnemyConfig` em `Characters/Enemies`. Crítico 0, defesa 0, sem skills
(`SkillDefinition`), exceto o Goblin Arpoador e o chefe. Os inimigos
provisórios e os do chefe reutilizam a arte existente, recolorida pelo tint do
config.

**Inimigos básicos**

| Inimigo | Papel | Comportamento | Vida | Andar | Alcance | Dano | XP | Profundidade mínima |
|---|---|---|---|---|---|---|---|---|
| Goblin | corpo a corpo | `ChaseAndAttack` | 30 | 1 | 1 | 5 | 10 | 1 |
| Rat (Rato) | enxame | `ChaseAndAttack` | 20 | 2 | 1 | 5 | 8 | 1 |
| Slime | corpo a corpo | `ChaseAndAttack` | 40 | 1 | 1 | 5 | 12 | 1 |
| FireSkull (Crânio de fogo) | atirador | `Ranged` | 25 | 1 | 2 | 7 | 15 | 1 |
| EyeBat (Morcego-olho) | enxame | `ChaseAndAttack` | 25 | 2 | 1 | 5 | 8 | 1 |

**Inimigos de exemplo dos papéis sem inimigo (provisórios)**

| Inimigo | Papel | Comportamento | Vida | Andar | Alcance | Dano | XP | Profundidade mínima |
|---|---|---|---|---|---|---|---|---|
| GoblinShaman (Xamã Goblin) | suporte | `Support` | 24 | 1 | 1 | 3 | 14 | 3 |
| RatQueen (Rainha dos Ratos) | invocador | `Summoner` | 30 | 1 | 1 | 4 | 16 | 6 |
| HexingEye (Olho Maldito) | controlador | `Controller` | 22 | 1 | 1 | 3 | 14 | 5 |

**Inimigo que puxa (provisório, G4)**

| Inimigo | Papel | Comportamento | Vida | Andar | Alcance | Dano | XP | Profundidade mínima |
|---|---|---|---|---|---|---|---|---|
| GoblinHookman (Goblin Arpoador) | controlador | `ChaseAndAttack` + skill Gancho | 26 | 1 | 1 | 4 | 14 | 8 (escala de 30 andares) |

O Gancho do Arpoador: dano 3, alcance 4 (Manhattan), cooldown 3, puxa 3 células
em direção a ele.

**Inimigos que cobram a build (provisórios)**

| Inimigo | Papel | Estado permanente | Vida | Andar | Alcance | Dano | XP | Profundidade mínima |
|---|---|---|---|---|---|---|---|---|
| ArmoredGoblin (Goblin Blindado) | corpo a corpo | Blindado (defesa +4) | 30 | 1 | 1 | 5 | 14 | 4 |
| RegeneratingSlime (Slime Regenerante) | corpo a corpo | Regeneração (3 de vida por turno) | 35 | 1 | 1 | 4 | 14 | 4 |
| ThornyRat (Rato Espinhoso) | corpo a corpo | Armadura de Espinhos (devolve 3) | 28 | 1 | 1 | 4 | 12 | 3 |

Todos usam `ChaseAndAttack`.

**Chefe final (provisório)**

| Inimigo | Papel | Vida | Andar | Alcance | Dano | XP | Observação |
|---|---|---|---|---|---|---|---|
| GoblinKing (Rei Goblin) | corpo a corpo, chefe | 120 | 1 | 1 | 10 | 50 | `ChaseAndAttack`; nó final; **imóvel**; skill Onda de Choque (dano 6, área 3×3 ao redor, empurra 2, cooldown 3) |
| GoblinGuard (Guarda Goblin) | corpo a corpo | 36 | 1 | 1 | 5 | 20 | Blindado; 2 compõem a escolta do chefe |

O encontro do chefe (`bossEncounter`) é o Rei Goblin e dois Guardas Goblin,
escalados para a profundidade final. Fora o chefe, **elites** ainda não existem.

Diretrizes de leitura:

- **Slime** — tanque fraco (mais vida, passo curto).
- **Rat / EyeBat** — rápidos e frágeis (`walkDistance = 2`, enxame: aparecem em
  quantidade).
- **FireSkull** — o mais perigoso dos comuns (ataque maior); atira de 2 células e
  recua para manter a distância.
- **Xamã Goblin, Rainha dos Ratos, Olho Maldito** — alvos prioritários que ficam
  atrás da linha de frente.

O encontro do editor (`DefaultEncounter`) coloca os cinco inimigos básicos no
grid de uma vez; as runs não usam isso.

### Skills (6.2)

`SkillDefinition` em `Skills/<Classe>` (e `Skills/Enemies`). Todas ofensivas e
afetam só inimigos; as de deslocamento (acréscimos de balanceamento, G4) têm o
efeito `Displace`. "Ilimitado" = a célula alvo pode ser qualquer uma do grid.

| Skill | Classe | Dano | Área | Tamanho | Alcance | Cooldown | Como se obtém |
|---|---|---|---|---|---|---|---|
| Golpe | Guerreiro | 10 | `Perpendicular` | 3 | 1 | 1 | inicial (não exige alvo) |
| Investida com Escudo | Guerreiro | 12 | `Linear` | 3 | 2 | 3 | talento Muralha |
| Bola de Fogo | Mago | 10 | `Circle` | 3 | ilimitado | 3 | talento Grimório |
| Nova de Gelo | Mago | 8 | `Circle` | 3 | 2 | 2 | inicial |
| Tempestade Elétrica | Mago | 14 | `Cone` | 3 | 3 | 4 | talento Conjurador de Tempestades |
| Adaga de Arremesso | Ladino | 6 | `Circle` | 1 | 3 | 1 | talento Arsenal Oculto |
| Bomba de Fumaça | Ladino | 5 | `Cross` | 1 | 1 | 2 | talento Mestre das Sombras |
| Adaga Envenenada | Ladino | 7 | `Linear` | 3 | 4 | 2 | inicial |
| Linha Venenosa | Ladino | 9 | `Linear` | 5 | 5 | 3 | talento Mestre do Veneno |
| Golpe de Escudo (G4) | Guerreiro | 8 | `Circle` | 1 | 1 | 3 | talento Força Bruta; empurra 2 longe do lançador |
| Rajada de Vento (G4) | Mago | 4 | `Circle` | 3 | 3 | 3 | talento Domínio do Vento; empurra 1 longe do centro da área |
| Gancho (G4) | Ladino | 4 | `Circle` | 1 | 4 | 3 | talento Gancho de Abordagem; puxa 3 em direção ao lançador |
| Gancho do Arpoador (G4) | Goblin Arpoador | 3 | `Circle` | 1 | 4 | 3 | puxa 3 em direção ao inimigo |
| Onda de Choque (G4) | Rei Goblin | 6 | `Circle` | 3 | 0 (ao redor) | 3 | empurra 2 longe do chefe |

> [!note]
> Toda classe começa com ao menos uma skill (decisão de 2026-10-05): Golpe,
> Nova de Gelo e Adaga Envenenada. O Golpe causa dano em área frontal
> (`Perpendicular` de tamanho 3).

### Consumíveis

`ConsumableDefinition` em `Consumables`. Inventário de 3 espaços, 1 uso por
turno, só em batalha.

| Consumível | Mira | Efeito | Peso de sorteio |
|---|---|---|---|
| Poção de Cura | sem alvo | recupera 30 de vida (recusa vida cheia) | 3 |
| Bomba | com alvo (até 3 células, Manhattan; área 3×3) | 15 de dano a cada inimigo na área | 2 |
| Tônico de Regeneração | sem alvo | aplica Regeneração (3 de vida por turno) por 3 turnos | 1 |

### Talentos

`TalentDefinition` em `Talents` (todos provisórios). Cada posto adiciona uma
pilha dos estados do talento (até 5 pilhas).

**Talentos que liberam skill** (peso base 2, um posto):

| Talento | Classe | Nível mínimo | Libera |
|---|---|---|---|
| Muralha | Guerreiro | 2 | Investida com Escudo |
| Grimório | Mago | 2 | Bola de Fogo |
| Conjurador de Tempestades | Mago | 4 | Tempestade Elétrica |
| Arsenal Oculto | Ladino | 2 | Adaga de Arremesso |
| Mestre das Sombras | Ladino | 3 | Bomba de Fumaça |
| Mestre do Veneno | Ladino | 4 | Linha Venenosa |
| Força Bruta (G4) | Guerreiro | 3 | Golpe de Escudo (peso base 1,5) |
| Domínio do Vento (G4) | Mago | 3 | Rajada de Vento (peso base 1,5) |
| Gancho de Abordagem (G4) | Ladino | 3 | Gancho (peso base 1,5) |

**Talentos de reforço da classe** (eram os talentos das skills que viraram
iniciais; peso base 2, um posto): **Criomancia** (Mago) e **Mãos Ligeiras**
(Ladino), cada um com um estado permanente de +15% de dano de skill.

**Talentos genéricos** (pool compartilhada, todas as classes, peso base 1):

| Talento | Nível mínimo | Postos | Efeito por posto |
|---|---|---|---|
| Vitalidade | 2 | 3 | +10 de vida máxima |
| Lâmina Afiada | 2 | 3 | +2 de dano do ataque básico |
| Olho Aguçado | 2 | 3 | +5% de chance de crítico |
| Precisão Letal | 4 | 3 | +0,25 no multiplicador de crítico (exige Olho Aguçado) |
| Perfuração de Armadura | 3 | 3 | ignora +10% da defesa do alvo |
| Robustez | 2 | 3 | +1 de defesa |
| Maestria em Habilidades | 2 | 3 | +10% de dano das skills |
| Longo Alcance | 3 | 2 | +1 de alcance das skills |
| Recuperação Rápida | 3 | 2 | −1 ação no cooldown das skills |
| Armadura de Espinhos | 3 | 3 | devolve +2 de dano |
| Roubo de Vida | 3 | 3 | recupera +5% do dano causado |
| Regeneração | 2 | 3 | +2 de vida no início de cada turno |
| Mais Opções | 4 | 2 | +1 opção por oferta de talento |
| Favor da Sorte | 3 | 3 | +1 reroll |
| Banidor | 3 | 2 | +1 banimento |

As tags de sinergia dos talentos são Ataque, Defesa, Crítico, Habilidades,
Recuperação, Utilidade, Guardião, Arcano, Sombra e Veneno, mais Brigão
(Guerreiro) e Elemental (Mago), criadas no G3.

**Peso dos genéricos (G3):** o talento que só chega à classe pelo pool
compartilhado tem o peso multiplicado por `TalentOfferSettings.sharedPoolWeightMultiplier`
(0,5; um talento que também está no `talentPool` da classe não sofre isso), antes
do bônus de sinergia. Com isso os talentos da classe são cerca de 61% (Guerreiro),
68% (Mago) e 83% (Ladino) do peso de uma oferta com a build vazia no nível 12.

**Talentos de classe (G3, identidade, provisórios).** Cada classe tem talentos que
modificam as próprias skills (`SkillModifierEffect`, vários com 2 a 3 postos,
incluindo as skills de deslocamento) e passivas exclusivas (bônus condicionais, ver
[Bônus condicionais](#bônus-condicionais-dos-estados-passivas)), encadeados por
pré-requisitos (a skill liberada abre os talentos que a melhoram) e espalhados dos
níveis 2 a 13. Os talentos que liberam skill (acima) são os "pré-requisitos" dessas
cadeias.

**Guerreiro** (talentos de classe, G3):

| Talento | Nível mínimo | Postos | Peso | Pré-requisito | Efeito por posto |
|---|---|---|---|---|---|
| Golpe Pesado | 2 | 3 | 1,5 | - | Golpe causa +3 de dano por nível. |
| Frenesi Corpo a Corpo | 2 | 2 | 1,5 | - | +7% de dano por inimigo adjacente (até 3) por nível, com ataques e habilidades. |
| Muralha | 2 | 1 | 2 | - | Desbloqueia Investida com Escudo: avança com o escudo e atinge uma linha de inimigos. |
| Força Bruta | 3 | 1 | 1,5 | - | Desbloqueia Golpe de Escudo: acerta um inimigo adjacente e o empurra 2 casas. Inimigos lançados contra algo sofrem dano extra. |
| Golpe Amplo | 3 | 2 | 1,5 | - | A linha do Golpe cresce 2 casas por nível. |
| Segurar a Linha | 4 | 2 | 1,5 | - | Recebe 4% menos dano de ataques por inimigo adjacente (até 3) por nível. |
| Golpe Concussivo | 5 | 2 | 1,5 | Força Bruta | O Golpe de Escudo empurra 1 casa a mais por nível. |
| Investida Rápida | 5 | 2 | 1,5 | Muralha | A recarga da Investida com Escudo diminui 1 por nível. |
| Golpe Abalador | 6 | 1 | 1,5 | Força Bruta | O Golpe de Escudo também expõe o inimigo por 2 turnos: ele sofre 25% mais dano. |
| Estilhaçar | 6 | 2 | 1,5 | Força Bruta | Inimigos que você empurra ou puxa contra algo sofrem 40% mais dano de colisão por nível. |
| Sede de Sangue | 7 | 2 | 1,5 | - | Matar um inimigo cura 6 PV por nível. |
| Golpe Empurrão | 8 | 1 | 1,5 | Golpe Pesado | O Golpe também empurra 1 casa os inimigos atingidos. |
| Trator | 9 | 2 | 1,5 | Muralha | A Investida com Escudo causa +4 de dano e pode começar 1 casa mais longe por nível. |
| Concussão | 12 | 1 | 1,5 | Golpe Abalador | O Golpe de Escudo também atordoa o inimigo por 1 turno. |

**Mago** (talentos de classe, G3):

| Talento | Nível mínimo | Postos | Peso | Pré-requisito | Efeito por posto |
|---|---|---|---|---|---|
| Foco Arcano | 2 | 2 | 1,5 | - | As habilidades causam +15% de dano por nível se você não se moveu no seu último turno. |
| Toque Gélido | 2 | 1 | 1,5 | - | A Nova de Gelo congela os inimigos por 1 turno: eles não se movem e causam 20% menos dano. |
| Criomancia | 2 | 1 | 2 | - | Domínio do gelo: suas skills causam 15% a mais de dano. |
| Grimório | 2 | 1 | 2 | - | Desbloqueia Bola de Fogo: queima os inimigos de uma área de qualquer ponto do grid. |
| Quebradiço | 3 | 2 | 1,5 | - | +20% de dano por nível a inimigos com um estado nocivo (queimando, congelado, envenenado...). |
| Piromancia | 3 | 3 | 1,5 | Grimório | A Bola de Fogo causa +2 de dano por nível. |
| Domínio do Vento | 3 | 1 | 1,5 | - | Desbloqueia Rajada de Vento: uma rajada que fere os inimigos de uma área pequena e os empurra para longe do centro. |
| Conjurador de Tempestades | 4 | 1 | 2 | - | Desbloqueia Tempestade Elétrica: um raio que se abre em cone a partir do alvo. |
| Ignição | 4 | 2 | 1,5 | Grimório | A Bola de Fogo incendeia os inimigos por 3 turnos (2 de dano por turno por pilha): uma pilha por nível a cada acerto. |
| Gelo Veloz | 4 | 1 | 1,5 | - | A recarga da Nova de Gelo diminui 1. |
| Força do Vendaval | 5 | 2 | 1,5 | Domínio do Vento | A Rajada de Vento empurra 1 casa a mais por nível. |
| Invocador de Tempestades | 6 | 2 | 1,5 | Conjurador de Tempestades | A Tempestade Elétrica causa +3 de dano por nível. |
| Furacão | 7 | 1 | 1,5 | Domínio do Vento | A área da Rajada de Vento cresce um anel (5x5 sem os cantos). |
| Colheita de Almas | 8 | 2 | 1,5 | - | Matar um inimigo reduz em 1 por nível as recargas das suas habilidades. |
| Expansão de Gelo | 9 | 1 | 1,5 | Toque Gélido | A área da Nova de Gelo cresce um anel (5x5 sem os cantos). |
| Explosão Ampliada | 11 | 1 | 1,5 | Piromancia | A área da Bola de Fogo cresce um anel (5x5 sem os cantos). |
| Frente de Tempestade | 13 | 1 | 1,5 | Invocador de Tempestades | O cone da Tempestade Elétrica cresce uma fileira e o alcance 1. |

**Ladino** (talentos de classe, G3):

| Talento | Nível mínimo | Postos | Peso | Pré-requisito | Efeito por posto |
|---|---|---|---|---|---|
| Arsenal Oculto | 2 | 1 | 2 | - | Desbloqueia Adaga de Arremesso: arremessa uma adaga em um inimigo distante. |
| Arma Envenenada | 2 | 3 | 4 | - | Seus ataques básicos também envenenam: 2 pilhas de veneno por nível (4 turnos). |
| Lâminas Envenenadas | 2 | 3 | 4 | - | Suas habilidades envenenam os inimigos atingidos: uma pilha de veneno por nível (1 de dano + 5% da vida máxima por turno por pilha, 4 turnos, até 5). |
| Mãos Ligeiras | 2 | 1 | 2 | - | Mãos rápidas: suas skills causam 15% a mais de dano. |
| Adagas Afiadas | 2 | 3 | 1,5 | - | A Adaga Envenenada e a Adaga de Arremesso causam +4 de dano por nível. |
| Olho de Águia | 3 | 2 | 1 | - | A Adaga Envenenada e a Adaga de Arremesso alcançam 1 casa a mais por nível. |
| Gancho de Abordagem | 3 | 1 | 1,5 | - | Desbloqueia Gancho: puxa um inimigo distante até 3 casas em sua direção. Arraste-o para áreas perigosas ou contra outros inimigos. |
| Bate e Corre | 3 | 2 | 1,5 | - | +30% de dano por nível no turno seguinte a se mover. |
| Mestre das Sombras | 3 | 1 | 2 | - | Desbloqueia Bomba de Fumaça: lança uma bomba de fumaça em um inimigo ao seu lado. |
| Ágil | 3 | 3 | 2 | - | +12 de vida máxima por nível. |
| Predador | 3 | 2 | 2,5 | - | +35% de dano por nível a inimigos envenenados. |
| Fumaça Sufocante | 4 | 1 | 1 | Mestre das Sombras | A Bomba de Fumaça enfraquece o inimigo por 2 turnos: ele causa 50% menos dano. |
| Mestre do Veneno | 4 | 1 | 2 | - | Desbloqueia Linha Venenosa: uma longa linha de veneno que atinge todos os inimigos nela. |
| Adaga Veloz | 4 | 1 | 1,5 | - | A recarga da Adaga Envenenada diminui 1. |
| Veneno Debilitante | 5 | 3 | 2,5 | - | Inimigos com um estado nocivo (veneno, fraquejado, congelado...) causam 15% menos dano a você por nível. |
| Puxão Forte | 5 | 2 | 1 | Gancho de Abordagem | O Gancho puxa 1 casa a mais e alcança 1 casa a mais por nível. |
| Banquete das Sombras | 6 | 3 | 2,5 | - | Matar um inimigo cura 12 PV por nível. |
| Dardo Venenoso | 6 | 2 | 1,5 | Mestre do Veneno | A Linha Venenosa causa +4 de dano por nível. |
| Arremesso Perfurante | 7 | 1 | 1 | Adagas Afiadas | A linha da Adaga Envenenada fica 2 casas mais longa. |
| Fumaça Expansiva | 8 | 1 | 1 | Mestre das Sombras | A área da Bomba de Fumaça cresce um anel (uma cruz de 5 casas). |
| Assassino | 9 | 2 | 2 | - | +40% de dano por nível a inimigos sem nenhum aliado ao lado. |
| Gancho Atordoante | 11 | 1 | 1 | Gancho de Abordagem | O Gancho também atordoa o inimigo por 1 turno. |
| Fumaça Persistente | 12 | 2 | 1 | Fumaça Sufocante | Os estados que a Bomba de Fumaça aplica duram 1 turno a mais por nível. |


### Configurações

| Asset | Valores principais |
|---|---|
| `CombatSettings` | defesa **fixa**; teto do modo percentual 80%; dano mínimo 1; arredondamento ao mais próximo; teto de crítico 100%; dano periódico, espinhos e terreno **ignoram defesa**; colisão: dano 6 + 2 por célula não percorrida, 6 ao personagem atingido, ignora defesa, a borda conta; métrica de andar e de ataque **euclidiana**; de skill **Manhattan**; adjacência dos bônus condicionais **Chebyshev** (8 vizinhos) |
| `SkillSettings` | 6 slots; a skill inicial usa um slot; cooldown mínimo 1 |
| `ConsumableSettings` | 3 espaços; inventário cheio: perguntar ao jogador; 1 uso por turno; só em batalha; o nó sorteia 1 item |
| `ProgressionSettings` | base 50, +25 por nível, nível máximo 30, sobra de XP guardada |
| `TalentOfferSettings` | 3 opções (até 5); 2 rerolls, 1 banimento e 1 pulo por run; peso por sinergia +50% por tag em comum; peso dos talentos só do pool compartilhado ×0,5 (G3); até 6 talentos de skill; a oferta evita repetir as opções na tela |
| `RunSettings` | nó de cura 30% da vida máxima; nó de talento custa 15% da vida máxima (nunca mata); estados temporários removidos entre batalhas; cooldowns zerados entre batalhas; salvamento automático |
| `GridMovementSettings` | estilo `Hop`; avanço do ataque de 5 px em 0,15 s; flash de acerto vermelho; morte de 0,15 s; deslize de empurrões e puxões 0,06 s por célula (`OutQuad`), reação de colisão 0,02 s depois |
| `GameplayInputSettings` | toque longo de 0,45 s |
| `TurnManager` (cena) | ritmo `Staggered`, 0,08 s entre inimigos, aceleração 2x |
| `XpRewardSystem` (cena) | 5 XP por orb |
