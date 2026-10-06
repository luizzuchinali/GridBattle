---
tags:
  - plano
  - balanceamento
created: 2026-10-05
status: em execução
---
# Plano de balanceamento e profundidade tática

Depois da implementação do [[GDD]] ([[plano_implementacao_gdd]]), o jogo
funciona, mas está desbalanceado. Os números são os padrões neutros de
[[valores_padrao_em_aberto]].

## Problemas relatados (usuário, 2026-10-05)

1. Nós fáceis dando mais XP que nós normais.
2. Muitos talentos repetidos entre as classes (pouca identidade).
3. Nas profundidades 8–9 a luta vira só checagem de números; não há o que fazer
   mecanicamente para vencer.
4. Ideia: skills de empurrar e puxar.

Meta escolhida: **difícil, roguelite clássico**. Um jogador médio vence cerca
de 1 run em 10 no começo e melhora com conhecimento e builds.

## Etapas

- **G1. Simulador de balanceamento** (C#, roda no Unity):
  - um bot joga runs completas pelo `RunManager`, com as regras reais e sem
    animações, com seed;
  - mede por classe: vitória, profundidade da morte, vida perdida por batalha,
    nível por profundidade, XP por nó e dificuldade, turnos por batalha,
    fontes de dano, talentos escolhidos;
  - tudo configurável por asset.
- **G2. XP coerente:** XP da batalha derivado do orçamento de ameaça
  (profundidade × dificuldade) e distribuído entre os inimigos pela ameaça de
  cada um. Garante fácil < normal < difícil na mesma profundidade.
- **G3. Identidade de classe nos talentos:** talentos que modificam as skills
  da própria classe e menos peso do pool genérico.
- **G4. Empurrar e puxar:** efeitos de skill (e de IA) que deslocam
  personagens, com dano de colisão e interação com o terreno.
- **G5. Profundidade tática nas lutas finais:** mais variedade de papéis e de
  terreno nas profundidades altas e crescimento de números mais contido.
- **G6. Ajuste fino com o simulador** até as metas, com os valores finais
  documentados.

## Decisões do usuário (2026-10-06)

- **Empurrar e puxar:**
  - skills novas, uma por classe e dedicadas a deslocar, liberadas por
    talento;
  - inimigos também empurram e puxam;
  - outras skills novas podem ser criadas como escolhas de balanceamento.
- **Colisão:** empurrado ou puxado contra obstáculo, borda ou personagem, o
  personagem para e leva dano de colisão (o outro personagem também). Ao parar
  numa célula de perigo ou de bônus, o efeito dela vale na hora. Valores
  configuráveis.
- **Identidade de classe:** talentos que modificam as skills da classe
  (dano, área, alcance, recarga, efeitos extras) + 2–3 **passivas exclusivas**
  por classe; genéricos com peso menor na oferta.

## Desenho das próximas etapas

- **G4 (deslocamento):**
  - efeito `Displace` (empurrar a partir do lançador ou do centro da área;
    puxar em direção ao lançador), com N células, colisão e reação do
    terreno, em `GridRules`;
  - ação de IA de empurrar/puxar e um inimigo provisório que puxa;
  - o chefe empurra;
  - uma skill de deslocamento por classe, liberada por talento;
  - skills novas extras para dar opções táticas, se o simulador mostrar falta.
- **G3 (identidade):**
  - efeito de estado `SkillModifier` (alvo: skills específicas ou todas da
    classe; dano, área, alcance, recarga, efeitos anexados);
  - ganchos de dano condicionais (por exemplo, número de inimigos
    adjacentes, sem inimigos adjacentes, alvo com estado negativo, ter se
    movido no turno anterior) para as passivas;
  - cerca de 10 talentos de classe por classe + 2–3 passivas;
  - peso menor para os genéricos.
- Ordem: G1 (linha de base) → G2 → G4 → G3 → G5/G6 com o simulador.

## Decisões do usuário (2026-10-06, tamanho do mapa)

- O mapa tem **30 andares** (`floorCount`).
- O balanceamento deve se ajustar ao número de andares: os valores por
  profundidade valem para 30 andares (`balanceFloorCount`) e se esticam para
  outro tamanho de mapa (ver [[balanceamento_e_geracao]]).

## Decisão do usuário (2026-10-06, custo do ajuste fino)

- O ajuste numérico não é feito por agentes caros testando à mão. Ele usa
  uma ferramenta no Unity:
  - metas em asset (`BalanceTargets`), conferidas a cada lote numa tabela
    curta de "passou ou falhou";
  - alavancas permitidas em asset (`BalanceTuningSettings`), com faixas de
    valores;
  - `BalanceTuner`: varredura e ajuste automático, que escolhe os valores
    mais próximos das metas e os grava nos assets.
- Agentes baratos (Haiku) operam a ferramenta e aplicam as mudanças só nas
  alavancas permitidas. Mudanças de regra e de estrutura, e a revisão, ficam
  com modelos maiores.

## Estado (2026-10-06)

- **G1 concluído.** O simulador (`Gameplay/Simulation`, `Editor/Simulation`)
  joga runs completas com as regras reais, cerca de 5 a 10 runs por segundo.
  Com ele desligado, a partida determinística continua idêntica. Uso:
  [[guia_sistemas]], seção 8.
- **Linha de base** (50 runs por classe, seeds 1000–1049, mapa misto, valores
  de 11 andares num mapa de 30):
  - nenhuma vitória; morte em média no andar 6,1 (Guerreiro), 4,6 (Mago) e
    4,7 (Ladino); o nível 2 só chegava por volta do andar 4;
  - XP invertido: fácil > normal em 20% dos pares do mesmo andar, normal >
    difícil em 15,5%;
  - mesmo com a vida cheia antes de cada batalha, as runs morriam nos andares
    7–8,5;
  - dano recebido: corpo a corpo 65%, enxame 22%, atirador 11%; 92% por
    ataque básico; o terreno quase não importava.
- **G2 concluído:**
  - profundidade de balanceamento (`balanceFloorCount` = 30); valores de 11
    andares convertidos para 30;
  - XP pelo orçamento de ameaça (`xpSource = ThreatBudget`, 8,5 por ponto,
    +7% por andar); conferido sem inversões em 200 mapas de 11, 30 e 60
    andares, e com o chefe e o nível final iguais nos três tamanhos.
- **Depois do G2** (mesmas seeds):

  | Classe | Vitória | Andar médio | Nível médio | Vitória com vida cheia antes de cada batalha |
  |---|---|---|---|---|
  | Guerreiro | 2% | 12,4 | 7,3 | 46% |
  | Mago | 2% | 10,0 | 6,0 | 44% |
  | Ladino | 0% | 8,3 | 5,0 | 0% |

  - 0 inversões de XP nas batalhas jogadas;
  - o que mata é o **desgaste** (vida perdida entre batalhas); o Ladino é
    fraco mesmo com vida cheia;
  - preferir batalhas difíceis rende só ~1 nível a mais (o mapa nem sempre
    oferece a escolha). Fica para o G6.
- **Cura ao subir de nível** (decisão do usuário, 2026-10-06): cada nível
  recupera toda a vida (`ProgressionSettings.levelUpHealFraction` = 1).
  Resultado (mesmas seeds):

  | Classe | Vitória (antes) | Andar médio (antes) | Nível médio |
  |---|---|---|---|
  | Guerreiro | 58% (2%) | 26,0 (12,4) | 16,1 |
  | Mago | 50% (2%) | 26,3 (10,0) | 16,4 |
  | Ladino | 6% (0%) | 19,2 (8,3) | 11,2 |

  - o desgaste deixou de ser o problema: com vida cheia antes de cada
    batalha, a vitória sobe só para 68% / 56% / 8%;
  - preferir batalhas difíceis agora compensa um pouco (Guerreiro: 60% no
    caminho difícil × 56% no fácil), porque mais XP dá mais níveis e mais
    curas;
  - o jogo ficou **bem acima da meta** (roguelite difícil, ~1 vitória em 10
    para um jogador; o bot é pior que um jogador). O G6 deve endurecer as
    lutas (orçamento, escala, papéis), que agora são o desafio de cada
    batalha;
  - o Ladino continua muito fraco (G3/G6);
  - 1 run abortada por limite de turnos com RatQueen (invocações sem fim; G6).
- **G4 concluído** (revisado: código, IA, textos nos 3 idiomas,
  `TalentChecks` 445/445, partida determinística idêntica):
  - deslocamento (`Displacement`, `DisplacementResolver`,
    `DisplaceSkillEffect`), dano de colisão 6 (+2 por casa não percorrida; 6
    ao personagem atingido), terreno reage na hora, `canBeDisplaced`;
  - Golpe de Escudo (Guerreiro, empurra 2), Rajada de Vento (Mago, empurra 1
    do centro; com 2 o Mago chegava a 72% de vitória), Gancho (Ladino, puxa 3),
    cada uma liberada por um talento da classe (nível 3);
  - Goblin Arpoador (puxa o jogador) a partir do andar 8; o chefe empurra
    (Onda de Choque) e é imóvel;
  - simulador (mesmas seeds): Guerreiro 46%, Mago 58%, Ladino 8% de vitória.
    Forçando os talentos novos: 60% / 64% / 6%. Cada run com o talento usa a
    skill nova de 9 a 25 vezes;
  - impasse encontrado: Xamã Goblin sozinho sobre a Pedra de proteção
    renova o escudo de 15 a cada turno e o bot não o mata (G6).

- **G3 concluído** (revisado: `TalentChecks` 7777/7777, partida
  determinística idêntica, textos nos 3 idiomas):
  - `EffectiveSkill` (números efetivos das skills, usado pela mira, recarga,
    IA, HUD e bot) e `SkillModifierEffect` (dano, área, alcance, recarga,
    empurrão, duração de estados, efeitos anexados);
  - dano condicional (`ConditionalDamageEffect`: por inimigo adjacente, sem
    adjacentes, alvo com estado negativo ou estado específico, moveu ou não no
    último turno, alvo isolado, alvo com vida cheia), `OnKillEffect`,
    `ApplyStateOnHitEffect`;
  - 43 talentos de classe novos (Guerreiro 12, Mago 13, Ladino 18) e estados
    novos (Queimando, Peçonha, Congelado, Atordoado, Exposto);
    `sharedPoolWeightMultiplier` = 0,5 (genéricos pesam metade);
  - simulador (mesmas seeds):

    | Política de talentos | Guerreiro | Mago | Ladino | Talentos de classe escolhidos |
    |---|---|---|---|---|
    | Primeira opção | 60% | 60% | 50% | 64% / 70% / 85% |
    | Maior sinergia | 72% | 70% | 54% | 79% / 90% / 98% |

  - pendências: o Ladino ainda fica abaixo; o Atordoado pega o chefe (falta
    imunidade); a Peçonha (5% da vida máxima por pilha, até 5) é muito forte
    contra o chefe.

**Ao retomar:**
1. G5/G6 (agente): especificação em `scratchpad/g56/G56_SPEC.md` da sessão
   `d1b6d413`.
