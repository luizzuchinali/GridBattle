---
tags:
  - design
  - estados
status: em definição (atributos básicos implementados; estados não)
updated: 2026-10-03
---
# Atributos e Estados

> Subdocumento do [[GDD]]. Resumo e decisões principais em 3.1. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## 3.1 Atributos e Estados

Duas camadas descrevem e alteram um personagem. Elas formam o vocabulário que talentos, skills e inimigos usam para modificá-lo:

- **Atributos:** os valores base que configuram o personagem no início da run. Cada classe define os seus (ver 3.2). O valor em jogo é o valor base somado aos estados ativos.
- **Estados:** têm um nome e concedem um ou mais efeitos ao personagem, que incrementam ou reduzem atributos ou modificam comportamentos do jogo. A duração (permanente ou X turnos) não é fixa do estado: depende da situação do jogo ou do talento que o aplica.

**Talentos (traits)** concedem estados, e é por meio deles que modificam o personagem e o jogo. Skills e inimigos também aplicam estados.

As configurações da run (opções de talento, rerolls e banimentos) não são atributos de classe; ver Mecânica 3 em 2.3. Estados ativos podem alterá-las.

❓ Quais atributos e estados também valem para os inimigos.

### Atributos

| Categoria | Atributo | Descrição | Situação |
|---|---|---|---|
| Base | Vida máxima | Quantidade máxima de vida. | Existe |
| Base | Alcance de movimento | Distância que o personagem anda por ação. | Existe |
| Base | Alcance de ataque | Distância do ataque básico. | Existe |
| Ofensivo | Dano básico | Dano do ataque básico. | Existe |
| Ofensivo | Chance de crítico | Probabilidade de um ataque causar dano extra. | Novo |
| Ofensivo | Multiplicador de crítico | Quanto o crítico multiplica o dano. | Novo |
| Ofensivo | Penetração de defesa | Ignora parte da defesa do alvo. | Novo |
| Defensivo | Defesa | Reduz o dano recebido. ❓ Valor fixo ou porcentagem. | Novo |
| Skills | Bônus de dano de skills | Aumenta o dano das skills ofensivas. | Novo |
| Skills | Alcance de skills | Aumenta a distância máxima para escolher o alvo. | Novo |
| Skills | Redução de cooldown | Skills voltam a ficar disponíveis mais cedo. ❓ Em turnos ou porcentagem, e mínimo. | Novo |

### Estados

Um estado tem um **nome** e concede um ou mais efeitos. Ao ser aplicado, recebe uma **duração**: permanente ou X turnos. Quem define a duração é a situação do jogo ou o talento que o aplica, então o mesmo estado pode ser temporário num caso e permanente em outro. Exemplo: o estado **Fraquejado** deixa o personagem com 50% menos dano.

| Estado                          | O que faz                                                                           |
| ------------------------------- | ----------------------------------------------------------------------------------- |
| Fraquejado                      | Reduz o dano do personagem em 50%.                                                  |
| Armadura de Espinhos            | Ao ser atacado, devolve X de dano ao atacante.                                      |
| Roubo de vida (nome provisório) | Ao causar dano, recupera uma porcentagem dele como vida.                            |
| Regeneração (nome provisório)   | A cada turno do jogador, recupera vida. ❓ Valor fixo ou porcentagem da vida máxima. |
| Escudo (nome provisório)        | Vida temporária que absorve dano antes da vida real. ❓ Como é obtido e se expira.   |
| Envenenado (nome provisório)    | Causa dano ao longo dos turnos.                                                     |

A lista final de estados, seus nomes e valores são ❓ a definir.

**Turno global e turno das entidades.** Existe o **turno global**. Em cada turno global o jogador joga primeiro e depois jogam os inimigos. O turno global termina depois que a última entidade do grid faz sua ação. Cada entidade (jogador ou inimigo) tem o seu próprio turno dentro do turno global.

**Duração.** A duração de um estado é contada em turnos da **entidade que está com o estado**, seja um buff ou um debuff, e não importa quem o aplicou. Um estado de duração 2 dura 2 turnos da entidade portadora: a duração diminui ao fim de cada turno dela e o estado termina quando chega a zero. O turno da portadora conta como o primeiro se ela ainda não terminou o turno dela no turno global atual; caso contrário, o primeiro é o próximo turno dela.

Exemplo com um buff de 2 turnos que o jogador aplica em si mesmo:

```
Turno global 1: Jogador usa o buff (1º turno do buff, ao fim da jogada: duração 2 → 1)
                Inimigos jogam (buff ativo)
Turno global 2: Jogador joga (2º turno do buff, ao fim da jogada: duração 1 → 0, o buff termina)
                Inimigos jogam (sem buff)
Turno global 3: Jogador joga (sem buff)
```

Exemplo com Fraquejado de 2 turnos aplicado pelo jogador em um inimigo (o turno do inimigo ainda não aconteceu no turno global 1, então conta como o primeiro):

```
Turno global 1: Jogador aplica Fraquejado (duração 2) em um inimigo
                O inimigo joga, com Fraquejado (1º turno, duração 2 → 1)
Turno global 2: Jogador joga
                O inimigo joga, com Fraquejado (2º turno, duração 1 → 0, termina)
Turno global 3: Jogador joga
                O inimigo joga, sem Fraquejado
```

Exemplo inverso: um inimigo aplica um debuff de 2 turnos no jogador durante a jogada dele no turno global 1. O turno do jogador nesse turno global já terminou, então a contagem começa no turno 2: o debuff afeta as jogadas do jogador nos turnos globais 2 e 3.

❓ Reaplicação e acúmulo do mesmo estado, remoção antecipada, e o que acontece com os estados ao mudar de batalha.

Fora do conjunto por enquanto: esquiva e tamanho da área das skills.
