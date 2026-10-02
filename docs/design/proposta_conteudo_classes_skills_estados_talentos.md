---
tags:
  - design
  - proposta
  - revisar
created: 2026-10-01
status: proposta para revisão
aliases:
  - Proposta de conteúdo
  - Análise de classes, estados e talentos
---

# Proposta de conteúdo: classes, skills, estados, talentos, inimigos e ondas

> **Status: rascunho para revisão. Nada aqui está decidido.** Este arquivo reúne análise e propostas feitas enquanto o autor estava ausente. O [[GDD]] não foi alterado por este material. Os ❓ pendentes do GDD (reaplicação e acúmulo de estados, remoção antecipada, estados ao mudar de onda, atributos nos inimigos) **não foram resolvidos**: a seção 12 só traz sugestões para discutir quando o autor voltar.

Relacionados: [[GDD]], [[sistema_combate]], [[talentos]], [[ideias_traits_itens]].

## 0. Como ler

- As **premissas** que sustentam as contas estão na seção 2. Os números de balanceamento são pontos de partida para teste, não valores finais.
- Os valores de projeto usados nas contas (vida, ataque, XP dos inimigos, curva de XP) vêm de [[sistema_combate]], que reflete o código atual.
- Nomes de estados, skills e talentos são **provisórios**.
- Quando uma proposta contradiz algo do GDD, está sinalizado como **conflito**.

## 1. Achados principais

1. **Skills do GDD estão na escala antiga.** A tabela da seção 6 do GDD usa dano 5 a 14, de uma época em que o ataque básico era 4 a 6 e a vida era 10 a 16. Hoje o ataque básico é 15 e a vida vai de 80 a 120. Convertendo para % do dano básico, as skills ficam entre 100% e 240%. **Proposta:** definir dano de skill como multiplicador do dano básico, o que também faz o atributo "Bônus de dano de skills" funcionar de forma natural.
2. **O Golpe do Guerreiro é redundante.** Pela conversão, o Golpe vale 100% do ataque básico, com área 1 e cooldown 1. Ele não faz nada que o ataque básico já não faça. Ver proposta na seção 6.
3. **Defesa fixa quebra com os danos atuais.** Com inimigos dando 5 a 7, uma defesa fixa de 3 reduz 60% (dano 5) e 43% (dano 7), mas só 15% quando o dano inimigo chega a 20. **Proposta:** defesa percentual com retorno decrescente, `reducao = d / (d + 100)`, e dano mínimo de 1.
4. **Ondas por orçamento puro geram poucos inimigos.** Se os atributos dos inimigos escalam por onda e o orçamento de ameaça cresce linearmente, o número de inimigos fica entre 3 e 5, pouco para um grid 6×6 e para builds de área. Uma variante em que a **contagem** cresce até um teto (12) e os atributos escalam mais devagar produz uma run mais tática (seção 2.4).
5. **O limite de 50 turnos é, na prática, uma checagem de dano.** Na variante recomendada, limpar a onda em ~35 turnos exige cerca de 15 de dano por turno na onda 10 e 31 na onda 20, contra os 15 do ataque básico. Ou seja, por volta da onda 20 a build precisa de aproximadamente o dobro do dano base, ou de área. Isso é desejável para o loop de build, mas precisa estar nos talentos.
6. **A vida precisa escalar junto.** Com 3 atacantes, o dano recebido por turno vai de ~16 (onda 1) a ~28 (onda 20) e ~41 (onda 40), contra 80 a 120 de vida. Sem talentos defensivos ou de vida, a maior parte das runs acaba antes da onda 20.
7. **Tamanho das árvores.** Na variante recomendada, uma run chega a 13 talentos na onda 20, 19 na onda 30 e 25 na onda 40, com um nível a cada 1,4 a 1,7 ondas. Com 3 ofertas por nível e rerolls, vale ter **pelo menos 35 a 40 talentos por classe** (incluindo compartilhados) para a run não repetir ofertas.
8. **Métrica de distância diverge entre GDD e código.** O GDD diz que o alcance de skills é distância Manhattan. O código mede distância euclidiana. Até o alcance 2 o resultado é o mesmo (12 células), mas no alcance 3 são 28 células (euclidiana) contra 24 (Manhattan). Skills com alcance 3 a 5 precisam de uma regra única.
9. **Alcance 2 vale muito.** Atacar a distância 2 alcança 12 células, contra 4 de um atacante corpo a corpo. Se o Mago ataca no alcance 2 com o mesmo dano do Guerreiro, ele domina. Proposta: dano básico menor para o Mago (seção 5).
10. **Falta definir "passar a vez".** Hoje toda ação válida consome o turno. Não existe uma ação de esperar. Ela importa para posicionamento, para estados com duração e para o anti-stall. É uma decisão em aberto (seção 12).
11. **Telegrafia.** Em Into the Breach, os desenvolvedores dizem que usaram ataques telegrafados para que "toda morte pareça culpa do jogador", e que um inimigo a mais pode transformar um desafio divertido em impossível. Vale telegrafar ataques de elites e chefes e calibrar contagens com cuidado.

## 2. Números de base

### 2.1 Premissas

- Valores reais do projeto: Knight 120 de vida, Mage 80, Rogue 90, todos com ataque básico 15. Inimigos: Goblin 30/5, Rat 20/5 (anda 2), Slime 40/5, FireSkull 25/7, EyeBat 25/5 (anda 2), com XP 10, 8, 12, 15 e 8.
- Curva de XP: `XP para o próximo nível = 50 + (nível − 1) × 25`.
- **Ameaça** de um inimigo (métrica proposta): `dano × vida / 50 × (1 + 0,25 × (movimento − 1))`. Goblin 3,0; Rat 2,5; Slime 4,0; FireSkull 3,5; EyeBat 3,1. O XP já concedido fica perto de 3,3 por ponto de ameaça, o que valida a métrica.
- A simulação usa uma **composição média** de inimigos, não ondas reais. Ela ignora a redução do dano recebido conforme os inimigos morrem, o tempo de aproximação e as skills do jogador. A eficiência de 60% é um chute. É um guia de ordem de grandeza, não substitui teste.

### 2.2 Curva de XP

| Nível | XP acumulado | XP para o próximo |
|---|---|---|
| 2 | 50 | 75 |
| 5 | 350 | 150 |
| 10 | 1.350 | 275 |
| 15 | 2.975 | 400 |
| 20 | 5.225 | 525 |
| 30 | 11.600 | 775 |

### 2.3 Pressão quando o jogador está cercado

Com dano inimigo de 5 por ataque e todos os atacantes acertando:

| Classe | 2 atacantes | 3 atacantes | 4 atacantes |
|---|---|---|---|
| Guerreiro (120) | 12,0 turnos | 8,0 turnos | 6,0 turnos |
| Mago (80) | 8,0 turnos | 5,3 turnos | 4,0 turnos |
| Ladino (90) | 9,0 turnos | 6,0 turnos | 4,5 turnos |

Com o FireSkull (dano 7), o tempo cai para 4,3 turnos (Guerreiro) e 2,9 (Mago) com 4 atacantes. O level up cura 100%, então a pressão se resolve em picos: o jogador sobrevive até o próximo nível.

### 2.4 Duas variantes de geração de ondas

**Variante A: orçamento puro.** Orçamento `6 + 2,5 × onda`, atributos dos inimigos +6% de vida e +4% de dano por onda, teto de 14 inimigos.

| Onda | Inimigos | Nível do jogador | Talentos acumulados | Dano/turno para limpar em 40 turnos |
|---|---|---|---|---|
| 1 | 3 | 1 | 0 | 3,5 |
| 10 | 5 | 5 | 4 | 9,0 |
| 20 | 5 | 9 | 8 | 12,5 |
| 30 | 4 | 12 | 11 | 12,8 |

Resultado: poucos inimigos (3 a 5) e um nível a cada 2,2 a 5 ondas, ritmo que desacelera demais para um loop de build.

**Variante B (recomendada): contagem cresce, atributos escalam devagar.** Inimigos `min(12, 3 + onda/2)`, +5% de vida e +4% de dano por onda, XP por inimigo escalado com a vida. Dano necessário calculado para limpar em 35 turnos com 60% de eficiência.

| Onda | Inimigos | x Vida | x Dano | Nível | Vida total | Dano/turno necessário | Dano recebido (3 atacam) |
|---|---|---|---|---|---|---|---|
| 1 | 3 | 1,00 | 1,00 | 1 | 84 | 4,0 | 16 |
| 5 | 5 | 1,20 | 1,16 | 4 | 168 | 8,0 | 19 |
| 10 | 8 | 1,45 | 1,36 | 7 | 325 | 15,5 | 22 |
| 15 | 10 | 1,70 | 1,56 | 10 | 476 | 22,7 | 25 |
| 20 | 12 | 1,95 | 1,76 | 14 | 655 | 31,2 | 29 |
| 30 | 12 | 2,45 | 2,16 | 20 | 823 | 39,2 | 35 |
| 40 | 12 | 2,95 | 2,56 | 26 | 991 | 47,2 | 42 |

Talentos acumulados na variante B: 6 (onda 10), 13 (onda 20), 19 (onda 30), 25 (onda 40). Um nível a cada 1,4 a 1,7 ondas.

## 3. Gramática de efeitos e tags de sinergia

### 3.1 Gramática: gatilho, condição, ação

Para que talentos e estados sejam dados (e não código novo a cada talento), vale descrevê-los com uma gramática simples:

| Elemento | Opções |
|---|---|
| **Gatilho** | início do turno do portador; fim do turno do portador; ao causar dano; ao receber dano; ao matar; ao morrer; ao subir de nível; ao iniciar a onda; ao usar skill; ao aplicar um estado |
| **Condição** (opcional) | vida abaixo/acima de X%; alvo com estado Y; primeiro ataque da onda; alvo é elite ou chefe; entidade já agiu na onda |
| **Ação** | modificar atributo (+/−, % ou fixo); curar; causar dano; aplicar estado; remover estado; gerar escudo; deslocar entidade; alterar cooldown; conceder reroll ou opção de talento |

Exemplos: Espinhos = (ao receber dano) → causar dano ao atacante. Roubo de vida = (ao causar dano) → curar uma fração. Regeneração = (início do turno) → curar. Fúria = (vida abaixo de 50%) → aplicar estado Fúria.

### 3.2 Tags de sinergia

Cada talento, skill e estado carrega tags. O sorteio usa as tags para ponderar as ofertas.

`#dano` `#crítico` `#execução` `#cura` `#roubo_de_vida` `#defesa` `#escudo` `#espinhos` `#controle` `#mobilidade` `#área` `#distância` `#cooldown` `#fogo` `#gelo` `#raio` `#veneno` `#marcado`

### 3.3 Fórmula de peso proposta (para a oferta de talentos)

```
peso(talento) = 1
              + 0,75 × min(4, tags em comum com os talentos já escolhidos)
              + 1,50 se for o próximo passo de um ramo já iniciado
              × 0,40 se for talento "morto" para a build (nenhuma tag em comum
                      e a build já tem 5+ talentos)
```

- Pelo menos 1 das 3 ofertas deve ter alguma tag em comum com a build (o GDD já fala em peso por sinergia).
- Talentos de **chave de ramo** só entram na oferta depois dos pré-requisitos.
- A oferta usa o fluxo de seed dos talentos (nível + tentativa de reroll), independente do fluxo das ondas.
- Detalhes como saco embaralhado e proteção contra azar seguem ❓ (já estão pendentes no GDD).

## 4. Estados

### 4.1 Convenções propostas

**Quando atua.** Estados de dano e cura ao longo do tempo (DoT e HoT) causam efeito **no início do turno do portador**, antes de ele agir. A duração, pela regra do GDD, diminui ao fim de cada turno do portador. Assim, um veneno de 3 turnos causa 3 ticks.

**Acúmulo (sugestão, ❓ do GDD).** Dois modelos existem em jogos comparáveis:

- **Duração:** reaplicar renova o tempo e não aumenta a força. É a regra do Vulnerable em Slay the Spire (+50% de dano de ataques, com acúmulo só de duração).
- **Intensidade:** cada aplicação soma e expira por conta própria. Guild Wars 2 usa isso para condições como veneno e sangramento.

Proposta de política padrão: **duração** para a maioria dos estados (reaplicação renova, mantendo a maior duração restante) e **intensidade com teto** só para veneno e sangramento.

**Controle.** Para evitar travar chefes e o jogador, estados de controle (atordoado, enraizado) seguem dois limites propostos: depois de terminar, o portador fica **imune ao mesmo controle por 2 turnos dele**, e chefes sofrem **metade da duração**.

**Contagem.** Todos seguem a regra de duração do GDD (turnos do portador, sem referência a quem aplicou).

### 4.2 Catálogo proposto

| Estado | Efeito | Tipo | Acúmulo | Tags | Exemplos de origem |
|---|---|---|---|---|---|
| Fraquejado | Dano causado −50% | Debuff | Duração | `#controle` | Grito de Guerra, Quebra-escudo |
| Vulnerável | Dano recebido por ataques +50% | Debuff | Duração | `#dano` | Marcar presa, Quebra-gelo |
| Quebrado | Defesa −20% (em valor percentual) | Debuff | Duração | `#defesa` | Quebra-escudo |
| Lento | Alcance de movimento −1 (mínimo 0) | Debuff | Duração | `#gelo` `#controle` | Nova de Gelo, Geada |
| Enraizado | Não anda, ainda ataca | Controle | Duração | `#gelo` `#controle` | Congelar, teia da Aranha |
| Atordoado | Perde a ação do turno | Controle | Não acumula | `#raio` `#controle` | Tempestade Elétrica, Golpe Sísmico |
| Queimado | Dano fixo no início do turno | DoT | Duração | `#fogo` | Bola de Fogo |
| Envenenado | Dano no início do turno por pilha | DoT | Intensidade (teto 5) | `#veneno` | Adaga Envenenada |
| Sangrando | Dano ao andar (por célula) | DoT | Duração | `#dano` | Lâminas |
| Marcado | Próximo ataque do marcador causa crítico | Debuff | Não acumula | `#marcado` `#crítico` | Marca da Presa |
| Provocado | Deve atacar quem provocou, se puder | Controle | Não acumula | `#controle` | Provocar |
| Cegado | Alcance de ataque e de skills −1 | Debuff | Duração | `#controle` | Bomba de Fumaça |
| Silenciado | Não pode usar skills | Controle | Não acumula | `#controle` | Bruxa, Necromante |
| Protegido | Escudo que absorve X de dano | Buff | Soma até o teto | `#escudo` | Postura de Ferro, Barreira Arcana |
| Espinhos | Devolve parte do dano recebido ao atacante | Buff | Duração | `#espinhos` `#defesa` | Armadura de Espinhos |
| Regeneração | Cura no início do turno | HoT | Duração | `#cura` | Tenacidade |
| Fúria | Dano causado +30%, defesa −20% | Buff | Duração | `#dano` | Fúria |
| Acelerado | Alcance de movimento +1 | Buff | Duração | `#mobilidade` | Sombra Veloz |
| Concentrado | Próxima skill: dano +50% | Buff | Não acumula | `#skill` | Concentração |
| Furtivo | Próximo ataque causa crítico garantido | Buff | Não acumula | `#crítico` `#mobilidade` | Passo Sombrio |

Valores são sugestões. Escudo, regeneração e espinhos devem usar % da vida máxima ou do dano recebido (e não valores fixos) para acompanhar a escala.

### 4.3 Afixos de elite como estados permanentes

Se estados já permitem duração permanente, afixos de elite não precisam de sistema novo: são estados aplicados ao inimigo no spawn. Detalhes na seção 8.3.

### 4.4 Referências

- Slay the Spire, Vulnerable: +50% de dano de ataques, acúmulo só de duração, dano arredondado para baixo.
- Guild Wars 2: efeitos acumulam por **duração** (ficam mais longos) ou por **intensidade** (ficam mais fortes, cada instância expira sozinha).

## 5. Classes (proposta para a 3.2 do GDD)

| | Guerreiro | Mago | Ladino |
|---|---|---|---|
| Papel | Linha de frente, resistente | Frágil, dano a distância e em área | Crítico, mobilidade, dano explosivo |
| Vida máxima | 120 | 80 | 90 |
| Dano básico | 15 | **12** | 15 |
| Alcance de movimento | 1 | 1 | 2 |
| Alcance de ataque | 1 | 2 | 1 |
| Chance de crítico | 5% | 10% | 25% |
| Multiplicador de crítico | 2,0 | 2,0 | 2,2 |
| Defesa | 10 (≈9%) | 0 | 0 |
| Skill inicial | Golpe (reformulado, seção 6) | Faísca | Adaga de Arremesso |
| Passiva (estado permanente) | **Pele de Ferro:** −10% de dano recebido | **Mente Afiada:** skills ofensivas +15% de dano | **Oportunista:** +50% de dano contra alvos com estado negativo |

Justificativas:

- **Mago com dano básico 12.** O alcance 2 já dá 12 células de alcance. Com o mesmo dano do Guerreiro, ele domina sem custo. O dano 12 compensa, e a passiva de skills +15% empurra a classe para as skills em vez do ataque básico.
- **Ladino com movimento 2.** Com movimento 2 e distância euclidiana ele alcança 12 células, contra 4 do Guerreiro. É uma vantagem grande de kiting. Os inimigos Rat e EyeBat também andam 2, o que limita o efeito em parte das ondas. Se pesar demais, o ajuste é reduzir o movimento para 1 e dar o bônus como passiva condicional.
- **Passivas como estados permanentes.** Reaproveitam o modelo de estados do GDD e são fáceis de balancear por dados.
- **Defesa do Guerreiro.** Ela só faz sentido com a fórmula percentual da seção 1.

## 6. Skills

### 6.1 Conversão das skills do GDD para % do dano básico

| Skill | Dano no GDD | ATK da época | % do básico | Com básico 15 |
|---|---|---|---|---|
| Golpe | 5 | 5 | 100% | 15 |
| Investida com Escudo | 12 | 5 | 240% | 36 |
| Bola de Fogo | 10 | 6 | 167% | 25 |
| Nova de Gelo | 8 | 6 | 133% | 20 |
| Tempestade Elétrica | 14 | 6 | 233% | 35 |
| Adaga de Arremesso | 6 | 4 | 150% | 22 |
| Bomba de Fumaça | 5 | 4 | 125% | 19 |
| Adaga Envenenada | 7 | 4 | 175% | 26 |
| Linha Venenosa | 9 | 4 | 225% | 34 |

### 6.2 Catálogo proposto

Dano em % do dano básico da classe. Cooldown em turnos do jogador. Alcance 0 significa centrada no próprio personagem. Skills listadas em itálico já existem no GDD.

**Guerreiro**

| Skill | Tipo | Dano | Área | Tam. | Alcance | CD | Estado aplicado | Origem |
|---|---|---|---|---|---|---|---|---|
| Golpe Largo (reformulado) | Ofensiva | 100% | PERPENDICULAR | 3 | 1 | 2 | — | Inicial |
| *Investida com Escudo* | Ofensiva | 240% | LINEAR | 3 | 2 | 4 | Atordoado (1) | Muralha |
| Grito de Guerra | Utilitária | 0% | CIRCLE | 3 | 0 | 5 | Fraquejado (2) nos inimigos | Comandante |
| Postura de Ferro | Defensiva | — | Si mesmo | 1 | 0 | 5 | Protegido (escudo de 25% da vida máx., 2 turnos) | Escudo Inabalável |
| Golpe Sísmico | Ofensiva | 150% | CROSS | 3 | 0 | 5 | Atordoado (1) | Berserker |

**Mago**

| Skill | Tipo | Dano | Área | Tam. | Alcance | CD | Estado aplicado | Origem |
|---|---|---|---|---|---|---|---|---|
| Faísca | Ofensiva | 130% | Alvo único | 1 | 3 | 1 | — | Inicial |
| *Bola de Fogo* | Ofensiva | 167% | CIRCLE | 3 | 3 | 3 | Queimado (2) | Grimório |
| *Nova de Gelo* | Ofensiva | 133% | CIRCLE | 3 | 2 | 3 | Lento (2) | Criomancia |
| *Tempestade Elétrica* | Ofensiva | 233% | CONE | 3 | 3 | 4 | Atordoado (1) em crítico | Conjurador de Tempestades |
| Raio Encadeado | Ofensiva | 140% | Salta entre alvos | 3 saltos | 3 | 3 | — | Conjurador de Tempestades |
| Barreira Arcana | Defensiva | — | Si mesmo | 1 | 0 | 5 | Protegido (escudo de 20% da vida máx., 3 turnos) | Pele de Gelo |

**Ladino**

| Skill | Tipo | Dano | Área | Tam. | Alcance | CD | Estado aplicado | Origem |
|---|---|---|---|---|---|---|---|---|
| *Adaga de Arremesso* | Ofensiva | 150% | Alvo único | 1 | 3 | 2 | — | Inicial |
| *Bomba de Fumaça* | Ofensiva | 125% | CROSS | 1 | 1 | 3 | Cegado (2) | Mestre das Sombras |
| *Adaga Envenenada* | Ofensiva | 175% | LINEAR | 3 | 4 | 2 | Envenenado (3 turnos, 1 pilha) | Mãos Ligeiras |
| *Linha Venenosa* | Ofensiva | 225% | LINEAR | 5 | 5 | 3 | Envenenado (3 turnos, 2 pilhas) | Mestre do Veneno |
| Passo Sombrio | Utilitária | — | Teleporte | — | 3 | 4 | Furtivo (1) | Mestre das Sombras |
| Golpe Fatal | Ofensiva | 200% (400% abaixo de 30% de vida do alvo) | Alvo único | 1 | 1 | 3 | — | Executor (compartilhado) |

Notas:

- **Golpe Largo.** Atinge o alvo e os dois vizinhos laterais ao alvo. Deixa de ser igual ao ataque básico (conflito com o GDD, que o define como CIRCLE de tamanho 1 com 100%).
- **Tempestade Elétrica.** O atordoamento só em crítico liga a skill aos talentos de crítico do Mago e mantém o RNG controlado por build.
- **Conflito com o GDD.** Faísca, Adaga de Arremesso e Golpe Largo viram skills iniciais. No GDD, as duas últimas dependem de traits (Arsenal Oculto) ou são tratadas diferente.
- **Métrica de alcance.** Alinhar skills de alcance 3 ou mais com uma só regra (Manhattan ou euclidiana).
- **Deslocamento forçado (opcional).** Empurrar ou puxar inimigos, como em Into the Breach, daria o próximo nível de profundidade tática à grade. É uma ideia para depois, não faz parte do catálogo acima.

## 7. Talentos

### 7.1 Estrutura proposta

- Cada classe tem **3 ramos de 5 talentos** (15), mais **5 talentos avulsos** da classe, mais os **talentos compartilhados** (seção 7.5). Isso dá de 38 a 39 talentos por classe, e cerca de 48 a 49 ofertas distintas quando se contam os talentos com mais de um nível. O alvo da seção 1 (35 a 40) fica atendido.
- Cada ramo segue a mesma forma: **1** entrada (muitas vezes a skill do ramo), **2 e 3** reforços, **4** talento de transição, **5** talento-chave (muda a forma de jogar). Os nomes dos ramos reaproveitam as *traits* do GDD (Muralha, Grimório, Criomancia, Conjurador de Tempestades, Mãos Ligeiras, Mestre do Veneno, Mestre das Sombras, Arsenal Oculto), então nada do que já está escrito se perde.
- O talento-chave só entra na oferta depois de pelo menos 3 talentos do ramo. A cada talento do ramo escolhido, o seguinte ganha o bônus de "próximo passo" da seção 3.3.
- Talentos marcados com **×N** podem ser escolhidos até N vezes (o nível seguinte só é ofertado depois do anterior). É a forma mais barata de aumentar a variedade.
- Valores são pontos de partida. Dano de skill sempre em % do dano básico (seção 6).
- Duas skills novas aparecem só nos talentos (Passo Arcano, do Mago, e Chuva de Lâminas, do Ladino) e passam a fazer parte do catálogo da seção 6.2 se a proposta for aceita.

### 7.2 Guerreiro

**Ramo Muralha** (defesa, escudo, controle)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Couraça | Defesa +10 | `#defesa` |
| 2 | Investida com Escudo | Desbloqueia a skill (requer Couraça) | `#controle` `#mobilidade` |
| 3 | Escudo Inabalável | Desbloqueia a skill Postura de Ferro | `#escudo` |
| 4 | Bastião | Enquanto estiver com Protegido: dano recebido −20% e inimigos adjacentes ficam Provocados | `#escudo` `#controle` |
| 5 | **Muralha** (chave) | Escudo seu que expira intacto cura 15% da vida máxima. Postura de Ferro dura +1 turno | `#escudo` `#cura` |

**Ramo Berserker** (dano, vida baixa, execução)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Sede de Sangue | Roubo de vida de 8% do dano causado | `#roubo_de_vida` |
| 2 | Fúria | Com vida abaixo de 50%, recebe o estado Fúria (dano +30%, defesa −20%) | `#dano` |
| 3 | Golpe Sísmico | Desbloqueia a skill | `#área` `#controle` |
| 4 | Dor é Poder | +1% de dano a cada 2% de vida perdida (máximo +30%) | `#dano` |
| 5 | **Berserker** (chave) | Ao matar em Fúria: cura 10% da vida máxima e −1 de cooldown em todas as skills | `#execução` `#cura` |

**Ramo Comandante** (controle e debuffs)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Grito de Guerra | Desbloqueia a skill | `#controle` |
| 2 | Voz de Comando | Grito de Guerra: +1 turno de duração e área de tamanho 5 | `#controle` `#área` |
| 3 | Presença Intimidante | Inimigos que terminam o turno adjacentes a você ficam Fraquejados (1) | `#controle` |
| 4 | Tática de Choque | +5% de dano por inimigo adjacente com estado negativo (máximo +20%) | `#dano` `#controle` |
| 5 | **Comandante** (chave) | Grito de Guerra também aplica Vulnerável (2). O cooldown do Grito cai 1 por inimigo morto | `#controle` `#cooldown` |

**Avulsos do Guerreiro**

| Talento | Efeito | Tags |
|---|---|---|
| Espinhos | Estado permanente: devolve 15% do dano recebido (antes da defesa) ao atacante | `#espinhos` `#defesa` |
| Tenacidade | Regeneração: cura 3% da vida máxima no início do turno | `#cura` |
| Último Reduto | Uma vez por run, ao receber dano letal fica com 1 de vida e Protegido (30% da vida máx.) | `#defesa` |
| Mão Pesada | Ataque básico +20% de dano contra alvos com mais de 75% de vida | `#dano` |
| Revide | O primeiro ataque recebido em cada turno global é respondido com um ataque básico | `#dano` `#espinhos` |

### 7.3 Mago

**Ramo Grimório** (fogo)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Bola de Fogo | Desbloqueia a skill | `#fogo` `#área` |
| 2 | Combustão | Queimado dura +1 turno e causa +50% de dano | `#fogo` |
| 3 | Brasas Vivas | Inimigo que morre Queimado explode (CIRCLE 3, 50% do dano básico) | `#fogo` `#área` |
| 4 | Pirômano | Skills de fogo causam +20% de dano contra alvos Queimados | `#fogo` `#dano` |
| 5 | **Inferno Vivo** (chave) | No fim do turno de um inimigo Queimado, o estado passa para 1 inimigo adjacente (duração 1) | `#fogo` `#área` |

**Ramo Criomancia** (gelo, controle)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Nova de Gelo | Desbloqueia a skill | `#gelo` `#controle` |
| 2 | Geada Profunda | Lento dura +1 turno e a Nova passa a ter tamanho 5 | `#gelo` `#área` |
| 3 | Quebra-gelo | Ataques e skills contra alvos Lentos ou Enraizados causam +25% de dano | `#gelo` `#dano` |
| 4 | Pele de Gelo | Desbloqueia Barreira Arcana. Inimigos que acertam você corpo a corpo ficam Enraizados (1) | `#gelo` `#escudo` |
| 5 | **Zero Absoluto** (chave) | Alvo Lento atingido por skill de gelo fica Enraizado (1). Vale a regra de imunidade a controle | `#gelo` `#controle` |

**Ramo Conjurador de Tempestades** (raio, crítico)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Tempestade Elétrica | Desbloqueia a skill | `#raio` `#crítico` |
| 2 | Sobrecarga | +10 pontos percentuais de chance de crítico com skills | `#crítico` |
| 3 | Raio Encadeado | Desbloqueia a skill | `#raio` `#área` |
| 4 | Capacitor | Cada skill usada dá +1 carga (máximo 3). O ataque básico consome as cargas: +15% de dano por carga | `#raio` `#dano` |
| 5 | **Tempestade Perfeita** (chave) | Crítico com skill reduz em 1 o cooldown das outras skills. Raio Encadeado ganha +2 saltos | `#raio` `#cooldown` |

**Avulsos do Mago**

| Talento | Efeito | Tags |
|---|---|---|
| Foco Arcano | −1 de cooldown nas skills com cooldown 3 ou mais | `#cooldown` |
| Alcance Arcano | Alcance de skills +1 | `#distância` |
| Explosão Final | Uma vez por onda, ao cair abaixo de 30% de vida: CIRCLE 5 de 200% do dano básico e Atordoado (1) | `#área` `#defesa` |
| Ritual de Nível | Ao subir de nível, o cooldown de todas as skills zera | `#cooldown` |
| Passo Arcano | Desbloqueia a skill utilitária (teleporte de alcance 2, cooldown 5) | `#mobilidade` |

### 7.4 Ladino

**Ramo Mãos Ligeiras / Mestre do Veneno** (veneno)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Adaga Envenenada | Desbloqueia a skill | `#veneno` |
| 2 | Lâminas Untadas | Ataque básico aplica Envenenado (1 pilha, 2 turnos) | `#veneno` |
| 3 | Toxina Potente | Teto de pilhas de 5 para 7 e dano por pilha +20% | `#veneno` |
| 4 | Linha Venenosa | Desbloqueia a skill | `#veneno` `#área` |
| 5 | **Epidemia** (chave) | Inimigo que morre Envenenado passa as pilhas ao inimigo mais próximo | `#veneno` `#área` |

**Ramo Mestre das Sombras** (crítico, mobilidade)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Bomba de Fumaça | Desbloqueia a skill | `#controle` |
| 2 | Passo Sombrio | Desbloqueia a skill | `#mobilidade` `#crítico` |
| 3 | Assassino | Furtivo também dá +50% no multiplicador de crítico do ataque | `#crítico` |
| 4 | Dança Mortal | Ao matar, ganha Acelerado (1) | `#mobilidade` `#execução` |
| 5 | **Mestre das Sombras** (chave) | Passo Sombrio com cooldown −2. Depois de usá-lo, o próximo ataque causa +50% de dano | `#mobilidade` `#crítico` |

**Ramo Arsenal Oculto** (arremesso, distância)

| # | Talento | Efeito | Tags |
|---|---|---|---|
| 1 | Adaga Dupla | Adaga de Arremesso atinge também o inimigo mais próximo do alvo (70% do dano) | `#distância` `#área` |
| 2 | Arremesso Rápido | Ataque básico passa a ter alcance 2 com −25% de dano | `#distância` |
| 3 | Aljava Infinita | Cooldown da Adaga de Arremesso −1 (mínimo 1) | `#cooldown` |
| 4 | Chuva de Lâminas | Desbloqueia a skill (CIRCLE 3, alcance 4, 100%, cooldown 4) | `#área` `#distância` |
| 5 | **Arsenal Oculto** (chave) | Cada skill diferente usada na onda dá +5% de chance de crítico até o fim da onda (máximo +25%) | `#crítico` `#skill` |

**Avulsos do Ladino**

| Talento | Efeito | Tags |
|---|---|---|
| Reflexos | Estado Protegido (1 ataque) no início de cada onda: anula o primeiro golpe recebido. Usa estado, não o atributo esquiva (que está fora do conjunto) | `#defesa` |
| Faca no Escuro | O primeiro ataque da onda contra inimigo que ainda não agiu causa +100% de dano | `#dano` |
| Pé Ligeiro | Usar uma skill concede Acelerado (1) | `#mobilidade` |
| Saque | Ao matar, −1 de cooldown em todas as skills (no máximo uma vez por turno) | `#cooldown` `#execução` |
| Golpe Baixo | +40% de dano contra alvos com 2 ou mais estados negativos | `#dano` `#veneno` |

### 7.5 Compartilhados (disponíveis para as três classes)

| Talento | Efeito | Tags |
|---|---|---|
| Vitalidade ×3 | Vida máxima +10% por nível | `#defesa` |
| Força Bruta ×3 | Dano básico +10% por nível | `#dano` |
| Precisão Letal ×3 | Chance de crítico +5 pontos percentuais por nível | `#crítico` |
| Resiliência ×3 | Defesa +8 por nível | `#defesa` |
| Perfuração ×2 | Penetração de defesa +15% por nível | `#dano` |
| Estudo ×2 | Bônus de dano de skills +10% por nível | `#skill` |
| Regeneração Leve | Regeneração: 2% da vida máxima no início do turno | `#cura` |
| Sede de Vida | Roubo de vida de 5% do dano causado | `#roubo_de_vida` |
| Armadura Reativa | Espinhos de 10% do dano recebido | `#espinhos` |
| Segundo Fôlego | Ao subir de nível, ganha Protegido (20% da vida máx., 3 turnos) | `#escudo` |
| Fôlego de Batalha | Ao iniciar a onda, ganha Protegido (10% da vida máx., 2 turnos) | `#escudo` |
| Disciplina | −1 de cooldown em todas as skills (mínimo 1) | `#cooldown` |
| Foco Inicial | A primeira skill de cada onda recebe Concentrado (+50% de dano) | `#skill` |
| Pressa | Acelerado no primeiro turno de cada onda | `#mobilidade` |
| Caçador de Elites | +25% de dano contra elites e chefes | `#dano` |
| Executor | Desbloqueia Golpe Fatal (só Guerreiro e Ladino) | `#execução` |
| Colecionador de Opções | +1 opção em cada oferta de talento (única) | progressão |
| Sorte do Aventureiro | +2 rerolls na run | progressão |
| Lista Negra | +2 banimentos na run | progressão |

Os três últimos mexem na **configuração da run**, e não em atributos de personagem. Isso respeita a decisão do GDD (a configuração é da run, mas estados e talentos podem influenciá-la).

### 7.6 O que a gramática de efeitos ainda não cobre

Ao escrever os talentos acima, quatro necessidades apareceram que não estavam na lista de gatilhos e condições da seção 3.1:

| Necessidade | Usada por | Sugestão |
|---|---|---|
| Gatilho **ao expirar** (estado) | Muralha | Adicionar gatilho `ao_expirar` com parâmetro "intacto" ou "quebrado" |
| Gatilho **ao receber dano letal** | Último Reduto | Gatilho que pode substituir o resultado (a morte), com limite de usos por run |
| Gatilho em **turno de outra entidade** | Presença Intimidante, Revide | Gatilho `fim_do_turno_de(entidade, relação)` |
| **Contador/carga** e **limite por onda ou por turno** | Capacitor, Faca no Escuro, Saque, Reflexos | Campo genérico de "usos por turno/onda/run" e variável de carga no estado |

Sem esses quatro pontos, 8 dos 79 talentos acima (Muralha, Último Reduto, Presença Intimidante, Revide, Capacitor, Faca no Escuro, Saque e Reflexos) viram código especial.

### 7.7 Combos entre ramos e estados (para teste de sinergia)

| Combo | Como funciona | Observação |
|---|---|---|
| Gelo + Quebra-gelo | Nova de Gelo deixa Lento, Quebra-gelo dá +25% nas skills seguintes | Faz a Criomancia ter dano real, não só controle |
| Fogo + Brasas Vivas + Inferno Vivo | Queimado se espalha e explode ao matar | Cresce com a contagem de inimigos: é a build "limpa-onda" |
| Veneno + Golpe Baixo + Oportunista | 2 estados negativos dão +40% e a passiva dá +50% (multiplicadores somados ou compostos ❓) | Precisa decidir se bônus condicionais somam ou multiplicam |
| Furtivo + Marcado | Dois efeitos de crítico garantido no mesmo ataque | Já listado como risco de exploit (seção 11) |
| Escudo + Muralha + Bastião | Escudo expira intacto cura, Provocado mantém inimigos batendo no escudo | Build defensiva que ainda precisa de dano |
| Cooldown em cadeia | Saque + Berserker + Tempestade Perfeita | Cada um reduz cooldown ao matar ou ao critar: pode virar loop. Regra: no máximo 1 redução de cooldown por gatilho por turno |

## 8. Inimigos, comportamentos, elites e chefes

### 8.1 Papéis e catálogo proposto

A ameaça usa a fórmula da seção 2.1, estendida para alcance e papel: `dano × vida / 50 × (1 + 0,25 × (mov − 1)) × (1 + 0,5 × (alcance − 1)) × fator de papel`. O fator de papel é 1,0 para combate comum, 1,3 para controle, 2,0 para suporte e 0,6 para unidades lentas.

Os cinco inimigos existentes entram como estão (valores de [[sistema_combate]]). Os novos adicionam papéis que hoje não existem.

| Inimigo | Papel | Vida | Dano | Mov. | Alc. | Ameaça | Onda mín. | Comportamento |
|---|---|---|---|---|---|---|---|---|
| Goblin | Bruto | 30 | 5 | 1 | 1 | 3,0 | 1 | Persegue e ataca |
| Rat | Enxame | 20 | 5 | 2 | 1 | 2,5 | 1 | Persegue e ataca (rápido) |
| Slime | Bruto | 40 | 5 | 1 | 1 | 4,0 | 1 | Persegue e ataca (resistente) |
| EyeBat | Enxame | 25 | 5 | 2 | 1 | 3,1 | 2 | Persegue e ataca (rápido) |
| FireSkull | Bruto | 25 | 7 | 1 | 1 | 3,5 | 3 | Persegue e ataca (dano alto) |
| Arqueiro (novo) | Distância | 20 | 6 | 1 | 2 | 3,6 | 4 | Mantém distância 2; foge se o jogador chega adjacente |
| Berserker (novo) | Bruto | 35 | 5 | 1 | 1 | 3,5 | 5 | Fúria ao cair abaixo de 50% de vida |
| Aranha (novo) | Controle | 25 | 4 | 1 | 2 | 3,9 | 6 | Teia a cada 3 turnos: Enraizado (1) no jogador |
| Cultista (novo) | Suporte | 25 | 3 | 1 | 1 | 3,0 | 7 | Cura 20% da vida máx. de um aliado ferido a até 2 casas (cooldown 2). Prioriza ficar atrás dos aliados |
| Bomba (novo) | Especial | 10 | 12 | 2 | 1 | 3,0 | 8 | Ao ficar adjacente ao jogador, arma e explode no turno seguinte (CIRCLE 3, telegrafado) |
| Bruxa (novo) | Controle | 22 | 5 | 1 | 2 | 4,3 | 9 | Maldição a cada 4 turnos: Fraquejado (2) no jogador |
| Golem (novo) | Bruto lento | 80 | 8 | 0,5* | 1 | 7,7 | 12 | Anda 1 casa a cada 2 turnos; ataca normalmente |
| Invocador (novo) | Suporte | 30 | 3 | 1 | 2 | 5,4 | 14 | A cada 4 turnos invoca um Rat (máximo 2 vivos); mantém distância |

Notas:

- (*) Movimento efetivo de 0,5 casa por turno; o fator de papel 0,6 já cobre isso.
- Os valores dos inimigos novos são pontos de partida, com a mesma lógica de ameaça dos existentes. Os existentes escalam por onda (seção 9); os novos usam o mesmo multiplicador.
- Um **controlador** (Aranha, Bruxa) tira a vez do jogador em vez de dar dano. Por isso há limite de duas unidades por tipo em cada onda (seção 9) e imunidade temporária ao mesmo controle (seção 4.1).
- O **Cultista** e o **Invocador** pedem uma regra de prioridade de alvo. Eles viram o primeiro inimigo que o jogador quer matar, o que cria decisão tática (ir atrás do suporte ou limpar a frente).
- A **Bomba** é o primeiro inimigo que o jogador quer manter longe e não destruir de perto. Mate de longe ou saia da área.

### 8.2 Comportamentos de IA (ideias de `EnemyAction`)

O sistema atual já separa `EnemyBehavior` e `EnemyAction`. As ações abaixo cabem nele como novos assets, com parâmetros nos dados:

| Ação | O que faz | Parâmetros | Usada por |
|---|---|---|---|
| `KeepDistance` | Se está mais perto que a distância alvo, recua; se está mais longe, aproxima | distância alvo | Arqueiro, Bruxa, Invocador |
| `FleeWhenLow` | Foge do jogador quando a vida cai abaixo de X% | limite de vida | Cultista (opcional) |
| `HealAlly` | Cura o aliado mais ferido no raio | % de cura, raio, cooldown | Cultista |
| `BuffAllies` | Aplica um estado a aliados em raio | estado, raio, cooldown | Afixo ou elite futuro |
| `SummonMinion` | Cria inimigos em células livres perto de si | tipo, máximo vivo, intervalo | Invocador, Necromante |
| `ApplyStateOnHit` | Ao acertar, aplica um estado ao alvo | estado, duração, chance ou cooldown | Aranha, Bruxa, afixos de elite |
| `ChargedAttack` | Gasta o turno carregando; resolve no turno seguinte; células marcadas | área, dano, atraso | Chefes, Bomba |
| `Explode` | Causa dano em área ao morrer ou ao armar | área, dano | Bomba, afixo Explosivo |
| `Teleport` | Reposiciona o inimigo | alcance, intervalo | Olho Ancião |

**Telegrafia.** O ataque carregado combina com a regra "lógica imediata, visual atrasado": a intenção do inimigo é decidida no turno dele, as células aparecem marcadas durante o turno do jogador, e o dano é resolvido no início do turno seguinte do inimigo. O jogador tem uma janela de decisão e não há surpresa.

### 8.3 Elites: afixos como estados permanentes

Um elite é um inimigo comum com **mais vida (×2), mais dano (×1,25) e de 1 a 3 afixos**, como em Diablo 4, onde elites têm de 1 a 3 afixos. Cada afixo é um estado permanente aplicado no spawn (seção 4.3), sem sistema novo.

| Afixo | Efeito | Contra-jogo |
|---|---|---|
| Blindado | Defesa +30 | Perfuração, dano de estado |
| Veloz | Movimento +1 | Controle de movimento, área |
| Vampírico | Cura 50% do dano que causa | Escudo, matar rápido |
| Espinhoso | Devolve 25% do dano recebido | Skills à distância, veneno, fogo |
| Explosivo | Ao morrer, explode (CIRCLE 3, 100% do dano dele) | Matar de longe |
| Regenerante | Regeneração de 5% da vida máx. por turno | Dano concentrado |
| Furioso | Fúria permanente (dano +30%) | Matar rápido, defesa |
| Congelante | Ataques aplicam Lento (1) no jogador | Imunidade a controle, escudo |
| Envenenador | Ataques aplicam Envenenado (2 turnos) | Regeneração, cura |
| Chocadeira | Ao morrer, invoca 2 Rats | Área |

Regras sugeridas:

- **Quantidade de afixos por onda:** 1 afixo até a onda 24, 2 afixos de 25 a 39, 3 afixos a partir da 40.
- **Incompatibilidades:** Blindado com Regenerante (o inimigo que ninguém consegue matar), Espinhoso com Explosivo, Congelante com Envenenador (controle e dano sobre o mesmo alvo ao mesmo tempo é demais para o início).
- **Custo no orçamento:** um elite custa 3 vezes a ameaça média e ocupa 3 vagas de contagem. Assim ele substitui três inimigos comuns (e ocupa uma célula só).
- **Telegrafia:** o nome e os afixos do elite aparecem ao selecionar ou tocar nele, e o ícone de cada afixo fica sobre o inimigo.
- **Recompensa:** XP do elite ×3. Outras recompensas dependem do ❓ de ondas especiais (seção 12).

### 8.4 Chefes

Chefes aparecem a cada 10 ondas (10, 20, 30 e 40, depois repetem com variações), em ondas de chefe: o chefe usa **60% do orçamento de ameaça** e o restante vai para uma escolta de cerca de 40% da contagem normal. Para chefes valem as regras de controle da seção 4.1 (metade da duração, imunidade de 2 turnos).

**Dimensionamento (ponto de partida):**

- Vida do chefe = 0,72 × vida total de uma onda normal da mesma altura. Somada à escolta (cerca de 0,4 da vida total normal), a onda de chefe fica perto de 1,1 vez a vida total de uma onda normal, ou seja, um pouco mais exigente na checagem de dano da seção 1.
- Dano do chefe = 2 × o dano escalado de um inimigo comum. A pressão vem de poucos golpes grandes e telegrafados, e não de muitos pequenos.

| Onda | Chefe | Vida | Dano | Mecânicas | O que testa |
|---|---|---|---|---|---|
| 10 | Rei Slime | 234 | 13,6 | Salto telegrafado (CIRCLE 3 sobre a célula do jogador, resolve no turno seguinte). Abaixo de 50%, divide a vida restante em dois Slimes | Posicionamento e área |
| 20 | Necromante | 472 | 17,6 | Mantém distância 2. Invoca Esqueleto a cada 3 turnos (máximo 4 vivos). Silencia o jogador (cooldown 5). Toma −50% de dano enquanto houver esqueleto vivo | Dano concentrado ou área (cenário D) |
| 30 | Golem Ardente | 593 | 21,6 | Anda 1 casa a cada 2 turnos. Pulso de Lava telegrafado (linha e coluna do Golem) com Queimado (2). Abaixo de 50%, o Pulso passa a ocorrer a cada 2 turnos | Mobilidade e leitura do grid |
| 40 | Olho Ancião | 714 | 25,6 | Alcance 3. Teleporta a cada 4 turnos. Raio telegrafado ocupa uma linha ou coluna inteira. Abaixo de 50%, usa linha e coluna ao mesmo tempo | Reação e burst |

As vidas acima vêm de `0,72 × vida total normal` do protótipo da seção 9 (325, 655, 823 e 991). A vida do chefe é o parâmetro que mais precisa de playtest.

## 9. Algoritmo de geração de ondas

### 9.1 Princípios

- **Função pura.** `GerarOnda(seed, número da onda) → lista de inimigos, elites e posições`. Sem estado global, o que torna a função testável e reproduzível.
- **PRNG próprio e fluxos separados.** Em vez de `UnityEngine.Random` ou `System.Random` (cujo algoritmo não é garantido entre versões e plataformas), usar um gerador pequeno e fixo (SplitMix64, PCG ou xoshiro) inicializado por `hash(seed, fluxo, índice)`. Fluxos propostos: **ondas** (índice = número da onda), **talentos** (índice = nível e tentativa de reroll), **combate** (críticos), **IA** (desempates), **visual** (sem relação com a lógica).
- **Orçamento de ameaça com contagem.** Duas variáveis guiam a onda: a **contagem** de inimigos (variante B da seção 2.4) e o **orçamento** de ameaça. A contagem fixa o número de vagas; o orçamento decide quem entra.
- **Papéis e limites** garantem que a onda seja legível, mesmo com sorteio.

### 9.2 Passos

```
N        = min(12, 3 + onda/2)                 // contagem (teto 12)
escala   = (1 + 0,05 × (onda − 1)) × (1 + 0,04 × (onda − 1))
B        = N × 3,23 × escala                   // orçamento (3,23 = ameaça média dos 5 inimigos atuais)
tipos    = 2 (ondas 1–3), 3 (4–9), 4 (10+)     // tipos distintos por onda
```

1. **Escolher os tipos.** Sorteio ponderado entre os inimigos com `onda mín. ≤ onda`. Tipos que entraram nas últimas 2 ondas pesam ×2 (para o jogador conhecê-los). O primeiro tipo sempre é corpo a corpo (Bruto ou Enxame), para a onda nunca ser só de longa distância.
2. **Reservar vagas de elite** (ondas de 6 em diante, exceto chefe). Probabilidade `min(0,6; 0,06 × (onda − 5))`, no máximo `1 + onda/20` elites. Cada elite ocupa 3 vagas e custa 3 vezes a ameaça média.
3. **Distribuir os inimigos.** Cada tipo escolhido entra pelo menos uma vez (se couber no orçamento). As vagas restantes saem de um sorteio ponderado, respeitando: **nenhum tipo passa de 50% das vagas**, no máximo 2 unidades de cada tipo de controle, suporte e especial (e 4 de distância), e cada sorteio só aceita um inimigo se o orçamento restante ainda cobre as vagas que faltam com o mais barato dos tipos.
4. **Posicionar.** Células livres sorteadas com fluxo de ondas, a pelo menos 3 casas (Manhattan) do jogador; atiradores e suportes na metade mais distante; nunca mais de 40% do grid ocupado (inclui o jogador).
5. **Validar** (e tentar de novo com `tentativa + 1` no mesmo fluxo): soma de ameaça entre 85% e 115% de B, pelo menos um inimigo corpo a corpo, pelo menos um que alcance o jogador em até 3 turnos.
6. **Chefe** (onda múltipla de 10): o chefe ocupa 60% de B; a escolta usa 40% do orçamento e cerca de 40% da contagem.

### 9.3 Protótipo e resultados

O passo a passo acima foi implementado em Python (arquivo `prototipo_ondas.py`, na mesma pasta) com o PRNG SplitMix64, para validar a ideia antes de escrever C#. Resultados:

**Determinismo.** `gerar(12345, 19)` retorna a mesma onda em toda chamada. Sementes diferentes geram ondas diferentes.

**Exemplos com a seed 12345** (ameaça usada / orçamento B):

| Onda | Vagas | Composição | Uso do orçamento |
|---|---|---|---|
| 1 | 3 | Rat ×2, Slime ×1 | 93% |
| 3 | 4 | Goblin ×2, FireSkull ×2 | 101% |
| 5 | 5 | Goblin ×3, FireSkull ×1, Arqueiro ×1 | 100% |
| 9 | 7 | Goblin ×4, Slime ×1, Arqueiro ×2 | 103% |
| 15 | 10 | Goblin ×5, EyeBat ×4, Bruxa ×1 | 99% |
| 19 | 12 | Rat ×4, EyeBat ×3, FireSkull ×3, Bruxa ×2 | 99% |
| 29 | 12 | Goblin ×6, FireSkull ×3, Berserker ×2, EyeBat ×1 | 100% |
| 39 | 8 inimigos (2 elites + 6 comuns) | 2 elites, Cultista ×3, Slime ×2, Arqueiro ×1 | 103% |

**Estatística por onda** (3.000 sementes por onda; ondas 1, 5, 9, 19, 29 e 39):

- Média do uso do orçamento: 94% na onda 1 e **100% a 102%** nas demais. Mínimo entre 83% e 87%.
- **Sem a etapa de validação**, entre 3,3% e 8,3% das ondas (nas ondas 5 a 39) ficam acima de 115% do orçamento (casos com um tipo caro obrigatório e poucas vagas, como Golem, Invocador ou elite). Por isso a etapa 5 existe.
- **Composições distintas** em 3.000 sementes: 5 na onda 1, 143 na onda 5, 897 na onda 9, mais de 1.800 a partir da 19. Nas ondas 1 a 3 há pouca variedade por construção (poucos inimigos e tipos), e isso é aceitável como ritmo de abertura.
- Maior fatia de um tipo (média): 67% na onda 1, 52% na onda 5, 43% a 45% nas ondas altas.

**Duas lições do protótipo (relevantes para a implementação):**

1. **Sorteio puro gera ondas monótonas.** A primeira versão (só peso por tipo) produziu 12 FireSkulls ou 7 EyeBats em uma onda. O limite de 50% por tipo e o mínimo de uma unidade por tipo escolhido resolveram.
2. **Elites têm de substituir vagas.** Quando o elite ocupava só uma vaga, o uso médio do orçamento nas ondas 29 e 39 ficava em 124%. Com 3 vagas por elite, voltou a 102%.

**O que o protótipo não valida:** o balanceamento (se uma onda é difícil de verdade), as posições (sem grid real), e as interações entre inimigos (cura, invocação). Isso exige o simulador de combate (seção 13).

## 10. Cenários de aplicação

Os cenários assumem **uma ação por turno** (andar, atacar ou usar skill), como no jogo atual, e as regras de duração e acúmulo da seção 4. Os números vêm de contas simples, descritas em cada cenário, e servem para mostrar o comportamento, não como balanceamento final.

### Cenário A: Mago de gelo e a leitura do controle

Mago com Nova de Gelo (CIRCLE 3, alcance 2, Lento 2) e Quebra-gelo, na onda 8: 7 inimigos, vida ×1,35 e dano ×1,28.

- **Lento reduz o movimento em 1.** Um Goblin (movimento 1) Lento fica com movimento 0: não se aproxima. O Mago, com alcance 2, ataca a cada turno sem ser tocado enquanto o Lento durar.
- **Duração.** Com a regra do GDD, Lento (2) aplicado no turno do jogador conta como primeiro turno do inimigo naquele mesmo turno global. O inimigo fica parado em 2 turnos dele. Como o cooldown da Nova é 3, há **1 turno de folga** a cada ciclo, o que evita travamento total.
- **Efeito sobre quem já está adjacente.** Lento e Enraizado **não impedem o ataque** de quem já está colado no jogador. Para o gelo funcionar como defesa, o Mago precisa agir antes que o inimigo chegue (alcance 2), ou usar a Pele de Gelo (atacantes corpo a corpo ficam Enraizados) e depois sair de perto.
- **Dano.** A Nova causa 16 por alvo (133% de 12). Em um aglomerado de 5 inimigos são 80 de dano por conjuração. A vida total da onda 8 é cerca de 265. Em estimativa grosseira (que ignora inimigos saindo da área ao morrer), 3 conjurações mais as Faíscas dos turnos intermediários limpam a onda em perto de 10 turnos, bem abaixo do limite de 50. Área de gelo sobre aglomerado é uma solução muito forte para ondas de contagem alta.

**Pergunta de design:** a Nova com alcance 2 e área de tamanho 3 é forte demais contra aglomerados? Possíveis ajustes: cooldown 4, ou dano 100% com o bônus vindo do Quebra-gelo.

### Cenário B: Guerreiro de espinhos contra o limite de 50 turnos

Guerreiro na onda 10, 4 atacantes adjacentes (dano ×1,36, ou seja, 6,8 por golpe). Build com Couraça, Resiliência ×2 (defesa total 36, redução 26,5%), Pele de Ferro (−10%), Tenacidade (3%) e Sede de Sangue.

| Item | Valor |
|---|---|
| Dano bruto por turno (4 atacantes) | 27,2 |
| Dano recebido após defesa e Pele de Ferro | 18,0 |
| Cura por turno (Tenacidade 3,6 + roubo 1,2) | 4,8 |
| Perda líquida por turno | 13,2 |
| Turnos até cair, partindo de 120 de vida | 9,1 |
| Espinhos 25% do dano bruto (Espinhos 15% + Armadura Reativa 10%) | 6,8 por turno |
| Espinhos 50% do dano bruto | 13,6 por turno |
| Dano por turno exigido da onda 10 (seção 2.4) | 15,5 |

Leituras:

- **Espinhos sozinhos não limpam a onda**, mas dobrar a taxa para 50% quase alcança o dano exigido. Eles funcionam como complemento do dano ativo.
- **A build não consegue "enrolar" sem risco.** Mesmo com defesa 36, a perda líquida de 13 por turno mata em cerca de 9 turnos sem level up. O jogador precisa matar para curar (level up), e isso é o comportamento desejado.
- **Quem calcula os espinhos importa.** Se a base for o **dano bruto** (antes da defesa), defesa e espinhos se somam como build. Se for o dano pós-defesa, defesa alta enfraquece os espinhos e as duas direções competem. A proposta adota o dano bruto.

### Cenário C: Ladino e a regra de duração por turno do portador

Ladino (dano básico 15) com Adaga Envenenada (175%, cooldown 2, veneno 3 turnos, 1 pilha) contra um Goblin da onda 10 (43 de vida). Dano por tick: **30% do dano básico por pilha, definido na aplicação**, ou seja, 4,5 por pilha.

- Turno global 1: o Ladino usa a Adaga (26 de dano direto). O inimigo ainda não jogou, então o veneno conta como primeiro turno: tick de 4,5 no início do turno dele (vida restante cerca de 12,8), duração 3 → 2.
- Turno global 2: o Ladino ataca (15 de dano) e mata o Goblin antes do segundo tick.
- Sem morte: o veneno daria 3 ticks (turnos globais 1, 2 e 3), total de 13,5, e termina ao fim do turno do inimigo no turno global 3.

Com **cooldown 2 e duração 3**, o jogador reaplica no turno 3 e a pilha anterior ainda está no último tick:

| Turno global | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Pilhas ativas no tick do inimigo | 1 | 1 | 2 | 1 | 2 | 1 |
| Dano de veneno | 4,5 | 4,5 | 9,0 | 4,5 | 9,0 | 4,5 |

Em regime, são **cerca de 1,4 pilha ativa**, ou seja, 6,3 de dano por turno por alvo com uma só skill. Cada aplicação rende 13,5 (51% do dano direto da skill), então o veneno é **um complemento relevante, não a fonte principal de dano**. Os talentos Toxina Potente (+20% e teto 7), Linha Venenosa (2 pilhas, área linear) e Epidemia (veneno passa ao morrer) são o que transforma isso em build de área.

**Ponto de atenção:** esse cálculo assume que pilhas expiram **separadamente** (modelo de intensidade, como Guild Wars 2). Se o GDD preferir que a reaplicação apenas renove a duração, o ❓ de acúmulo precisa ser decidido antes de balancear o veneno.

### Cenário D: chefe sem build de área (Necromante, onda 20)

Necromante com 472 de vida, dano 17,6, invocando 1 Esqueleto a cada 3 turnos (máximo 4; vida 15 × 1,95 = 29,3; dano 7). Modelo simplificado: o jogador mata os esqueletos vivos antes de atacar o chefe (por isso a redução de dano do chefe nunca entra na conta).

| Dano por golpe do jogador | Golpes para matar um esqueleto | Turnos para vencer o chefe |
|---|---|---|
| 15 | 2 | 90 |
| 22 | 2 | 60 |
| 28 | 2 | 45 |
| 30 | 1 | 23 |
| 40 | 1 | 17 |

**Penhasco de breakpoint.** A diferença entre 29 e 30 de dano por golpe corta o tempo quase pela metade (45 → 23 turnos). Com 15 de dano, o chefe sobrevive ao limite de 50 turnos com folga. Parte disso é desejável (builds sem dano concentrado nem área não vencem o chefe), mas vale deixar o penhasco **menos cortante**. Com esqueletos **sem escala** (15 de vida), 15 de dano já derruba cada um em um golpe, e o tempo para vencer fica em 47 turnos com 15 de dano, 32 com 22 e 23 com 30: uma curva suave, sem salto.

**Pergunta de design:** o que acontece quando o limite de 50 turnos estoura em uma onda de chefe? A próxima onda chega com o chefe ainda vivo? Esse caso precisa de regra explícita (ver seção 12).

### Cenário E: cercado e a janela do level up

Guerreiro (120 de vida, defesa 10, Pele de Ferro) cercado por 4 Goblins na onda 5 (dano ×1,16, vida ×1,2): bruto de 23,2 por turno, 19,0 após defesa e Pele de Ferro. Sem cura, ele aguenta 6,3 turnos.

- Um Goblin da onda 5 tem 36 de vida: 3 golpes de 15.
- **Abaixo de cerca de 57 de vida**, o jogador não consegue mais matar um Goblin sem arriscar cair (3 turnos × 19). Esse é o limiar em que a decisão "matar o inimigo que me dá o level up" vira questão de vida ou morte.
- Se a barra de XP estiver quase cheia, **o inimigo certo a matar é o que fecha o nível**, e o jogador sabe disso. Isso cria uma tensão boa (risco calculado), e a cura de 100% faz com que o level up funcione como "último recurso previsível".

**Efeitos colaterais a observar:**

- O jogador pode **evitar o final da onda** de propósito para controlar quando o level up cai. Como a onda termina pelo limite de 50 turnos, isso é limitado, mas vale testar.
- Talentos como Último Reduto e Segundo Fôlego (escudo ao subir de nível) reforçam esse ciclo e precisam ser contados na curva de dificuldade.

## 11. Riscos e exploits

| Risco | Por que importa | Mitigação sugerida |
|---|---|---|
| **Build imortal ou de enrolação** (defesa, cura e escudo somados) | O limite de 50 turnos só ajuda se a onda seguinte chegar. A defesa percentual nunca chega a 100%, mas cura por turno e escudo somados podem anular o dano | Defesa com retorno decrescente e dano mínimo de 1 por golpe; teto para cura por turno (por exemplo 10% da vida máxima); escudo com teto de 50% da vida máxima |
| **Roubo de vida em área** | Roubo de vida × número de alvos atingidos cresce com a contagem de inimigos (12 alvos em uma Nova) | Roubo de vida só sobre o alvo principal, ou teto por turno (15% da vida máxima) |
| **Crítico sem teto** | Furtivo, Marcado, Sobrecarga e Precisão Letal empilham | Teto de chance 75% e de multiplicador 4,0. Furtivo e Marcado não se acumulam no mesmo ataque |
| **Loop de cooldown** | Saque, Berserker, Tempestade Perfeita e Ritual de Nível reduzem ou zeram cooldown | No máximo 1 redução de cooldown por gatilho por turno; Ritual de Nível só ao subir de nível |
| **Rerolls infinitos por anúncio** | Se anúncios recarregam rerolls sem limite, a qualidade da run depende de quantos anúncios o jogador assiste, e quem compra a remoção de anúncios recarrega de graça, o que pede o mesmo limite | Limite de recargas por nível e por run; recarga dá no máximo +1; decisão ligada à seção 7 do GDD (monetização) |
| **Pool esgotada** | Com 38 a 39 talentos por classe e uma run de 25 talentos na onda 40, a oferta pode ficar sem opção nova útil depois do nível 30 | Talentos com níveis (×N); talentos de "rotina" que podem aparecer de novo; prever uma oferta de reserva |
| **Inimigos sobreviventes se acumulam** | Pelo GDD, a nova onda chega mesmo com inimigos vivos. Uma build que não acaba a onda recebe a onda seguinte por cima | Teto de inimigos vivos no grid (por exemplo 14, perto de 40% das células); excedente entra como reforço quando houver célula livre |
| **Limite de 50 turnos em onda de chefe** | O chefe continua vivo quando a onda seguinte chega | Regra explícita (seção 12): o chefe fica e a escolta soma; ou o chefe entra em Fúria permanente |
| **IA gananciosa travada** | `FindStepToward` escolhe o passo que reduz a distância sem considerar bloqueios; com 12 inimigos em um grid 6×6 formam-se filas que não andam | Busca de caminho (BFS) que trata células ocupadas como custo, não como parede, e/ou permitir trocar de lugar com aliado parado |
| **RNG sem semente** | `Random.Range` ou `System.Random` na lógica quebra a reprodutibilidade (o dano visual da `Cell` é só visual, mas qualquer sorteio de lógica precisa de fluxo próprio) | PRNG próprio com fluxos nomeados (seção 9.1); teste automático que roda a mesma run duas vezes e compara |
| **Sensibilidade a um inimigo a mais** | Em Into the Breach, um inimigo extra pode transformar um desafio divertido em impossível | Telegrafar ataques de elites e chefes; calibrar contagem com playtest; validação de ameaça na geração (seção 9.2) |
| **Escala infinita sem fim natural** | Na onda 80 a vida dos inimigos é ×4,95 e o dano ×4,16. Builds sem multiplicadores morrem, o que é desejado, mas sem teto os números ficam enormes | Aceitar o fim natural da run; avaliar dividir o aumento (por exemplo +3% a partir da onda 50) quando houver dados |
| **Ação de esperar inexistente** | Sem "passar a vez", o jogador é forçado a andar ou atacar, e isso mexe em kiting, estados de duração e DoT (esperar o veneno agir) | Decidir na seção 12 |
| **Distância Manhattan × euclidiana** | O GDD e o código divergem a partir do alcance 3 (24 × 28 células) | Escolher uma regra e centralizar em um helper (seção 13) |

## 12. Sugestões para os ❓ do GDD (não decididas)

Esta seção **não resolve** nada. Ela organiza opções e dá uma inclinação, para a conversa em que o autor retomar os ❓.

### 12.1 Estados

| ❓ | Opções | Inclinação |
|---|---|---|
| Reaplicação e acúmulo | (A) renova a duração, sem somar força. (B) soma força, cada aplicação expira sozinha. (C) política por estado | **C**: renovação como padrão; intensidade com teto só em veneno e sangramento (seção 4.1) |
| Remoção antecipada | (A) nenhuma por enquanto. (B) skills e talentos que removem um estado negativo. (C) remoção ao subir de nível | **A** na primeira versão; **B** quando houver uma classe com tema de cura. Se existir, definir se elites e chefes são imunes |
| Estados ao mudar de onda | (A) todos persistem, com a duração em turnos da portadora. (B) debuffs do jogador caem no fim da onda, buffs seguem. (C) tudo é limpo | **A**: coerente com a regra "sem pausa entre ondas" e com sobreviventes que carregam estados; o impacto fica pequeno porque as durações são curtas |
| Atributos e estados nos inimigos | (A) mesmo modelo do jogador (atributos base + estados). (B) modelo reduzido só com vida, dano, movimento, alcance | **A** para os estados e um conjunto reduzido de atributos (vida, dano, movimento, alcance, defesa), e a escala por onda aplicada como **multiplicador no spawn**, e não como estado visível |
| Resistência a controle de elites e chefes | (A) imunidade total a controle. (B) metade da duração e imunidade pós-efeito de 2 turnos | **B** (seção 4.1) |

### 12.2 Ondas e inimigos

| ❓ | Opções | Inclinação |
|---|---|---|
| Força esperada por onda | (A) orçamento puro (variante A). (B) contagem cresce e atributos escalam devagar (variante B) | **B** (seção 2.4) |
| Força de cada inimigo | Fórmula de ameaça (seção 8.1) | Usar a fórmula como ponto de partida e ajustar com simulação |
| Regras da pool (onda mín. e máx.) | Só onda mínima; ou mínima e máxima | Só **onda mínima**: inimigos simples continuam úteis como enxame em ondas altas |
| Elites e chefes | Seções 8.3 e 8.4 | Afixos como estados permanentes; chefes a cada 10 ondas |
| Limite de inimigos pelo grid | Teto de contagem 12; ocupação máxima de 40% | **12 e 40%** (seções 9.2 e 11) |
| Ondas especiais: frequência | A cada 5 ondas (5, 15, 25…) entre os chefes, ou aleatória pela seed | **A cada 5 ondas** nas ondas 5, 15, 25, 35… |
| Ondas especiais: mecânicas | Horda (1,5× contagem de comuns com vida ×0,7), Elite (1 a 2 elites garantidos), Névoa (Cegado global no início), Maldição (inimigos nascem com Fúria) | Começar com **Horda** e **Elite** (usam só o que já existe) |
| Ondas especiais: recompensas | XP ×1,5, +1 reroll, +1 opção na próxima oferta | **+1 reroll** (reforça a ferramenta do jogador e não mexe no ritmo de XP) |
| Onda de chefe estourando o limite de turnos | (A) chefe permanece e a escolta da próxima onda soma. (B) chefe entra em Fúria permanente. (C) a onda só termina com o chefe morto | **A + B** combinados |
| Sobreviventes de onda anterior | (A) acumulam sem limite. (B) acumulam até o teto do grid, excedente fica em fila | **B** |

### 12.3 Talentos, ferramentas e regras de turno

| ❓ | Opções | Inclinação |
|---|---|---|
| Detalhes do sorteio | (A) sorteio puro ponderado. (B) saco embaralhado por classe | Ponderado (seção 3.3) com **proteção contra azar**: se 3 ofertas seguidas não têm nenhuma tag em comum, a seguinte tem |
| Quantidade e custo de rerolls e banimentos | (A) total da run (por exemplo 3 rerolls, 2 banimentos e 1 skip). (B) recarga a cada 5 níveis. (C) 1 reroll grátis por nível, o resto limitado | **B**: começa com 2 rerolls e 1 banimento e ganha +2 e +1 a cada 5 níveis (níveis 5 e 10 incluídos), o que dá 6 rerolls e 3 banimentos para as 13 ofertas até a onda 20. Anúncio recarrega até +1 a cada 5 níveis |
| Ação de esperar | (A) não existe. (B) existe e consome o turno. (C) existe e dá um pequeno benefício (por exemplo +10% de defesa até o próximo turno) | **B** como ferramenta tática; **C** pode virar talento |
| Métrica de distância | (A) Manhattan, como no GDD. (B) euclidiana, como no código | **A** (previsível no grid, cada alcance cobre uma forma de losango) e atualizar o código |
| Soma ou composição de bônus condicionais | Somar bônus (+50% +40% = +90%) ou multiplicar (×1,5 ×1,4 = ×2,1) | **Somar** dentro da mesma categoria (dano, defesa), compor entre categorias diferentes |
| Fórmula de defesa | Percentual `d/(d+100)` com dano mínimo 1 (seção 1) | Adotar, mais a penetração como atributo ofensivo |

## 13. Impacto no código e próximos passos

### 13.1 O que isto pede do código (sem decidir ainda)

Não há nada para implementar até as decisões da seção 12, mas vale já saber o formato dos dados.

| Peça | Descrição |
|---|---|
| `StateDefinition` (ScriptableObject) | Nome, ícone, lista de efeitos, política de duração/acúmulo, tags, tipo (buff/debuff/controle/DoT) |
| `TalentDefinition` (ScriptableObject) | Tags, pré-requisitos, nível máximo, efeitos (gatilho, condição, ação), classe ou compartilhado |
| `EffectRunner` | Executa gatilhos e ações da gramática da seção 3.1 e os quatro acréscimos da seção 7.6 |
| `StateController` | Cuida de adição, renovação, acúmulo, expiração por turno da portadora e leitura de atributos efetivos |
| `DamageCalculator` | Fórmulas de dano, crítico, defesa, penetração, escudo e dano mínimo, em um lugar só e com testes |
| `RngStreams` | PRNG próprio (seção 9.1) com fluxos nomeados |
| `WaveGenerator` | Função pura da seção 9.2, testada em EditMode |
| `DistanceHelper` | Uma única função para Manhattan ou euclidiana |
| Novas `EnemyAction` | `KeepDistance`, `HealAlly`, `SummonMinion`, `ApplyStateOnHit`, `ChargedAttack`, `Explode`, `Teleport` |
| Marcador de telegrafia (UI) | Camada visual para as células marcadas |
| Ação de esperar | Entrada, regras de turno e testes |
| Simulador em lote | Roda milhares de ondas e runs (seed fixa) e mede tempo de limpeza, vida perdida e curva de nível (substitui contas de cabeça) |

### 13.2 Ordem sugerida (para quando as decisões estiverem fechadas)

1. **Base determinística:** `RngStreams`, `DistanceHelper`, `DamageCalculator`, com testes.
2. **Estados:** `StateDefinition` e `StateController` com a regra de duração do GDD, depois os estados do catálogo da seção 4.2.
3. **Classes e skills:** valores da seção 5 e skills da seção 6, com dano em % do básico.
4. **Gerador de ondas** (seção 9) e novos inimigos, sem elites.
5. **Talentos:** gramática de efeitos e uma árvore completa de uma classe (a que o autor quiser provar primeiro), depois as demais.
6. **Elites, chefes e telegrafia.**
7. **Simulador** (pode vir antes, a partir do passo 4, para calibrar).

### 13.3 Perguntas para a próxima conversa

Em ordem de impacto:

1. Reaplicação e acúmulo de estados (muda veneno, sangramento e vários talentos).
2. Fórmula de defesa (proposta percentual com dano mínimo) e tetos de crítico, roubo de vida e cura.
3. Estados e atributos nos inimigos (muda a forma de configurar inimigos e elites).
4. O que acontece quando o limite de 50 turnos estoura, em onda comum e em onda de chefe.
5. Ação de esperar e métrica de distância.
6. Valores finais de classe e as skills iniciais (seção 3.2 do GDD), que podem sair desta proposta.

## Fontes

- [Slay the Spire, Vulnerable](https://slay-the-spire.fandom.com/wiki/Vulnerable): +50% de dano de ataques, acúmulo só de duração.
- [Guild Wars 2, Effect stacking](https://wiki.guildwars2.com/wiki/Effect_stacking): acúmulo por duração ou por intensidade.
- [Into the Breach (Game Developer)](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-): telegrafia dos ataques e sensibilidade à contagem de inimigos.
- [Diablo 4, elites e afixos (Maxroll)](https://maxroll.gg/d4/resources/elites-affixes): elites com 1 a 3 afixos.
- [Vampire Survivors, análise de design (teemo.dev)](https://teemo.dev/game-design/vampire-survivors/): ritmo, tempo e metaprogressão.
- [GameAnalytics, benchmarks de jogos 2026](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks): retenção e duração de sessão em mobile.
- [Dofus, devblog de duração dos efeitos (espelho JeuxOnline)](https://dofus.jeuxonline.info/actualite/32815/devblog-modifications-duree-effets-sorts): regra de duração por turno, usada como inspiração e depois substituída pela regra do GDD.

Arquivos de apoio: `prototipo_ondas.py` (protótipo do algoritmo de ondas, na mesma pasta).
