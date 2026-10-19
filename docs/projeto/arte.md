---
tags:
  - projeto
  - arte
created: 2026-10-18
---
# Arte: ícones, terreno e peças de interface

Guia de estilo e de manutenção da arte que substituiu os placeholders em 2026-10-18. Os ícones e o terreno
foram gerados no PixelLab; as insígnias de 8×8 foram desenhadas à mão; os slots redondos de item foram
derivados do `UI.png`. **Ficaram de fora** os ícones e visuais das skills (`Art/Sprites/Placeholders/Skills` e
`Placeholders/Vfx`), que ainda vão ser avaliados.

## Onde fica cada coisa

Tudo em `Assets/Application/Art/Sprites/`:

| Pasta | Conteúdo | Tamanho |
|---|---|---|
| `Icons/Talents/` | um ícone por talento (o estado que o talento concede usa o mesmo ícone) | 16×16 |
| `Icons/States/` | estados de combate: Burning, Chilled, Exposed, Stunned, Venom (16×16); Armored, LifeSteal, Poisoned, Regeneration, Shield, ThornArmor, Weakened (8×8, aparecem sobre os inimigos) | 16×16 e 8×8 |
| `Icons/Roles/` | papéis de inimigo (sobre o inimigo e nas fichas do mapa) | 8×8 |
| `Icons/Consumables/` | consumíveis | 16×16 |
| `Icons/Map/` | tipos de nó do mapa | 16×16 |
| `Icons/UI/` | ícones da interface: andar, agir e item do turno, cadeado, skill nova, "combina com a build", dica | 16×16 |
| `Terrain/` | sprites de terreno desenhados sobre a célula (também servem de ícone) | 24×24 |
| `UI/` | slots redondos da barra de itens (normal, pressionado, selecionado) | 32×32 |

Os arquivos mantiveram o GUID dos placeholders (foram movidos pelo `AssetDatabase`), então nenhuma referência
mudou. A única mudança de dados: os estados dos talentos de classe (`Settings/States/Talents/`) passaram a usar o
ícone do talento, como os talentos compartilhados já faziam, e as 43 cópias em `Placeholders/States/` foram
apagadas.

Importação de todos: Sprite (Single), filtro Point, sem compressão, 100 PPU, sem mipmaps.

## Estilo

- **Paleta:** ENDESGA 32, a mesma em todos os ícones e terrenos:
  `#be4a2f #d77643 #ead4aa #e4a672 #b86f50 #733e39 #3e2731 #a22633 #e43b44 #f77622 #feae34 #fee761 #63c74d
  #3e8948 #265c42 #193c3e #124e89 #0099db #2ce8f5 #ffffff #c0cbdc #8b9bb4 #5a6988 #3a4466 #262b44 #181425
  #ff0044 #68386c #b55088 #f6757a #e8b796 #c28569`.
- **Contorno** escuro de 1 px (`#181425`), silhueta forte, objeto centralizado, fundo transparente.
- **Cor por tema**, para o jogador reconhecer de relance: fogo laranja, gelo ciano, vento branco, raio amarelo,
  veneno verde (estado Venom em roxo), fumaça cinza, aço para golpes e escudos, vermelho para vida e sangue.
- **Modificadores com o mesmo sinal**: `+` amarelo = mais dano, anel em volta = área maior, ampulheta = recarga
  menor, seta = alcance ou empurrão.
- **Mapa** em escala de perigo: uma espada (fácil), duas cruzadas (normal), duas com caveira vermelha (difícil),
  caveira com coroa (chefe).
- **Papéis de inimigo** com cores diferentes entre si: corpo a corpo aço, à distância flecha, suporte verde,
  invocador roxo, controlador azul, enxame vermelho.
- **Peças de interface** (slots) usam as cores do `UI.png`, não a paleta dos ícones, para combinar com as molduras
  existentes.

## Como gerar ícones novos no mesmo estilo

1. Use `create_image_pro` em 16×16 com a paleta acima (`palette` + `palette_name: ENDESGA 32`). Nesse tamanho ele
   devolve 64 opções por 10 gerações.
2. Descreva uma lista numerada: cada opção segue o item da mesma posição. Um lote pode ter 64 ícones diferentes
   ("1 fireball with a small yellow plus sign; 2 ...") ou faixas de variações do mesmo ícone ("1-10 ...;
   11-20 ..."). Comece com "64 different 16x16 fantasy RPG icons, one per candidate in this order, bold readable
   silhouette, dark outline:".
3. Escolha a opção, baixe e salve por cima do PNG (mesmo nome) ou crie o arquivo na pasta certa e ligue no campo
   *Icon* do asset.
4. 8×8: o PixelLab não gera menos de 16×16, e reduzir um ícone de 16 apaga o contorno. Desenhe à mão na paleta.
5. Terreno: 24×24, com "isolated on a fully transparent background with no floor and no tile" (sem isso ele
   desenha um piso por baixo, que briga com a madeira da célula).

## Origem de cada asset

Lotes gerados com `create_image_pro` (galeria do PixelLab da conta do Luiz; ainda não há um projeto GridBattle lá):

| Lote | Tamanho | Pedido | Imagem na galeria |
|---|---|---|---|
| A | 16×16 | 64 talentos, um por opção | `eb3e4484-7eef-5455-a8aa-be00b75da5fe` |
| B | 16×16 | talentos compartilhados, estados, consumíveis, mapa, interface | `80707244-d9a8-58f0-b97e-8fe0a94a0cec` |
| C | 16×16 | variações dos talentos que saíram fracos no lote A | `63f71f88-5520-5a3a-ac3e-2dbf9fe80739` |
| D | 16×16 | Venom, Golpe Concussivo, enxame | `7062b847-e940-59ac-b4bc-a803ab861e59` |
| E | 16×16 | bomba de fumaça, frasco de veneno, adagas | `f6861009-5fc4-527b-80bb-3060cde679a8` |
| F | 24×24 | terrenos (fogo, pântano, santuário, rocha, pedra de proteção) | `7336df65-7b88-5b52-9d24-80e2554fdbfe` |
| G | 24×24 | fogo, pântano e pedra de proteção sem piso | `55386739-ed66-553d-9776-9ee579fd52b7` |

Escolhas (lote e número da opção, contando de 1):

- `Icons/Talents/`: Bloodlust (A1), BruteForce (A2), Bulldozer (A3), Concussion (A4), ConcussiveBash (D28),
  HeavyStrike (C7), HoldTheLine (C14), MeleeFrenzy (A8), Muralha (C17), RapidCharge (C22), RattlingBash (A11),
  Shatter (A12), ShovingStrike (C27), WideSwing (A14), ArcaneFocus (A15), Brittle (A16), ChillingTouch (A17),
  ConjuradorDeTempestades (A18), Criomancia (A19), FrostExpansion (A20), GaleForce (A21), Grimorio (A22),
  Hurricane (A23), Ignite (A24), Pyromancy (C52), QuickFrost (A26), SoulHarvest (A27), StormCaller (A28),
  StormFront (C32), WideBlast (A30), WindMastery (A31), ArsenalOculto (A32), Assassin (A33), BillowingSmoke (A34),
  ChokingSmoke (A35), CoatedWeapon (A36), CripplingVenom (C47), EagleEye (A38), EnvenomedBlades (A39),
  GrapplingHook (A40), HitAndRun (A41), LingeringSmoke (A42), MaosLigeiras (A43), MestreDasSombras (E16),
  MestreDoVeneno (A45), Nimble (A46), PiercingThrow (A47), Predator (A48), ShadowFeast (A49),
  SharpenedDaggers (A50), StrongPull (A51), StunningHook (A52), SwiftDagger (A53), VenomBolt (A54),
  ArmorPiercing (A55), Banisher (A56), FarReach (A57), FortunesFavor (A58), KeenEye (A59), LethalPrecision (A60),
  LifeSteal (A61), QuickRecovery (A62), Regeneration (A63), SharpenedBlade (A64), SkillMastery (B1),
  ThornArmor (B2), Toughness (B3), Vitality (B4), WiderChoice (B5).
- `Icons/States/`: Burning (B6), Chilled (B7), Exposed (B8), Stunned (B9), Venom (E29); Armored, LifeSteal,
  Poisoned, Regeneration, Shield, ThornArmor e Weakened à mão.
- `Icons/Roles/`: todos à mão.
- `Icons/Consumables/`: Bomb (B11), HealingPotion (B12), RegenerationTonic (B13).
- `Icons/Map/`: BattleEasy (B14), BattleNormal (B15), BattleHard (B16), Boss (B17), Consumable (B18), Heal (B19),
  Talent (B20).
- `Icons/UI/`: Lock (B21), NewSkill (B22), BuildMatch (B23), TurnMove (B24), TurnAct (B28), TurnItem (B26),
  Tip (B27).
- `Terrain/`: Fire (G16), PoisonBog (G39), WardStone (G45), HealingShrine (F29), Rock (F41).
- `UI/`: ItemSlot, ItemSlotPressed e ItemSlotSelected redesenhados com as camadas e cores dos slots quadrados do
  `UI.png` (contorno, aço claro, aço escuro, contorno, sombra, fundo), com o brilho de bisel nos cantos.

## Pendências

- Ícones e visuais das skills: continuam provisórios até a avaliação do Luiz.
- Os inimigos provisórios ainda são a arte de outros inimigos com outra cor (`tint` no `EnemyConfig`): Goblin
  blindado, Guarda Goblin, Goblin arpoador, Rei Goblin (chefe), Xamã goblin, Olho maldito, Rainha dos ratos,
  Slime regenerante e Rato espinhoso.
