---
tags:
  - projeto
  - arte
created: 2026-10-18
---
# Arte: ícones, terreno e peças de interface

Guia de estilo e de manutenção da arte que substituiu os placeholders em 2026-10-18. Os ícones e o terreno
foram gerados no PixelLab; as insígnias de 8×8 foram desenhadas à mão; os slots redondos de item foram
derivados do `UI.png`. Os ícones das skills vieram depois, no mesmo estilo. Na refatoração do novo molde
(2026-10-19) entraram, no mesmo estilo, os sprites e as animações paradas dos 10 inimigos novos, os ícones das
skills, dos talentos de regra, dos estados Congelado e Cego e dos nós de acampamento e elite, e a Muralha de Gelo
(terreno). **Ficaram de fora** os visuais das
skills (`Art/Sprites/Placeholders/Vfx`), que ainda vão ser avaliados.

## Onde fica cada coisa

Tudo em `Assets/Application/Art/Sprites/`:

| Pasta | Conteúdo | Tamanho |
|---|---|---|
| `Icons/Skills/` | um ícone por skill, das classes e dos inimigos (mostram a ação; os talentos que modificam a skill mostram o modificador) | 16×16 |
| `Icons/Talents/` | um ícone por talento (o estado que o talento concede usa o mesmo ícone) | 16×16 |
| `Icons/States/` | estados de combate: Blinded, Burning, Chilled, Exposed, Frozen, Stunned, Venom, Webbed (16×16); Armored, LifeSteal, Poisoned, Regeneration, Shield, ThornArmor, Weakened (8×8, aparecem sobre os inimigos) | 16×16 e 8×8 |
| `Icons/Roles/` | papéis de inimigo (sobre o inimigo e nas fichas do mapa) | 8×8 |
| `Icons/Consumables/` | consumíveis | 16×16 |
| `Icons/Map/` | tipos de nó do mapa (inclusive acampamento e elite) | 16×16 |
| `Icons/UI/` | ícones da interface: andar, agir e item do turno, cadeado, skill nova, "combina com a build", dica | 16×16 |
| `Terrain/` | sprites de terreno desenhados sobre a célula (também servem de ícone), inclusive a Muralha de Gelo (`IceWall.png`), criada por skill | 24×24 |
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
6. Ícones de skill: o modelo tende a desenhar um quadrado escuro de fundo ("ícone de habilidade"). Peça
   "single isolated object on a fully transparent background, no frame, no tile, no square background" e confira a
   transparência antes de usar.

## Como fazer um inimigo novo com arte própria

1. **Sprite parado de frente (24×30):** `create_image_pro` com a imagem de estilo recortada do `Characters.png`
   (os personagens que já existem) e uma lista de faixas, uma por inimigo ("1-21 ..., 22-42 ..., 43-64 ...").
   ⚠️ Em tamanhos não quadrados (24×30) o PixelLab devolve as opções recortadas fora do lugar (pedaços do vizinho).
   Junte as 64 opções de volta numa folha 8×8, na ordem, e recorte cada sprite pelas partes conectadas (pixels não
   transparentes). Depois centralize o escolhido num quadro 24×30, com os pés na borda de baixo.
2. **Animação parada:** `animate_image` com o sprite como primeiro e último quadro e 8 quadros ("subtle looping
   idle ... returns to the first pose"), 1 geração. O resultado tem 9 imagens; use as 8 primeiras.
3. **No Unity:** tira de quadros em `Art/Sprites/Enemies/<Nome>.png` (Multiple, 24×30, pivô embaixo no centro),
   clipe `<Nome>Idle` e `<Nome>AnimController` em `Art/Animations/Enemies/<Nome>/`, `EnemyConfig` com `sprite`,
   `animatorController` e `tint` branco.
4. **Habilidades:** `SkillDefinition` em `Settings/Skills/Enemies/` (com a opção de telegrafar) e ícone em
   `Icons/Skills/`. Lembre que a IA só usa uma skill com empurrão ou puxão quando o deslocamento vale a pena; um golpe
   que deve ser usado sempre não deve ter `Displace`.

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
| H | 16×16 | skills (muitos saíram com fundo escuro) | `0f5f1baa-b3d6-52f1-b011-3e1d938c1f5d` |
| I | 16×16 | skills com fundo transparente | `b479cd9b-0687-544c-a01e-b3990bfd4235` |
| J | 16×16 | bomba de fumaça e chuva de meteoros | `b681b925-9479-5406-8258-30abc6668d41` |
| K | 24×30 | Goblin Bombardeiro, Ogro Brutamontes e Aranha Tecelã | `de17d061-d211-5b73-a52c-6839181eae61` |
| L | 24×30, 8 quadros | animações paradas (Bombardeiro, Ogro, Aranha) | `b48d3592-cfe5-5a21-9f72-1bbac756ee85`, `e6d2b4f1-6fa9-54d5-b656-a9d61c0880ed`, `c2dbcb22-838d-5b62-878f-e53c77f6db7c` |
| M | 24×30 | 10 inimigos novos do novo molde (commit `650886b`) | `29b755aa-1057-5b85-938c-9115275cfba5` |
| N | 24×30, 8 quadros | animações paradas dos 10 inimigos novos, uma imagem por inimigo (ver as escolhas) | `1f60b062-4ca6-583c-980b-986b4ac2b176`, `29ec6f8e-df3c-56d6-a7ec-abfb4635acef`, `ad3b5079-b027-5b4a-bd68-0cd83d85aa1d`, `1774a0d0-df9b-5e29-8722-8c6b9d8ad537`, `334686ed-bba4-58da-80d5-137111656670`, `6bf91957-c165-58c8-bb9b-6d21dcac64ab`, `7bb23e5f-9540-5b90-8d4a-676012afb169`, `17a96b71-1465-5c1c-9da1-85fd1f29358b`, `dd1971b3-9dac-5078-9c60-24fa987a7456`, `3d99bb1b-dd61-5b1b-b3bd-9f7cbe17afc2` |
| O | 16×16 | ícones do novo molde (commit `37ff643`): skills novas das classes e dos inimigos, talentos de regra, Congelado, Cego, acampamento e elite; trabalho `f981155d` | `51347edd-11af-51f6-bc35-2a7d63fe9770` |
| P | 24×24 | Muralha de Gelo (terreno), sem piso; trabalho `e9411cfc` | `62516f6f-5f09-5b4f-8c21-8c0062fa9ae8` |
| Q | 16×16 | 64 opções, paleta ENDESGA 32: os 17 golpes novos dos inimigos e o estado Exausto (2026-10-20, commit `1288849`); trabalho `869d91fc-6616-4992-8d7b-71ec3a897624` | `db6d7dab-39c7-5104-a011-99f1d1b5856b` |
| R | 16×16 | 64 opções, paleta ENDESGA 32: os 5 consumíveis novos, 13 opções de cada (1-13 Granada de Impacto, 14-26 Pergaminho de Troca, 27-39 Pedra, 40-52 Frasco de Fogo, 53-64 Frasco de Gelo) (2026-10-20); trabalho `097b3e59-017d-4b6f-af76-b9859fbbfdb8` | `fff10c2c-bc5b-5283-ab98-225535ae8d9f` |

Nos lotes O e P, o registro da geração guardou o número do trabalho (`f981155d`, `e9411cfc`), não o da imagem na
galeria. As imagens da coluna da direita são as únicas da galeria com esse tamanho e essa quantidade de opções
geradas depois do lote J, então a ligação foi feita por isso. O registro contou as opções a partir de 0; abaixo
elas aparecem contando de 1, como nos outros lotes (opção = candidato + 1).

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
- `Icons/Skills/`: Strike (I6), ShieldBash (H6), ShieldCharge (H9), Fireball (H13), FrostNova (H15), Gust (H18),
  LightningStorm (I8), Hook (I14), PoisonLine (I22), PoisonedDagger (H32), SmokeBomb (J6), ThrowingDagger (I29),
  GoblinHook (I32), KingShockwave (I39), KingSlam (I46), FireRain (J34), CrushingBlow (I51), Web (I55).
  Do novo molde: ShadowStep (O1), Stomp (O3), ShieldThrow (O5), Quake (O7), IceWall (O9), Meteor (O11),
  ClubStrike (O34), Bite (O35), AcidSplash (O37), FlameBolt (O39), DiveBite (O41), Spark (O44), Screech (O45),
  HexGaze (O48), ShieldPush (O49), Thrust (O51), Ram (O53), Mortar (O55), Bolt (O57), Tremor (O58),
  BogCurse (O59), VampireBite (O60), Gale (O61), Sweep (O62), DarkCharge (O63), Eruption (O64).
  Golpe básico e especial de 2026-10-20: Sling (Q1), KingStrike (Q6), MagmaFist (Q7), StaticShock (Q11),
  OgrePunch (Q13), StoneFist (Q17), MudBolt (Q19), Charge (Q22), Javelin (Q26), ArcLightning (Q30), Shriek (Q33),
  FlameBurst (Q36), BlindingGaze (Q38), PlagueBite (Q42), QuillBurst (Q45), AcidSpit (Q48), Tornado (Q50).
- `Icons/Consumables/` de 2026-10-20: ImpactGrenade (R1), SwapScroll (R20), Stone (R28), FireFlask (R46),
  FrostFlask (R55).
- `Icons/Talents/` do novo molde (talentos de regra): Domino (O13), FlameTrail (O15), Momentum (O17),
  Counterstrike (O19), SlipperyIce (O21), SwapStep (O23).
- `Icons/States/`: Burning (B6), Chilled (B7), Exposed (B8), Stunned (B9), Venom (E29), Webbed (I59),
  Frozen (O25), Blinded (O27), Exhausted (Q55, uma bota presa a uma bola de ferro); Armored, LifeSteal, Poisoned, Regeneration, Shield, ThornArmor e Weakened à mão.
- `Enemies/`: GoblinBomber, OgreBrute e WeaverSpider (lote K, recortados da folha) com as animações do lote L.
- `Enemies/` do novo molde (lote M, recortados da folha, com as animações do lote N; o número da opção escolhida
  de cada um não foi registrado): GoblinArtillery, GoblinLancer, LavaGolem, LightningTotem, RamBeetle,
  SkeletonKnight, StoneGolem, SwampWitch, VampireBat e WindShaman. Cada um tem a tira de 8 quadros em
  `Art/Sprites/Enemies/<Nome>.png` (192×30) e o clipe `<Nome>Idle` com o `<Nome>AnimController` em
  `Art/Animations/Enemies/<Nome>/`. Animações do lote N pela descrição na galeria: LavaGolem `1f60b062`,
  WindShaman `29ec6f8e`, SkeletonKnight `ad3b5079`, VampireBat `1774a0d0`, StoneGolem `334686ed`,
  SwampWitch `6bf91957`, LightningTotem `7bb23e5f`, RamBeetle `17a96b71`; `dd1971b3` e `3d99bb1b` são as do
  GoblinArtillery e do GoblinLancer (a descrição truncada na galeria não diz qual é qual).
- `Icons/Roles/`: todos à mão.
- `Icons/Consumables/`: Bomb (B11), HealingPotion (B12), RegenerationTonic (B13).
- `Icons/Map/`: BattleEasy (B14), BattleNormal (B15), BattleHard (B16), Boss (B17), Consumable (B18), Heal (B19),
  Talent (B20). Do novo molde: Elite (O29), Camp (O31). Desde 2026-10-20 (batalhas só normal ou de elite),
  BattleEasy e BattleHard não são usados por nada; os arquivos continuam na pasta.
- `Icons/UI/`: Lock (B21), NewSkill (B22), BuildMatch (B23), TurnMove (B24), TurnAct (B28), TurnItem (B26),
  Tip (B27).
- `Terrain/`: Fire (G16), PoisonBog (G39), WardStone (G45), HealingShrine (F29), Rock (F41), IceWall (P1).
- `UI/`: ItemSlot, ItemSlotPressed e ItemSlotSelected redesenhados com as camadas e cores dos slots quadrados do
  `UI.png` (contorno, aço claro, aço escuro, contorno, sombra, fundo), com o brilho de bisel nos cantos.

## Pendências

- Visuais das skills (`Placeholders/Vfx`): continuam provisórios até a avaliação do Luiz.
- Os inimigos provisórios ainda são a arte de outros inimigos com outra cor (`tint` no `EnemyConfig`): Goblin
  blindado, Guarda Goblin, Goblin arpoador, Rei Goblin (chefe), Xamã goblin, Olho maldito, Rainha dos ratos,
  Slime regenerante e Rato espinhoso.
