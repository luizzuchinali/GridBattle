---
tags:
  - design
  - classes
status: em definição (classes implementadas, sem skills)
updated: 2026-10-03
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

- Hoje as classes diferem só na vida máxima. Movimento, alcance e dano básico são iguais, e nenhuma tem skill configurada (a lista de skills de cada classe em 6.2 é a direção de design, ainda não implementada).
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
- **Slots de skill:** o personagem carrega até 6 skills ativas por vez, uma por botão da barra de skills (4.2). Em uma run, só 6 talentos que liberam skills podem ser escolhidos. ❓ Se a skill inicial da classe ocupa um dos 6 espaços, e o que acontece quando os 6 já estão ocupados (sugestão: talentos que liberam skills deixam de ser oferecidos).
- **Skills dos inimigos:** os inimigos têm skills exclusivas de cada tipo, descritas em [[inimigos]].
- **Uso (Mecânica 7):** toque no botão da barra de skills (parte inferior da tela, 6 botões; ver 4.2) e depois seleção do alvo.
- **Feedback visual/sonoro:** animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo).

## 6.2 Tabela de Skills por Classe

### Guerreiro
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Golpe | OFENSIVA | 5 | CIRCLE | 1 | 1 | 1 | Inicial |
| Investida com Escudo | OFENSIVA | 12 | LINEAR | 3 | 2 | 3 | Muralha |

### Mago
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Bola de Fogo | OFENSIVA | 10 | CIRCLE | 3 | — | 3 | Grimório |
| Nova de Gelo | OFENSIVA | 8 | CIRCLE | 3 | 2 | 2 | Criomancia |
| Tempestade Elétrica | OFENSIVA | 14 | CONE | 3 | 3 | 4 | Conjurador de Tempestades |

### Ladino
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Adaga de Arremesso | OFENSIVA | 6 | CIRCLE | 1 | 3 | 1 | Arsenal Oculto |
| Bomba de Fumaça | OFENSIVA | 5 | CROSS | 1 | 1 | 2 | Mestre das Sombras |
| Adaga Envenenada | OFENSIVA | 7 | LINEAR | 3 | 4 | 2 | Mãos Ligeiras |
| Linha Venenosa | OFENSIVA | 9 | LINEAR | 5 | 5 | 3 | Mestre do Veneno |
