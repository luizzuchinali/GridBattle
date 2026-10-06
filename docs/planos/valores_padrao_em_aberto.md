---
tags:
  - plano
  - balanceamento
  - revisar
created: 2026-10-05
status: para revisão
---
# Valores padrão das perguntas em aberto

Cada ❓ dos documentos de design virou um campo editável no Inspector. Este
documento lista o valor inicial escolhido para cada um. **Nenhum valor vem de
`docs/propostas/`.** São escolhas neutras para o jogo funcionar; ajuste
direto nos assets indicados.

Ver também: [[plano_implementacao_gdd]], [[perguntas_em_aberto]].

## Combate e estados (`Settings/Combat/CombatSettings`, estados em `Settings/States`)

| Pergunta | Documento | Valor | Onde |
|---|---|---|---|
| Defesa: fixa ou porcentagem | estados_e_atributos, 2.9 | Fixa (dano − defesa); modo percentual disponível com teto de 80% | `CombatSettings.defenseMode` |
| Dano mínimo | 2.9 | 1 (quando o dano base > 0) | `CombatSettings.minimumDamage` |
| Multiplicador de crítico | estados_e_atributos | 2,0 (chance de crítico 0 em todos os personagens hoje) | cada `CharacterConfig` |
| Métrica de distância | classes_e_skills | Movimento e ataque: euclidiana (como já era). Alcance de skill: Manhattan (GDD) | `CombatSettings` |
| Dano ao longo do tempo, espinhos e terreno ignoram defesa | — | Sim (configurável) | `CombatSettings` |
| Reaplicação de estados | estados_e_atributos | Renova a duração (fica a maior). Outras políticas por estado: somar duração, acumular pilhas, ignorar | `StateDefinition.stackPolicy` |
| Fraquejado | estados_e_atributos | Dano causado −50% (do documento) | `Weakened` |
| Armadura de Espinhos | estados_e_atributos | Devolve 3 de dano por golpe recebido | `ThornArmor` |
| Roubo de vida | estados_e_atributos | 15% do dano causado | `LifeSteal` |
| Regeneração | estados_e_atributos | 3 de vida por turno (valor fixo) | `Regeneration` |
| Escudo | estados_e_atributos | 15 pontos; some quando esgota | `Shield` |
| Envenenado | estados_e_atributos | 3 de dano por turno | `Poisoned` |
| Estados ao mudar de batalha | estados_e_atributos | Temporários são removidos; permanentes continuam | `RunSettings` |
| Atributos e estados valem para inimigos | estados_e_atributos | Sim, o mesmo modelo | — |
| Empurrar e puxar: dano de colisão (balanceamento G4) | plano_balanceamento | 6 ao deslocado, +2 por célula que não percorreu, 6 ao personagem atingido (nunca ao lançador); ignora defesa; a borda do grid conta como colisão; sem crítico, espinhos nem roubo de vida | `CombatSettings` (seção Displacement) |
| Empurrar e puxar: direção e parada | plano_balanceamento | 8 vizinhos (ângulo a 45°); para em obstáculo, borda, personagem; ao puxar, para ao lado do lançador; a célula final de perigo ou bônus reage na hora (além do gatilho normal) | `Displacement`, `TerrainEffects` |
| Bônus condicionais de dano (balanceamento G3) | plano_balanceamento | Estados mudam o dano de golpes mirados (ataque básico e skill) conforme a situação, pela máscara `EDamageKindMask` do efeito; dano periódico, espinhos e terreno só se o efeito pedir; dano exato nunca. Colisões podem ser incluídas (Estilhaçar) | `ConditionalDamageEffect`, `OnKillEffect`, `ApplyStateOnHitEffect` |
| Adjacência dos bônus condicionais (G3) | plano_balanceamento | Distância 1 pela métrica Chebyshev (8 vizinhos); só personagens vivos | `CombatSettings.adjacencyMetric` |
| "Andou no turno anterior" (G3) | plano_balanceamento | Andar ou teletransportar conta; ser empurrado ou puxado não. Entra no salvamento da batalha | `Character.MovedLastTurn`, `EntitySnapshot.MovedLastTurn` |
| Estados novos (G3, provisórios) | plano_balanceamento | Queimando: 2 por pilha, até 3. Veneno letal: 1 + 5% da vida máxima e −6% de dano causado por pilha, até 5. Congelado: não anda, −20% de dano. Atordoado: não anda, ataca nem usa skills. Exposto: +25% de dano recebido | `Settings/States` |
| Personagem imóvel | plano_balanceamento | Só o chefe (`canBeDisplaced` desligado): não se move, mas bloqueia e leva o dano de quem for lançado nele | `CharacterConfig.canBeDisplaced` |

## Skills (`Settings/Skills/SkillSettings`)

| Pergunta | Documento | Valor |
|---|---|---|
| A skill inicial ocupa um dos 6 espaços | classes_e_skills | Sim |
| Redução de cooldown: unidade e mínimo | estados_e_atributos | Em ações; cooldown mínimo 1 para skills que têm cooldown |
| Cooldowns entre batalhas | — | Zerados a cada batalha (`RunSettings`) |
| Habilidades dos inimigos | inimigos | Mesma regra das skills: "N ações entre usos" |
| Modificadores de skill dos talentos (G3) | plano_balanceamento | Todo número de skill vem do `EffectiveSkill`: dano fixo e percentual (só skills com dano), área em passos de +2 (máx. 9), alcance (menos nas centradas e ilimitadas), cooldown (nunca abaixo do mínimo), empurrão, duração de estados e efeitos anexados. Valores por pilha; pilha = posto | `SkillModifierEffect` |
| Redução de cooldown ao matar (G3) | plano_balanceamento | Entra no fim do turno do dono, antes de os cooldowns caírem 1 (inclui a skill usada no golpe) | `OnKillEffect`, `SkillCooldowns.QueueReduction` |

## Inimigos (`Settings/AI`, `Settings/Characters/Enemies`)

| Pergunta | Documento | Valor |
|---|---|---|
| Papel dos inimigos atuais | inimigos | Goblin e Slime: corpo a corpo. Rat e EyeBat: enxame. FireSkull: atirador (passa a manter distância) |
| Fator de ameaça por papel | balanceamento | 1 para todos |
| Invocados dão XP | xp_e_niveis | Não |
| Inimigos de exemplo dos papéis sem inimigo | inimigos | GoblinShaman (suporte), RatQueen (invocador), HexingEye (controlador): **provisórios**, com arte existente recolorida |
| Inimigos que cobram a build | inimigos | ArmoredGoblin (defesa), RegeneratingSlime (regeneração), ThornyRat (espinhos): **provisórios** |
| Chefe final | inimigos, mapa_e_nos | GoblinKing com 2 GoblinGuard: **provisório**; ganhou a Onda de Choque (empurra 2) e é imóvel (G4) |
| Inimigo que puxa (G4) | plano_balanceamento | GoblinHookman (Goblin Arpoador): **provisório**, papel controlador, a partir do andar 8 (escala de 30 andares), skill Gancho (puxa 3, cooldown 3) |
| IA de skills de deslocamento (G4) | plano_balanceamento | Só usa quando a pontuação prevista chega a 1: dano de colisão e de terreno, +5 por aliado que passa a alcançar o jogador, +1 por célula de aproximação, +50 por abate | `UseSkillsAction.displacement` |

## Terreno (`Settings/Terrain`)

| Pergunta | Documento | Valor |
|---|---|---|
| Quando o efeito é aplicado | grid_e_terreno | No fim do turno de quem está na célula |
| Bloqueia área de skill | grid_e_terreno | Não |
| Afeta jogador e inimigos | grid_e_terreno | Sim, igualmente |
| Quantidade por profundidade | grid_e_terreno | Andares 1–3: nada. 4–12: 0–2 obstáculos, 0–1 perigo, 0–1 bônus. 13+: 1–3, 1–2, 0–1 (num mapa de 30 andares) |
| Tipos | grid_e_terreno | Rocha (obstáculo), Fogo (4 de dano), Pântano (Envenenado 3), Santuário (Regeneração 3), Pedra de proteção (Escudo 2) |

## Consumíveis (`Settings/Consumables`)

| Pergunta | Documento | Valor |
|---|---|---|
| Espaços | consumiveis | 3 |
| Inventário cheio | consumiveis | O jogador escolhe qual descartar, ou recusa o novo |
| Sorteado ou escolhido | consumiveis | 1 item sorteado pela seed (`choiceCount` > 1 permite escolher) |
| Precisa de alvo | consumiveis | Depende do item: poção e tônico sem alvo; bomba com alvo |
| Aparecem no glossário | consumiveis | Não |

## Mapa e batalhas (`Settings/Map`, `Settings/Run`)

| Pergunta | Documento | Valor |
|---|---|---|
| Formato do mapa | mapa_e_nos | 30 andares (escolha do usuário; o último só com o chefe), 4 colunas, 2–4 nós por andar, até 2 caminhos por nó, sem cruzamentos (~85 nós, ~21 batalhas por caminho) |
| Tamanho de referência do balanceamento | — | 30 andares (`MapGenerationSettings.balanceFloorCount`). Os valores por profundidade abaixo valem para 30 andares e se esticam para outro tamanho de mapa |
| Distribuição dos tipos de nó | mapa_e_nos | Profundidade 1 só batalhas; depois 55% batalha, 15% cura, 10% talento, 20% consumível |
| Dificuldade dentro do andar | mapa_e_nos | Fácil 35%, normal 40%, difícil 25% |
| Cura do nó de cura | balanceamento | 30% da vida máxima |
| Custo do nó de talento | mapa_e_nos | 15% da vida máxima (nunca deixa abaixo de 1) |
| Força esperada por profundidade | balanceamento | Orçamento de ameaça 6 + 0,52 por andar (os 6 + 1,5 pensados para 11 andares, convertidos); fácil ×0,75, normal ×1, difícil ×1,35 |
| Escala dos inimigos | balanceamento | +2,1% de vida e +1,03% de dano por andar (os +6% e +3% de 11 andares, convertidos) |
| Limite de inimigos | balanceamento | 1 a 8, ocupação máxima de 40% do grid |
| Composição | balanceamento | Pelo menos um inimigo corpo a corpo ou enxame |
| Pool por profundidade | balanceamento | Os 5 originais desde o andar 1; os provisórios a partir dos andares 7 (GoblinShaman, ThornyRat), 10 (RegeneratingSlime, ArmoredGoblin), 13 (HexingEye) e 16 (RatQueen) |

## Perfil, meta e interface (`Settings/Meta`, `Settings/Input`, `Settings/Hud`)

| Pergunta | Documento | Valor |
|---|---|---|
| Batalhas de runs perdidas contam para liberar classes | meta_progressao_e_perfil | Sim |
| O que conta como talento descoberto | meta_progressao_e_perfil | Só o escolhido |
| Inimigos não enfrentados no glossário | meta_progressao_e_perfil | Aparecem como "?" |
| Desistir conta como derrota | interface 4.3 | Sim |
| Dicas podem ser desativadas | interface 4.4 | Sim; ativadas por padrão |
| Métricas | GDD 9 | Só locais (`metrics.jsonl`), ativadas |
| Tempo do toque longo | interface 4.1 | 0,45 s |
| Profundidade na HUD | interface 4.2 | Bloco à esquerda da barra de XP |
| Atributos na janela de detalhes | interface 4.1 | Os básicos sempre; os demais só quando diferentes de zero |
| Idioma na primeira abertura | GDD 8.1 | Idioma do sistema, com inglês como padrão |
| Faixas de música de batalha | GDD 5.2 | Qualquer número; uma sorteada, sem repetir em seguida |
| Música na pausa de talento | GDD 5.2 | Continua tocando |
| Mapa mostra os consumíveis | interface 4.2 | Sim, só para consulta (`ScreensSettings.showConsumablesOnMap`) |
| Glossário durante a escolha de talento | interface 4.3 | Não (`ModalsSettings.allowGlossaryDuringTalentChoice`) |
| Como banir | interface 4.1 | Modo de banir: tocar em Banir e depois no talento |
| Confirmação ao pular talento e ao desistir | interface 4.3 | Sim (`ModalsSettings.confirmSkip/confirmGiveUp`) |
| Dicas sobre a escolha de talento | interface 4.4 | Sim; nunca sob outro modal |

## XP, nível e talentos (`Settings/Progression`, `Settings/Talents`)

| Pergunta | Documento | Valor |
|---|---|---|
| Curva de XP | xp_e_niveis | 50 para o nível 2, +25 por nível (a de antes). Agora é global (`ProgressionSettings`), fora das classes |
| Nível máximo | xp_e_niveis | 30 (exemplo do documento). Com 30 andares a run chega perto do nível 19 antes do chefe |
| Sobra de XP ao subir de nível | xp_e_niveis | É guardada |
| Vida ao subir de nível | xp_e_niveis | Recupera toda a vida a cada nível (decisão do usuário, 2026-10-06; `ProgressionSettings.levelUpHealFraction`) |
| XP por batalha | xp_e_niveis | Orçamento de ameaça × 8,5 × (1 + 0,07 × (profundidade − 1)), dividido pela ameaça dos inimigos (`xpSource = ThreatBudget`). Fácil < normal < difícil no mesmo andar. Andar 1: 38 / 51 / 69; andar 15: ~160 / 214 / 288 |
| Opções por oferta | talentos_e_oferta | 3 (GDD), até 5 com talentos |
| Rerolls, banimentos e pulos por run | talentos_e_oferta | 2, 1 e 1 (mais os de talentos) |
| Peso por sinergia | talentos_e_oferta | Peso base × (1 + 0,5 × tags em comum com os talentos escolhidos) |
| Peso dos talentos genéricos (G3) | talentos_e_oferta | ×0,5 para o talento que só chega à classe pelo pool compartilhado (`sharedPoolWeightMultiplier`), antes da sinergia | `TalentOfferSettings` |
| Banir | talentos_e_oferta / interface | O talento sai da run e a mesma posição recebe um novo sorteio |
| Pular | talentos_e_oferta | O talento daquele nível é perdido |
| Depois do 6º talento de skill | talentos_e_oferta | Talentos que liberam skill deixam de ser oferecidos |
| Talento com vários níveis | talentos_e_oferta | Por talento (`maxRank`); os genéricos têm 2–3 |
| Nó de talento conta como nível | xp_e_niveis | Não; dá só o talento, escolhido entre os talentos do nível atual + 1 (`talentNodeLevelBonus`) |
| Talentos de classe (G3) | talentos_e_oferta, classes_e_skills | 14 (Guerreiro), 17 (Mago) e 23 (Ladino) talentos próprios, 24 a 42 postos: modificadores das skills da classe e passivas exclusivas; níveis mínimos de 2 a 13; encadeados por pré-requisitos. **Provisórios** (tabelas em sistema_combate) | `Settings/Talents/<Classe>` |
| Talentos de exemplo | talentos_e_oferta, classes_e_skills | As 6 traits que liberam as skills da tabela 6.2 (Criomancia e Mãos Ligeiras viraram reforço: +15% de dano de skill, porque Nova de Gelo e Adaga Envenenada são iniciais) e 15 genéricos (um por atributo, os estados do documento e +opções/+rerolls/+banimentos): **provisórios** |

## Mapa: escolha de risco e recompensa
- Novo (`MapGenerationSettings.diversifyBattleChoices`, ligado): quando um nó
  leva a duas ou mais batalhas, elas têm dificuldades diferentes, o mesmo no
  primeiro andar.
- **Para calibrar (2026-10-06, 30 andares):**
  - uma batalha difícil dá 1,8× o XP de uma fácil no mesmo andar;
  - mas quem prefere difíceis termina só ~1 nível acima de quem prefere
    fáceis (19,5 × 18,7), porque o caminho só oferece a dificuldade preferida
    em 35–45% das batalhas. Se toda batalha fosse a preferida: 16,5 (fácil),
    19,1 (normal), 22,3 (difícil);
  - aumentar o multiplicador de XP por dificuldade em
    `BattleGenerationSettings` fortalece essa escolha (etapa G6 do
    [[plano_balanceamento]]).
