---
tags:
  - design
  - classes
  - trait
created: 2026-07-12
aliases:
  - Catálogo de Traits
---

# Traits por Classe

Catálogo de todos os traits disponíveis, organizados por classe. Cada trait
pode ter até 3 efeitos: modificadores de stats (buffs/debuffs), desbloqueio de
itens na pool de drop e prerequisitos (outros traits que precisam ser
adquiridos antes).

> O fluxo de aquisição é orientado a **escolha obrigatória no level-up**: a
> cada nível, o jogador escolhe 1 entre 3 traits sorteados da pool da classe.
> Traits no rank máximo não reaparecem no sorteio.

Ver também: [[talentos]], [[ideias_traits_itens]], [[plano_progressao]].

## Guerreiro

Classe resistente e direta: muita vida e dano consistente.
- **Base:** ATK 5 · HP 16 · Crit 5% · Mult 2.0

| Trait | Ranks | Nível | Pré-req | Efeito |
|---|---|---|---|---|
| Pele de Pedra | 2 | 1 | — | +8 HP máx por rank |
| Força Bruta | 2 | 1 | — | +2 ATK por rank |
| Fúria | 1 | 4 | Força Bruta | +4 ATK, −8 HP máx |
| Alquimia de Guerra | 1 | 3 | — | Libera **Poção de Vigor** na pool de drop |
| Brado de Guerra | 1 | 2 | — | Libera **Grito de Guerra** na pool de drop |
| Muralha | 2 | 2 | Pele de Pedra | +4 HP máx por rank, libera **Investida com Escudo** |
| Sede de Sangue | 1 | 3 | — | +3 ATK, −4 HP máx |
| Fúria Sangrenta | 2 | 3 | Força Bruta | +1 ATK por rank, libera **Ritual de Sangue** |
| Vigilância | 2 | 2 | — | +6% drop e +4% crit por rank |
| Berserker | 2 | 4 | — | +3 ATK, −2 HP máx por rank |

### Itens liberados pelo Guerreiro

| Item | Trait | Efeito |
|---|---|---|
| Poção de Vigor | Alquimia de Guerra | Restaura 15 de vida (nível mín. 3) |
| Grito de Guerra | Brado de Guerra | Restaura 8 de vida (nível mín. 2) |
| Investida com Escudo | Muralha | Dano 12 em linha (size 3, alcance 2, nível mín. 4) |
| Ritual de Sangue | Fúria Sangrenta | Restaura 20 de vida (nível mín. 3) |

## Mago

Classe frágil, mas com golpes fortes e críticos frequentes.
- **Base:** ATK 6 · HP 10 · Crit 10% · Mult 2.0

| Trait | Ranks | Nível | Pré-req | Efeito |
|---|---|---|---|---|
| Mente Arcana | 1 | 1 | — | +5% de chance de crítico |
| Foco | 1 | 1 | — | +2 de ataque |
| Grimório | 1 | 3 | — | Libera **Bola de Fogo** na pool de drop |
| Pacto Sombrio | 1 | 4 | Mente Arcana | +0.5 de multiplicador de crítico, −6 HP máx |
| Criomancia | 1 | 2 | — | Libera **Nova de Gelo** na pool de drop |
| Barreira Arcana | 1 | 3 | — | Libera **Escudo Arcano** na pool de drop |
| Maestria Elemental | 2 | 4 | Foco | +0.3 de multiplicador de crítico por rank |
| Sobrecarga | 2 | 2 | — | +8% de chance de crítico por rank |
| Fluxo Arcano | 2 | 2 | — | +7% de chance de drop por rank |
| Conjurador de Tempestades | 1 | 4 | Grimório | Libera **Tempestade Elétrica** na pool de drop |

### Itens liberados pelo Mago

| Item | Trait | Efeito |
|---|---|---|
| Bola de Fogo | Grimório | Dano 10 em área (CIRCLE size 3, nível mín. 3) |
| Nova de Gelo | Criomancia | Dano 8 em área (CIRCLE size 3, alcance 2, nível mín. 2) |
| Escudo Arcano | Barreira Arcana | Restaura 12 de vida (nível mín. 3) |
| Tempestade Elétrica | Conjurador de Tempestades | Dano 14 em cone (CONE size 3, alcance 3, nível mín. 4) |

## Ladino

Classe de crítico e sorte: dano explosivo e mais itens.
- **Base:** ATK 4 · HP 12 · Crit 25% · Mult 2.2

| Trait | Ranks | Nível | Pré-req | Efeito |
|---|---|---|---|---|
| Lâmina Afiada | 2 | 1 | — | +10% de chance de crítico por rank |
| Golpe Duplo | 1 | 4 | Lâmina Afiada | +0.5 de multiplicador de crítico |
| Arsenal Oculto | 1 | 3 | — | Libera **Adaga de Arremesso** na pool de drop |
| Sorte de Ladrão | 1 | 1 | — | +8% de chance de drop de itens |
| Mestre das Sombras | 1 | 2 | — | Libera **Bomba de Fumaça** na pool de drop |
| Mãos Ligeiras | 1 | 3 | — | Libera **Adaga Envenenada** na pool de drop |
| Evasão | 2 | 2 | — | +6 HP máx e +5% de chance de drop por rank |
| Acerto Mortal | 2 | 3 | Lâmina Afiada | +0.4 de multiplicador de crítico por rank |
| Fortuna | 2 | 2 | — | +10% de chance de drop por rank |
| Mestre do Veneno | 1 | 4 | Arsenal Oculto | Libera **Linha Venenosa** na pool de drop |

### Itens liberados pelo Ladino

| Item | Trait | Efeito |
|---|---|---|
| Adaga de Arremesso | Arsenal Oculto | Dano 6 à distância (size 1, alcance 3, nível mín. 3) |
| Bomba de Fumaça | Mestre das Sombras | Dano 5 em cruz (CROSS size 1, alcance 1, nível mín. 2) |
| Adaga Envenenada | Mãos Ligeiras | Dano 7 em linha (LINEAR size 3, alcance 4, nível mín. 3) |
| Linha Venenosa | Mestre do Veneno | Dano 9 em linha (LINEAR size 5, alcance 5, nível mín. 4) |