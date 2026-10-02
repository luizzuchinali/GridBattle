---
tags:
  - design
  - gdd
created: 2026-07-12
updated: 2026-10-01
---
## Grid Battle (TEMP)

| Campo                        | Valor                   |
| ---------------------------- | ----------------------- |
| **Título**                   | Grid Battle             |
| **Gênero**                   | RPG, Survive, TurnBased |
| **Plataforma(s)**            | Mobile e PC             |
| **Engine**                   | Unity                   |
| **Classificação indicativa** | Livre                   |
| **Autor(es)**                | Zuchinali Softworks     |

---

## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid. A #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A cada nível que o jogador receber, irá receber um ponto de trait para escolher na árvore de #trait. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo.

### 1.3 Pillars de Design

[3 a 5 princípios que guiam todas as decisões de design. Ex.: "Decisões sobre sorte", "Cada partida é única".]

1. Cada ação do jogador importa
2. Cada partida deve passar o sentimento de ser única 
3. O rng no jogo deve ser um fator, porém um fator controlável pelo jogador

### 1.4 Unique Selling Points (USPs)
[O que diferencia este jogo de outros do mesmo gênero.]

- O sistema de combate em um grid por turno onde cada ação do jogador gera uma reação.
- ...

---

## 2. Jogabilidade

### 2.1 Core Loop

Run infinita, sem condição de vitória: a run é uma sequência de ondas cada vez mais difíceis e termina com a morte do jogador. O foco é a construção de build ao longo da run.

```
Iniciar onda (inimigos no grid)
  → Agir por turnos (mover / atacar / usar skill)
  → Derrotar inimigos → ganhar XP
  → Subir de nível → recupera vida → o jogo oferece 3 talentos da classe
  → Escolher 1 (ou usar reroll / banir / pular)
  → A build evolui e muda a forma de jogar com a classe
  → Limpar a onda (ou esgotar o limite de turnos) → próxima onda, mais difícil, sem pausa
  → A dificuldade cresce até superar a build
  → Morte → resumo da build + onda alcançada (recorde) → nova run (outra classe ou outro caminho)
```

### 2.2 Objetivo do Jogador

O objetivo é montar builds diferentes com as árvores de talento de cada classe e descobrir até que onda cada uma consegue chegar.

- **Curto prazo:** limpar a onda atual e escolher o próximo talento.
- **Médio prazo:** formar uma build coerente, com sinergia entre os talentos oferecidos.
- **Longo prazo:** chegar à maior onda possível com cada classe e cada build, explorando caminhos diferentes das árvores.
- **Métrica de sucesso:** onda alcançada, com recorde por classe.

### 2.3 Mecânicas Principais

#### Mecânica 1: Combate em Grid por Turnos
- **Descrição:** O combate acontece num grid de células, com no máximo uma entidade por célula. Cada ação consumida pelo jogador (mover, atacar ou usar skill) é seguida por uma rodada de ações de todos os inimigos. Ações inválidas não consomem o turno. Jogador e inimigos seguem as mesmas regras de alcance e ocupação.
- **Entrada do jogador:** Toque em uma célula (andar ou atacar, conforme o conteúdo e o alcance).
- **Feedback visual/sonoro:** Células destacadas para andar e para atacar, animações de movimento e de ataque, texto de dano flutuante.
- **Interação com outras mecânicas:** Inimigos derrotados dão XP, que alimenta a escolha de talentos. Skills e talentos alteram alcance, dano e comportamento das ações.

#### Mecânica 2: Ondas e Escalonamento
- **Descrição:** A run é uma sequência infinita de ondas. Cada onda é um conjunto de inimigos no grid; ao limpá-la, a próxima começa, mais difícil: inimigos mais fortes e/ou mais numerosos e, em algumas ondas, mecânicas específicas, como elites e chefes. A dificuldade sobe por onda e deve, em algum ponto, superar qualquer build. A onda alcançada é a métrica de sucesso e vira recorde por classe.
- **Ondas especiais:** algumas ondas trazem elites ou chefes com mecânicas próprias, que exigem que o jogador adapte a build e o posicionamento. ❓ Frequência (por exemplo, a cada N ondas), quais mecânicas e quais recompensas.
- **Fim da onda:** a próxima onda chega quando o jogador elimina todos os inimigos **ou** quando passa tempo demais na onda atual (limite de turnos). A segunda condição impede que uma build "enrole" o jogo indefinidamente: a nova onda chega mesmo que ainda haja inimigos vivos. O limite inicial é de **50 turnos do jogador** por onda; o valor será verificado e ajustado durante o desenvolvimento.
- **Entre ondas:** não há pausa, cura nem salvamento. A próxima onda começa em sequência, e a recuperação de vida vem do level up (ver Mecânica 3).
- **Geração de ondas:** as ondas não são escritas à mão; um algoritmo as formula de acordo com o número da onda. Entradas: a *seed* da run, o número da onda e a pool de inimigos do jogo. Saída: quais inimigos entram e em que quantidade. A quantidade é definida pela força de cada inimigo em relação à força esperada para aquele número de onda (inimigos mais fortes entram em ondas mais altas, e os mais fracos aparecem em maior número). A mesma *seed* com o mesmo número de onda gera sempre a mesma onda, o que torna a run totalmente reproduzível (para debug). *Seeds* diferentes geram ondas diferentes, para que o jogador não sinta repetição entre runs. A mesma *seed* governa todos os aspectos aleatórios da run (ondas, ofertas de talento, rerolls e demais sorteios): com a mesma *seed* e as mesmas ações do jogador, a run se repete por completo.
- **Em aberto (geração de ondas):** ❓ função da força esperada por onda, ❓ força de cada inimigo, ❓ regras da pool por onda (onda mínima/máxima de cada inimigo), ❓ regras de elites e chefes e ❓ limite de inimigos pela capacidade do grid.
- **Entrada do jogador:** Nenhuma direta; a próxima onda começa ao limpar a atual ou ao esgotar o limite de turnos.
- **Feedback visual/sonoro:** Indicação da onda atual na HUD, aviso visível do limite de turnos restante e transição entre ondas.
- **Interação com outras mecânicas:** Cada onda derrotada alimenta o XP e, portanto, a escolha de talentos. A escalada de dificuldade é o que testa a build.

#### Mecânica 3: Level Up e Escolha de Talentos (build adaptativa)
- **Descrição:** A cada nível, o jogo oferece 3 talentos sorteados entre os que a classe escolhida pode pegar naquele momento (pré-requisitos cumpridos). O jogador escolhe 1. A build nasce da adaptação ao que aparece, e não de um plano fechado desde o início. O sorteio é ponderado por sinergia: talentos ligados aos já escolhidos têm mais chance de aparecer, para que as builds tendam a se formar sem serem garantidas. Detalhes do sorteio (por exemplo, evitar repetição excessiva ou proteção contra azar) ❓ a definir.
- **Configuração da escolha de talentos:** o número de opções oferecidas (base: 3), de rerolls e de banimentos são configurações da run, não características de classe. Estados ativos podem alterá-las (por exemplo, um estado que concede mais rerolls). ❓ Quantidades base.
- **Recuperação de vida:** ao subir de nível, o jogador recupera vida. É a principal fonte de recuperação durante a run, o que liga o ritmo de XP à sobrevivência. A porcentagem de cura é configurável; o valor inicial é de **100%** da vida máxima, a ser ajustado durante o desenvolvimento.
- **Ferramentas do jogador:** o RNG deve ser controlável pelo jogador (pilar 3). Para isso, o jogador pode **rerrolar** a oferta, **banir** um talento da pool e **pular** a oferta. Custo de uso ❓ a definir. Possibilidade em avaliação: recarregar essas ferramentas assistindo a anúncio recompensado, de forma gratuita para quem comprar a remoção de anúncios (ver seção 7).
- **Fora do escopo desta mecânica:** escolha livre periódica na árvore inteira (descartada). A *seed* da run existe apenas para debug e reprodução de cenários e governa todos os sorteios da run, incluindo as ofertas de talento e a geração de ondas (ver Mecânica 2); não é exposta ao jogador.
- **Entrada do jogador:** Tela de escolha ao subir de nível, com as 3 opções e as ferramentas. Toque para escolher.
- **Feedback visual/sonoro:** Destaque nos talentos que combinam com a build atual; efeito visual de aquisição.
- **Interação com outras mecânicas:** Talentos concedem estados (que alteram atributos, comportamentos e regras do jogo) e liberam skills ativas. As ondas escalam a dificuldade para testar a build.

### 2.4 Mecânicas Secundárias

> **Será revisto no futuro.** A ideia inicial é deixar o jogo base funcionando antes de implementar mais coisas acima desta camada. O que segue abaixo é a direção atual, sujeita a revisão.

#### Mecânica 4: Skills Ativas
- **Descrição:** Cada classe possui um conjunto de skills ativas adquiridas via traits. Skills são ações especiais com cooldown, área de efeito (shape), dano (se ofensivas) e outros parâmetros. São reutilizáveis (cooldown) e vinculadas ao personagem.
- **Entrada do jogador:** Toque em botão de skill na HUD → seleção de alvo.
- **Feedback visual/sonoro:** Animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** Skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo).

### 2.5 Progressão

> **Será revisto no futuro.** As regras abaixo descrevem a direção desejada da progressão; os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.

### 2.5.1 Skills e Níveis

- **Skills de Classe:** São definidas pela classe do personagem e desbloqueadas progressivamente via traits. Cada skill possui:
  - **Nome e descrição**
  - **Tipo** (ofensiva, defensiva ou utilitária)
  - **Dano** (se ofensiva) — valor fixo
  - **Área de efeito** (shape: CIRCLE, CROSS, LINEAR, CONE, ARC, PERPENDICULAR)
  - **Tamanho da área** (ímpar, 1-9)
  - **Alcance** (distância Manhattan do jogador para selecionar alvo)
  - **Cooldown** (em ações do jogador — número de turnos que precisa esperar entre usos)
  - **Ícone**

- **Sistema de cooldown:** Cada skill ativa tem um contador de cooldown que decrementa a cada ação do jogador (movimento ou ataque). Skills não podem ser usadas enquanto o cooldown não zera.

### 2.5.2 Progressão de Conteúdo

- **Spawn por onda:** A tabela de spawn filtra inimigos por onda mínima/máxima. Conforme as ondas avançam, inimigos mais fortes aparecem.
- **Traits por nível:** Traits têm nível requerido e pré-requisitos. Ao subir de nível, o jogador vê 3 traits sorteados entre os disponíveis para a classe, com peso maior para os que têm sinergia com a build atual (ver Mecânica 3 em 2.3).

- **Sistema de progressão:** XP e níveis, árvore de traits e skills por classe
- **Curva de dificuldade:** sobe a cada onda, sem teto, até superar a build do jogador. ❓ Valores e ritmo a definir.
- **Unlocks:** Traits desbloqueiam skills ativas (que entram no repertório do personagem). O avanço das ondas introduz inimigos mais fortes.

### 2.6 Economia
[Recursos, moedas, custos, recompensas.]

| Recurso | Origem | Uso |
|---------|--------|-----|
| [Moeda] | [Drop de inimigos] | [Comprar upgrades] |
| ... | ... | ... |

---


## 3. Personagens e Mundo

### 3.1 Atributos e Estados

Duas camadas descrevem e alteram um personagem. Elas formam o vocabulário que talentos, skills e inimigos usam para modificá-lo:

- **Atributos:** os valores base que configuram o personagem no início da run. Cada classe define os seus (ver 3.2). O valor em jogo é o valor base somado aos estados ativos.
- **Estados:** têm um nome e concedem um ou mais efeitos ao personagem, que incrementam ou reduzem atributos ou modificam comportamentos do jogo. A duração (permanente ou X turnos) não é fixa do estado: depende da situação do jogo ou do talento que o aplica.

**Talentos (traits)** concedem estados, e é por meio deles que modificam o personagem e o jogo. Skills e inimigos também aplicam estados.

As configurações da run (opções de talento, rerolls e banimentos) não são atributos de classe; ver Mecânica 3 em 2.3. Estados ativos podem alterá-las.

❓ Quais atributos e estados também valem para os inimigos.

#### Atributos

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

#### Estados

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

❓ Reaplicação e acúmulo do mesmo estado, remoção antecipada, e o que acontece com os estados ao mudar de onda.

Fora do conjunto por enquanto: esquiva, ganho de XP, cura do level up como atributo (ela é uma configuração global do jogo, ver Mecânica 3 em 2.3) e tamanho da área das skills.

### 3.2 Personagem do Jogador
- **Nome:** ...
- **Descrição:** ...
- **Motivação:** ...
- **Habilidades iniciais:** ...

> Skills de classe e slots de skill fazem parte do escopo "Será revisto no futuro".

- **Skills de classe:** Cada classe começa com 1-2 skills básicas (ex.: Guerreiro começa com "Golpe" — dano em área frontal). Novas skills são desbloqueadas via traits.
- **Slots de skill:** O personagem pode carregar até N skills ativas por vez, selecionadas na tela de classe antes da run (ou durante, via traits).

### 3.3 NPCs / Inimigos
[Liste tipos de inimigos, bosses e NPCs relevantes.]

| Nome | Tipo | Comportamento | Dificuldade |
|------|------|---------------|-------------|
| ... | Inimigo comum | ... | Fácil |
| ... | Boss | ... | Difícil |

### 3.4 Mundo / Cenário
- **Ambientação:** ...
- **Estética:** ...
- **Estrutura de fases/mundo:** [linear, aberto, procedural, fases fixas...]

---

## 4. Controles e Interface

### 4.1 Esquema de Controles
| Ação | Entrada |
|------|--------|
| Mover | [Toque / WASD / Analógico] |
| Atacar | [Toque / Botão] |
| ... | ... |

### 4.2 HUD
[Quais elementos aparecem na tela durante o jogo — vida, pontos, minimapa, etc.]

- ...

### 4.3 Menus
[Liste telas de menu: Principal, Pausa, Opções, Inventário, Fim de partida...]

- ...

---

## 5. Arte e Áudio

### 5.1 Direção de Arte
- **Estilo visual:** [pixel art, 2D vetorial, 3D low-poly...]
- **Paleta de cores:** ...
- **Referências:** ...

### 5.2 Áudio
- **Trilha sonora:** [estilo, ritmo, momentos]
- **SFX:** [lista de efeitos necessários]
- **Locução/Voz:** [sim/não, idioma]


## 6. Skills

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando. As skills detalhadas abaixo representam a direção inicial e serão reavaliados antes da implementação completa.

### 6.1 Definição de Skill

**Skill** é uma ação ativa do personagem, vinculada à classe ou a traits adquiridos. Toda skill tem:

- **Tipo:** Ofensiva (causa dano), Defensiva (escudo/cura/buff), Utilitária (teleporte, troca etc.)
- **Área de efeito:** Define a forma geométrica do efeito no grid (CIRCLE, CROSS, LINEAR, PERPENDICULAR, ARC, CONE).
- **Tamanho da área:** Ímpar, de 1 a 9.
- **Alcance:** Distância Manhattan máxima para seleção do alvo.
- **Dano:** Valor fixo (se ofensiva).
- **Cooldown:** Número de ações do jogador entre usos.

Skills são **reutilizáveis** — após o cooldown, podem ser usadas novamente. Não são consumidas.

### 6.2 Tabela de Skills por Classe

#### Guerreiro
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Golpe | OFENSIVA | 5 | CIRCLE | 1 | 1 | 1 | Inicial |
| Investida com Escudo | OFENSIVA | 12 | LINEAR | 3 | 2 | 3 | Muralha |

#### Mago
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Bola de Fogo | OFENSIVA | 10 | CIRCLE | 3 | — | 3 | Grimório |
| Nova de Gelo | OFENSIVA | 8 | CIRCLE | 3 | 2 | 2 | Criomancia |
| Tempestade Elétrica | OFENSIVA | 14 | CONE | 3 | 3 | 4 | Conjurador de Tempestades |

#### Ladino
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Adaga de Arremesso | OFENSIVA | 6 | CIRCLE | 1 | 3 | 1 | Arsenal Oculto |
| Bomba de Fumaça | OFENSIVA | 5 | CROSS | 1 | 1 | 2 | Mestre das Sombras |
| Adaga Envenenada | OFENSIVA | 7 | LINEAR | 3 | 4 | 2 | Mãos Ligeiras |
| Linha Venenosa | OFENSIVA | 9 | LINEAR | 5 | 5 | 3 | Mestre do Veneno |

---
## 7. Monetização (se aplicável)

- **Modelo:** [Premium, F2P, Ad-supported, IAP]
- **Conteúdo pago:** ...
- **Ética de monetização:** [princípios para evitar pay-to-win, etc.]

---
## 8. Plataforma Técnica

### 8.1 Especificações
- **Engine:** Unity
- **Renderização:** [URP, Built-in...]
- **Resolução alvo:** ...
- **Orientação:** [Retrato / Paisagem]
- **FPS alvo:** ...
- **Estado da run:** se o app for para segundo plano, a run continua de onde parou quando o jogador voltar (o jogo é por turnos, então nada avança sem ele). Se o app for fechado, a run é perdida: não há salvamento de run.

### 8.2 Requisitos mínimos
- ...

### 8.3 Plataformas de publicação
- Steam
- App Store
- Google Play
---

## 9. Acessibilidade

[Liste recursos de acessibilidade planejados.]

- [ ] Tamanho de fonte ajustável
- [ ] Daltonismo (cores alternativas)
- [ ] Legendas
- [ ] Dificuldade ajustável
- [ ] Remapeamento de controles
- [ ] ...

---

## 10. Métricas de Sucesso

[Como medir se o jogo atingiu seus objetivos.]

- **KPIs de jogo:** [retenção D1/D7/D30, tempo de sessão, taxa de conclusão]
- **KPIs de negócio:** [vendas, ARPU, conversão]
- **Metas qualitativas:** [avaliações na loja, feedback de comunidade]

---

## 11. Cronograma e Escopo

### 11.1 Milestones
| Milestone | Data alvo | Entregáveis |
|-----------|-----------|-------------|
| Protótipo jogável | [DD/MM/AAAA] | [Core loop funcional] |
| Alpha | [DD/MM/AAAA] | [Conteúdo principal] |
| Beta | [DD/MM/AAAA] | [Balanceamento e polish] |
| Lançamento | [DD/MM/AAAA] | [Versão 1.0] |

### 11.2 Escopo (Out of Scope)
[O que NÃO será feito nesta versão. Ajuda a controlar feature creep.]

- Itens (consumíveis e itens ativos) — fora do escopo por enquanto; podem voltar em uma versão futura.
- Skills detalhadas — serão revistas no futuro, após o jogo base funcionar.
- ...

---

## 12. Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| [ex.: Engine não suporta X] | Média | Alto | [Plano B] |
| ... | ... | ... | ... |

---

## 13. Glossário

| Termo | Definição                                                                                                                         | Tags   |
| ----- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| Grid  | Tabuleiro de células onde ocorre o combate                                                                                        | #grid  |
| Trait | São mecânicas obtidas a cada nível ganho pelo jogador, eles concedem estados que alteram características, comportamentos e mecânicas de jogo, e liberam skills. | #trait |
| Skill | Ação ativa do personagem, vinculada à classe ou a traits. Possui área de efeito, dano, alcance e cooldown. Pode ser reutilizada. | #skill |
| Atributo | Valor base que configura o personagem no início da run (vida máxima, dano básico, defesa etc.). | #atributo |
| Estado | Condição com nome que concede efeitos ao personagem (incrementa atributos ou modifica comportamentos do jogo), permanente ou por X turnos conforme a situação ou o talento que o aplica. | #estado |
| Turno | Turno global: o jogador joga primeiro e depois jogam os inimigos; termina depois que a última entidade do grid faz sua ação. Cada entidade tem o seu turno dentro dele. | #turno |
