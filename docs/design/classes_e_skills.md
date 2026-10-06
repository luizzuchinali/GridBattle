---
tags:
  - design
  - classes
status: em definição (classes e skills implementadas, valores provisórios)
updated: 2026-10-06
---
# Classes e Skills

> Subdocumento do [[GDD]]. Resumo e decisões principais em 3.2, Mecânica 7 e seção 6. As skills das classes vêm dos talentos; os inimigos têm skills exclusivas de cada tipo, descritas em [[inimigos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## 3.2 Personagem do Jogador
Valores atuais das três classes, como estão implementados no jogo (um asset de configuração por classe). Todos os valores são provisórios e serão balanceados.

| Classe | Vida máxima | Alcance de movimento | Alcance de ataque | Dano básico |
|---|---|---|---|---|
| Guerreiro (Knight) | 120 | 1 | 1 | 15 |
| Mago (Mage) | 80 | 1 | 1 | 15 |
| Ladino (Rogue) | 90 | 1 | 1 | 15 |

- Hoje as classes diferem na vida máxima e nas skills: cada uma começa com uma skill (Guerreiro: Golpe; Mago: Nova de Gelo; Ladino: Adaga Envenenada), e as demais skills vêm de talentos (as nove skills da tabela 6.2 existem como assets). Movimento, alcance e dano básico são iguais.
- ❓ Papel e identidade de cada classe e a motivação do personagem. Nada disso está definido ainda, só os números acima. Sugestão a validar: o Guerreiro com mais vida (já é assim), o Mago com skills de área e o Ladino com mais mobilidade ou alcance.

> **Será revisto no futuro.** As regras abaixo descrevem a direção desejada da progressão; os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.
- **Curva de XP e liberação das classes:** a curva de XP implementada está em [[xp_e_niveis]], e as regras de liberação das classes em [[meta_progressao_e_perfil]].

## Skills

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando. As skills detalhadas abaixo representam a direção inicial, e os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.

- **Definição:** uma skill é uma ação ativa do personagem, vinculada à classe ou a traits adquiridos. É reutilizável: após o cooldown pode ser usada de novo, e não é consumida. Toda skill tem:
  - **Nome e descrição**
  - **Tipo:** ofensiva (causa dano), defensiva (escudo, cura ou buff) ou utilitária (teleporte, troca etc.)
  - **Dano:** valor fixo (se ofensiva)
  - **Área de efeito:** a forma geométrica do efeito no grid (shape: CIRCLE, CROSS, LINEAR, CONE, ARC, PERPENDICULAR)
  - **Tamanho da área:** ímpar, de 1 a 9
  - **Alcance:** distância Manhattan máxima para selecionar o alvo
  - **Cooldown:** número de ações do jogador entre usos
  - **Ícone**
- **Sistema de cooldown:** cada skill ativa tem um contador de cooldown que decrementa a cada ação do jogador (movimento ou ataque). Uma skill não pode ser usada enquanto o cooldown não zera.
- **Skills de classe:** cada classe começa com 1-2 skills básicas (ex.: o Guerreiro começa com "Golpe", dano em área frontal). Novas skills são desbloqueadas via traits.
- **Slots de skill:** o personagem carrega até 6 skills ativas por vez, uma por botão da barra de skills (4.2). Em uma run, só 6 talentos que liberam skills podem ser escolhidos. ❓ Se a skill inicial da classe ocupa um dos 6 espaços, e o que acontece quando os 6 já estão ocupados (sugestão: talentos que liberam skills deixam de ser oferecidos). Valores iniciais configuráveis: a skill inicial ocupa um dos 6 espaços, e os talentos que liberam skills deixam de ser oferecidos quando os espaços acabam (ver [[valores_padrao_em_aberto]]).
- **Skills dos inimigos:** os inimigos têm skills exclusivas de cada tipo, descritas em [[inimigos]].
- **Uso (Mecânica 7):** toque no botão da barra de skills (parte inferior da tela, 6 botões; ver 4.2) e depois seleção do alvo.
- **Feedback visual/sonoro:** animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo).

## 6.2 Tabela de Skills por Classe

As nove skills da tabela estão implementadas como assets (`Settings/Skills/<Classe>`), e os valores atuais dos assets estão em [[sistema_combate]].

**Decisão (2026-10-05): toda classe começa com ao menos uma skill.** Golpe (Guerreiro), Nova de Gelo (Mago) e Adaga Envenenada (Ladino) são as skills iniciais. As demais são liberadas pelos talentos da coluna Trait. Criomancia e Mãos Ligeiras, que liberavam as skills que viraram iniciais, passaram a ser talentos de reforço (+15% de dano de skill). O Golpe foi reajustado para dano em área frontal (`PERPENDICULAR` de tamanho 3).

### Guerreiro
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Golpe | OFENSIVA | 10 | PERPENDICULAR | 3 | 1 | 1 | Inicial |
| Investida com Escudo | OFENSIVA | 12 | LINEAR | 3 | 2 | 3 | Muralha |
| Golpe de Escudo (acréscimo de balanceamento) | OFENSIVA | 8 | CIRCLE | 1 | 1 | 3 | Força Bruta |

### Mago
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Bola de Fogo | OFENSIVA | 10 | CIRCLE | 3 | — | 3 | Grimório |
| Nova de Gelo | OFENSIVA | 8 | CIRCLE | 3 | 2 | 2 | Inicial |
| Tempestade Elétrica | OFENSIVA | 14 | CONE | 3 | 3 | 4 | Conjurador de Tempestades |
| Rajada de Vento (acréscimo de balanceamento) | OFENSIVA | 4 | CIRCLE | 3 | 3 | 3 | Domínio do Vento |

### Ladino
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Adaga de Arremesso | OFENSIVA | 6 | CIRCLE | 1 | 3 | 1 | Arsenal Oculto |
| Bomba de Fumaça | OFENSIVA | 5 | CROSS | 1 | 1 | 2 | Mestre das Sombras |
| Adaga Envenenada | OFENSIVA | 7 | LINEAR | 3 | 4 | 2 | Inicial |
| Linha Venenosa | OFENSIVA | 9 | LINEAR | 5 | 5 | 3 | Mestre do Veneno |
| Gancho (acréscimo de balanceamento) | OFENSIVA | 4 | CIRCLE | 1 | 4 | 3 | Gancho de Abordagem |

#### Skills de deslocamento (acréscimo de balanceamento, 2026-10-06, G4)

Decisão do usuário: uma skill dedicada a **empurrar ou puxar** por classe, cada uma liberada por um talento da classe, mais inimigos que também empurram e puxam. Não fazem parte da tabela original do GDD; os valores são provisórios.

| Skill | Efeito de deslocamento | Observação |
|---|---|---|
| Golpe de Escudo (Guerreiro) | empurra 2 células para longe do Guerreiro | alvo adjacente; quem bate num obstáculo, na borda ou em outro personagem leva dano de colisão |
| Rajada de Vento (Mago) | empurra 1 célula para longe do centro da área | área 3×3 a até 3 células; quem está no centro vai para longe do Mago |
| Gancho (Ladino) | puxa até 3 células em direção ao Ladino | para ao lado dele; arraste inimigos para perigos ou contra outros inimigos |

Regras (direção, colisão, reação do terreno, personagens imóveis): [[sistema_combate]], seção "Empurrar e puxar". Os talentos que as liberam (Força Bruta, Domínio do Vento e Gancho de Abordagem) pedem nível 3 e têm peso base 1,5.

## Talentos de classe: identidade (balanceamento G3, 2026-10-06, provisório)

Decisão do usuário: as classes se diferenciam por **talentos que modificam as próprias skills** e por **passivas exclusivas**; os talentos genéricos pesam menos na oferta (ver [[talentos_e_oferta]]). A mecânica está em [[sistema_combate]] ("Valores efetivos e modificadores" e "Bônus condicionais dos estados") e as tabelas completas, em "Valores atuais dos assets > Talentos".

| Classe | Papel | Skills modificáveis | Passivas exclusivas |
|---|---|---|---|
| Guerreiro | brigão da linha de frente | Golpe (dano, largura, empurra 1), Investida com Escudo (recarga, dano, alcance), Golpe de Escudo (empurra mais, expõe, atordoa) | Frenesi Corpo a Corpo (+7% de dano por inimigo adjacente, até 3), Segurar a Linha (−4% de dano recebido por inimigo adjacente), Sede de Sangue (cura ao matar), Estilhaçar (+40% de dano de colisão) |
| Mago | controle de área | Nova de Gelo (congela por 1 turno, recarga, área), Bola de Fogo (dano, queima, área), Tempestade Elétrica (dano, cone), Rajada de Vento (área, empurra mais) | Quebradiço (+20% de dano contra alvos com estado nocivo), Foco Arcano (+15% de dano de skill se não andou), Colheita de Almas (matar encurta os cooldowns) |
| Ladino | assassino e veneno | Adagas (dano, alcance, recarga, linha), Bomba de Fumaça (enfraquece, duração, área), Gancho (puxa mais, atordoa), Linha Venenosa (dano) | Veneno em habilidades e ataques básicos (Lâminas Envenenadas, Arma Envenenada), Depredador (+35% contra envenenados), Bate e Corre (+30% depois de andar), Assassino (+40% contra isolados), Banquete das Sombras (cura ao matar), Veneno Debilitante (−15% de dano de inimigos com estado nocivo) |

- **Ladino mais forte:** antes do G3 o Ladino vencia ~8% das runs do simulador contra ~46% e ~58% das outras classes (mesmas seeds). O que o limitava era a sustentação (90 de vida, golpes de 6 a 9) e as passivas que dependem de situação rara; o veneno que cresce com a vida do inimigo e enfraquece o envenenado, a cura ao matar e a redução de dano contra inimigos com estado nocivo foram o que mais o aproximou das outras classes. Resultados do simulador na etapa: [[plano_balanceamento]].
- **Estados novos** (provisórios): Queimando, Veneno letal, Congelado, Atordoado e Exposto (`Settings/States`), usados pelos talentos e disponíveis para inimigos.
- A parte de conteúdo (nomes, textos, ícones, valores) é provisória e será revista no G6.
