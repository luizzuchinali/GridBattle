---
tags:
  - design
  - gdd
created: 2026-07-12
updated: 2026-10-05
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

> **Documentos de detalhe** (em `docs/design/`): [[mapa_e_nos]], [[xp_e_niveis]], [[talentos_e_oferta]], [[consumiveis]], [[meta_progressao_e_perfil]], [[estados_e_atributos]], [[inimigos]], [[grid_e_terreno]], [[balanceamento_e_geracao]], [[classes_e_skills]], [[interface]] e [[sistema_combate]] (combate implementado). Perguntas em aberto: [[perguntas_em_aberto]]. Cada regra existe em um só lugar: o GDD guarda as decisões em resumo, e o detalhe fica no subdocumento.

---

## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid. A #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A run é um mapa de nós: o jogador escolhe o caminho, enfrenta batalhas no grid e ganha XP ao vencê-las; a cada nível alcançado, escolhe um trait entre as ofertas da classe. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo.

### 1.3 Pillars de Design

1. Cada ação do jogador importa
2. Cada partida deve passar o sentimento de ser única 
3. O rng no jogo deve ser um fator, porém um fator controlável pelo jogador

### 1.4 Unique Selling Points (USPs)

- **Combate em grid por turnos onde cada ação do jogador gera uma reação.** Mover, atacar ou usar uma skill consome o turno, e todos os inimigos respondem em seguida.
- **Build montada durante a batalha.** O talento é escolhido no momento em que a barra de XP cruza o limiar de nível, com o jogo pausado, inclusive no meio da luta (Mecânica 3). O sorteio é controlável pelo jogador: reroll, banir e pular.
- **Risco e recompensa no mapa.** Batalhas mais difíceis rendem mais XP, e a vida persiste entre as batalhas, então o jogador decide quanto arriscar para ficar mais forte (Mecânica 2).
- **Run curta e de uma mão.** Run finita, em retrato, pensada para sessões curtas no celular, com salvamento automático a cada ação (ver 8.1).

---

## 2. Jogabilidade

### 2.1 Core Loop

Run finita em um mapa de nós, no estilo de Slay the Spire: o jogador escolhe o caminho, enfrenta batalhas no grid e termina a run derrotando o chefe final ou morrendo. O foco é a construção de build ao longo da run.

```
Iniciar a run (classe + seed) → mapa de nós
  → Escolher o próximo nó
      • Batalha → combate em grid por turnos (mover / atacar / usar skill)
          → cada inimigo derrotado soma o seu XP na hora (o total da batalha é mostrado na prévia do nó)
          → se o XP alcançar o próximo nível → subir de nível na hora (inclusive no meio da batalha) → o jogo oferece 3 talentos da classe
          → escolher 1 (ou usar reroll / banir / pular)
          → a build evolui e muda a forma de jogar com a classe
      • Nó sem luta (cura, talento com custo, consumível…) → efeito do nó
  → A vida do jogador persiste entre os nós
  → Repetir até o nó do chefe final
  → Chefe final derrotado → vitória
  → Morte em qualquer batalha → fim da run (resumo da build + nível e profundidade alcançados) → nova run (outra classe ou outro caminho)
```

### 2.2 Objetivo do Jogador

O objetivo é montar builds diferentes com as árvores de talento de cada classe e levá-las até o fim do mapa, derrotando o chefe final.

- **Curto prazo:** vencer a batalha atual e escolher o próximo talento ou o próximo nó.
- **Médio prazo:** formar uma build coerente, com sinergia entre os talentos oferecidos, e decidir a rota no mapa (quando lutar, quando curar).
- **Longo prazo:** vencer a run com cada classe e cada build, explorando caminhos diferentes das árvores.
- **Métrica de sucesso:** vitória, nível do jogador e profundidade alcançados, registrados por classe (detalhes em [[meta_progressao_e_perfil]]).

### 2.3 Mecânicas Principais

#### Mecânica 1: Combate em Grid por Turnos
- **Descrição:** O combate acontece num grid de células, com no máximo uma entidade por célula. Cada ação consumida pelo jogador (mover, atacar ou usar skill) é seguida por uma rodada de ações de todos os inimigos. Ações inválidas não consomem o turno. Usar um consumível também não consome a ação (no máximo um por turno; ver 2.8). Jogador e inimigos seguem as mesmas regras de alcance e ocupação.
- **Entrada do jogador:** Toque em uma célula (andar ou atacar, conforme o conteúdo e o alcance).
- **Feedback visual/sonoro:** Células destacadas para andar e para atacar, animações de movimento e de ataque, texto de dano flutuante.
- **Interação com outras mecânicas:** Cada inimigo derrotado concede o seu XP na hora, que leva ao nível e à escolha de talentos. Skills e talentos alteram alcance, dano e comportamento das ações.
- **Sem telegrafia (decisão de design):** os inimigos não anunciam onde vão atacar. O jogo se mantém como está: o impacto vem principalmente da build do jogador, e não de esquivar de golpes. A variedade entre batalhas vem de outras fontes (papéis dos inimigos, personalidade do grid e inimigos que cobram a build; ver Mecânicas 4, 5 e 6).

#### Mecânica 2: Mapa, Nós e Batalhas
- **Descrição:** a run é um mapa de nós, no estilo de Slay the Spire. O jogador escolhe o próximo nó, e um nó de batalha leva ao combate no grid. A run é finita (exemplo, não final: cerca de 30 nós) e termina na vitória, ao derrotar o chefe final, ou na morte.
- **Profundidade e nível:** a profundidade (posição no mapa) define a escala dos inimigos. O nível é do jogador e vem do XP, então varia conforme o caminho escolhido.
- **Risco e recompensa:** cada inimigo derrotado soma o seu XP na hora, e batalhas mais difíceis rendem mais XP. O nível tem um teto fixo, e quem escolhe só batalhas fáceis termina a run com menos níveis.
- **Tipos de nó:** batalha, cura, talento (com custo) e consumível. Outros tipos (elite, evento, chefe) em aberto.
- **Vida:** persiste entre as batalhas e só é recuperada em nós de cura ou por efeitos da build.
- **Fim da batalha:** quando todos os inimigos são eliminados, sem limite de turnos.
- **Prévia do nó:** antes de escolher, o jogador vê o tipo do nó e, nas batalhas, o XP total, os papéis dos inimigos e o terreno.
- **Geração:** as batalhas e o mapa são gerados por algoritmo a partir da seed, de forma reproduzível.
- **Detalhes:** [[mapa_e_nos]] (mapa e nós), [[xp_e_niveis]] (XP, nível e curva) e [[balanceamento_e_geracao]] (geração das batalhas).

#### Mecânica 3: XP, Nível e Escolha de Talentos (build adaptativa)
- **Descrição:** a cada nível alcançado, no momento em que a barra de XP cruza o limiar, o jogo é pausado e oferece 3 talentos sorteados entre os que a classe pode pegar. O jogador escolhe 1. O sorteio é ponderado por sinergia com a build.
- **Controle do RNG (pilar 3):** o jogador pode rerrolar a oferta, banir um talento e pular a oferta. Quantidades e custos em aberto. Valores iniciais configuráveis: 2 rerolls, 1 banimento e 1 pulo por run, sem custo além desse limite (ver [[valores_padrao_em_aberto]]).
- **Vida:** cada nível ganho recupera toda a vida (decisão de balanceamento de 2026-10-06; fração configurável em `ProgressionSettings`). Entre os níveis, a vida persiste entre as batalhas e se recupera nos nós de cura.
- **Detalhes:** [[talentos_e_oferta]] (oferta, reroll, banir e pular) e [[xp_e_niveis]] (XP e nível).

#### Mecânica 4: Papéis dos Inimigos
- **Descrição:** cada inimigo tem um papel (corpo a corpo, enxame, atirador, suporte, invocador, controlador) que define como ele se comporta, em vez de todos perseguirem e atacarem. Seguem as mesmas regras de turno, alcance e ocupação do jogador, sem telegrafar ataques, e a prévia do nó mostra os papéis. Detalhes: [[inimigos]].

#### Mecânica 5: Personalidade do Grid
- **Descrição:** o grid de cada batalha pode ter terreno (obstáculos, células de perigo e de bônus) que muda o posicionamento e o valor das skills. O terreno é gerado pela seed e pela profundidade e aparece na prévia do nó. Detalhes: [[grid_e_terreno]].

#### Mecânica 6: Inimigos que Cobram a Build
- **Descrição:** alguns inimigos exigem uma resposta específica da build (blindado, regenerante, enxame, rápido, espinhoso). Isso incentiva diversificar os talentos e faz o conhecimento do jogo, com o glossário, virar vantagem estratégica. Detalhes: [[inimigos]].

### 2.4 Mecânicas Secundárias

> **Será revisto no futuro.** A ideia inicial é deixar o jogo base funcionando antes de implementar mais coisas acima desta camada. O que segue abaixo é a direção atual, sujeita a revisão.

#### Mecânica 7: Skills Ativas
- **Descrição:** cada classe tem skills ativas adquiridas via talentos: ações especiais com cooldown, área de efeito e dano, reutilizáveis. Uso: toque no botão da skill na barra inferior e depois na célula alvo. Detalhes: [[classes_e_skills]].

### 2.5 Progressão

> **Será revisto no futuro.** Os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.

- **Sistema de progressão:** XP por inimigo derrotado e nível do jogador (a profundidade no mapa é um eixo separado), árvore de talentos e skills por classe. A curva de dificuldade sobe com a profundidade até o chefe final.
- **Detalhes:** [[xp_e_niveis]] (XP e nível), [[talentos_e_oferta]] (talentos) e [[classes_e_skills]] (skills).

### 2.6 Economia
Não há moeda por enquanto. Os recursos da run são: vida (persiste entre os nós e se recupera em nós de cura), XP (por inimigo derrotado, leva a níveis e talentos), consumíveis (nó de consumível, ver 2.8) e as ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3). O custo do nó de talento ainda é ❓ a definir (ver Mecânica 2). Valor inicial configurável: 15% da vida máxima, sem nunca matar (ver [[valores_padrao_em_aberto]]). Itens ativos e equipáveis estão fora do escopo (ver seção 10).

### 2.7 Glossário do Jogo

> Não confundir com o glossário de termos da seção 11 deste documento. Este é um recurso dentro do jogo.

- **Descrição:** o jogo tem um glossário que o jogador preenche conforme joga, com as classes liberadas (os talentos já escolhidos aparecem, os demais como "?") e os inimigos já enfrentados (atributos, skills e como agem). Persiste entre runs, no perfil do jogador (8.1). Detalhes: [[meta_progressao_e_perfil]].

### 2.8 Consumíveis
- **Descrição:** item de uso único obtido no nó de consumível. Só pode ser usado em batalha, não consome a ação do turno e só um pode ser usado por turno, o que o torna muito valioso. Detalhes: [[consumiveis]].

### 2.9 Balanceamento e Fórmulas
- **Princípio:** a dificuldade vem da profundidade no mapa e o poder do jogador vem do nível (XP). A geração é determinística por seed, então o balanceamento pode ser simulado antes de implementar.
- **Estado atual:** a curva de XP implementada é global e configurável (`ProgressionSettings`): 50 de XP no nível 1→2 mais 25 por nível, com nível máximo 30 e sobra de XP guardada. O XP de cada batalha vem do orçamento de ameaça da profundidade e da dificuldade: orçamento × 8,5 × (1 + 0,07 × (profundidade − 1)), dividido entre os inimigos pela ameaça de cada um. Assim uma batalha fácil sempre dá menos XP que uma normal, e uma normal menos que uma difícil, no mesmo andar. Com 30 andares, a primeira batalha normal dá o nível 2 e a run chega perto do nível 19 antes do chefe. São valores iniciais: a regra final (custo crescente, batalha difícil rendendo cerca de um nível, nível máximo fixo) ainda está em definição. Detalhes: [[balanceamento_e_geracao]], [[xp_e_niveis]] e [[valores_padrao_em_aberto]].

## 3. Personagens e Mundo

### 3.1 Atributos e Estados

Duas camadas descrevem e alteram um personagem:

- **Atributos:** valores base (vida máxima, alcance de movimento e de ataque, dano básico etc.), definidos por classe ou inimigo. O valor em jogo é o base somado aos estados ativos.
- **Estados:** têm um nome e concedem efeitos (alteram atributos ou comportamentos), permanentes ou por X turnos. Talentos, skills e inimigos aplicam estados.
- **Duração:** conta em turnos da entidade que está com o estado, e não de quem o aplicou.
- **Detalhes:** tabela de atributos, catálogo de estados, regra de turno global e exemplos em [[estados_e_atributos]].

### 3.2 Personagem do Jogador

Três classes, implementadas com diferença de vida máxima e de skills (valores provisórios):

| Classe | Vida máxima | Liberação |
|---|---|---|
| Guerreiro (Knight) | 120 | disponível desde o início |
| Mago (Mage) | 80 | ao vencer 15 batalhas |
| Ladino (Rogue) | 90 | ao vencer 30 batalhas |

- **Liberação:** só contam batalhas vencidas, acumuladas entre runs e guardadas no perfil do jogador (8.1). Regras em [[meta_progressao_e_perfil]].
- **Skills:** o personagem carrega até 6 skills, uma por botão da barra de skills (ver [[classes_e_skills]]). Toda classe começa com ao menos uma skill: Golpe (Guerreiro), Nova de Gelo (Mago) e Adaga Envenenada (Ladino); as demais vêm de talentos.
- **Detalhes:** valores, curva de XP implementada e perguntas em aberto em [[classes_e_skills]].

### 3.3 NPCs / Inimigos

Cinco inimigos básicos implementados (Goblin, Rat, Slime, FireSkull e EyeBat), cada um com um papel e um comportamento configurável: corpo a corpo, enxame e atirador. Há também inimigos provisórios para os papéis de suporte, invocador e controlador e para os que cobram a build (blindado, regenerante e espinhoso), e um chefe final provisório (o Rei Goblin, com dois Guardas Goblin). Cada um tem vida, movimento, alcance, dano e um XP concedido ao ser derrotado. Elites ainda não existem. Tabela e detalhes: [[inimigos]]. Cada tipo de inimigo terá skills exclusivas dele.

### 3.4 Mundo / Cenário
- **Ambientação:** masmorra subterrânea. A run é uma descida por salas e corredores (os nós do mapa) até o chefe final, onde as batalhas acontecem em salas de grid. Os inimigos são criaturas da masmorra (goblin, rato, slime, crânio de fogo, morcego-olho e as próximas).
- **Estética:** fantasia sombria leve, em pixel art (ver 5.1). O terreno das batalhas (obstáculos, células de perigo e de bônus, ver Mecânica 5) é o que dá personalidade a cada sala.
- **Estrutura de fases/mundo:** mapa de nós ramificado e gerado por seed, com batalhas no grid (ver Mecânica 2 em 2.3). Um único tema visual do início ao fim: a dificuldade e os inimigos mudam com a profundidade, mas não há regiões visualmente diferentes. ❓ Se isso muda no futuro (por exemplo, variação de cor do grid por trecho do mapa, ver 5.1).
- **Narrativa:** sem história explícita. O mundo é sugerido pela arte e pelos nomes, sem diálogos nem textos de história, o que também evita traduzir narrativa para os três idiomas (ver 8.1).

### 3.5 Talentos

- **Definição:** talentos (traits) são o que o jogador escolhe ao alcançar um nível. Concedem estados e podem liberar skills.
- **Pool:** cada classe tem uma pool maior que o número de escolhas da run (exemplo: cerca de 50 talentos). Em uma run, só 6 talentos que liberam skills podem ser escolhidos.
- **Oferta:** 3 talentos sorteados, com peso maior para os que combinam com a build.
- **Detalhes:** estrutura sugerida (ramos), pré-requisitos e perguntas em aberto em [[talentos_e_oferta]].

## 4. Controles e Interface

Interface em UI Toolkit, em retrato no mobile (ver 8.1). Os controles são os mesmos em mobile e PC (toque ou clique do mouse), sem controle de teclado.

- **Controles:** toque ou clique em uma célula para andar ou atacar; toque no botão de skill e depois na célula alvo para usar a skill; toque no botão de item para usar um consumível; toque longo (mobile) ou clique direito (PC) em uma entidade abre a janela de detalhes. As entradas só são aceitas na vez do jogador.
- **HUD em batalha, de cima para baixo:** barra de XP com o botão de menu/pausa à direita; o grid; a barra de itens (botões circulares de 32×32); a barra de skills (6 botões quadrados de 32×32).
- **Telas:** inicial, seleção de classe, mapa, batalha, escolha de talento (com o jogo pausado), glossário, pausa, opções, fim de run, vitória e remoção de anúncios.
- **Tutorial:** dicas contextuais na primeira run, mostradas uma vez (sugestão).
- **Detalhes:** [[interface]].

---

## 5. Arte e Áudio

### 5.1 Direção de Arte
- **Estilo visual:** pixel art 2D, com câmera Pixel Perfect (PPU 100) e render URP 2D. Resolução de referência em retrato de 216×384. Os personagens e inimigos são sprites animados em tiras de quadros (hoje 36×30 px por quadro); skills e elementos de interface têm folhas de sprite próprias.
- **Tom:** fantasia sombria leve. Fundo escuro e criaturas estranhas (como o crânio de fogo e o morcego-olho), mas legível e sem ser pesado, coerente com a classificação indicativa livre.
- **Paleta de cores:** paleta limitada e fixa, com um número pequeno de cores compartilhado por todo o jogo, para manter a pixel art coesa e facilitar a criação de conteúdo novo. Na interface já existem o fundo escuro (rgb 27, 27, 27), o amarelo de destaque (#ffcc00) e o dourado do XP. ❓ Número de cores e a paleta final.
- **Legibilidade (prioridades):** a arte deve deixar claro, de relance:
  - **Papéis dos inimigos:** silhueta e cor distintas por papel (corpo a corpo, enxame, atirador, suporte, invocador, controlador, elite), além do ícone de papel sobre o inimigo (Mecânica 4).
  - **Terreno do grid:** obstáculos, células de perigo e células de bônus identificáveis antes de o jogador entrar nelas (Mecânica 5).
  - **Estados e dano:** ícones de estado e texto de dano legíveis no tamanho pequeno da tela.
  - **Dificuldade do nó no mapa:** os nós de batalha fácil, média e difícil, e o XP, distinguíveis visualmente no mapa (Mecânica 2).
- **Referências:** ❓ A definir.
- ❓ Arte do mapa de nós (ícones dos tipos de nó e do caminho), do glossário e das telas de fim de run, e se haverá variação visual do grid conforme a profundidade.

### 5.2 Áudio
Há a infraestrutura de áudio (efeitos por evento, música por contexto e volumes nas Opções), mas ainda nenhum clipe de áudio no projeto.

- **Trilha sonora:** chiptune/sintetizador atmosférico, coerente com a pixel art e com o tom de fantasia sombria leve. A trilha é dividida por contexto: uma faixa calma para o menu e o mapa, outra para as batalhas e outra para o chefe final. A música muda na transição entre mapa e batalha. ❓ Número de faixas de batalha (para não repetir sempre a mesma) e se a música continua durante a pausa da escolha de talento. Valores iniciais configuráveis: qualquer número de faixas de batalha, uma sorteada sem repetir em seguida, e a música continua durante a pausa (ver [[valores_padrao_em_aberto]]).
- **SFX (efeitos necessários):**
  - **Combate:** andar, ataque básico, skill (um som por skill ou por tipo), dano recebido, morte de inimigo, morte do jogador.
  - **Progressão:** ganho de XP (orbs de XP), subida de nível e abertura da tela de escolha de talento, escolha de talento, reroll, banimento, pular.
  - **Estados e terreno:** aplicação e fim de estados (por exemplo, Fraquejado), células de perigo e de bônus.
  - **Mapa:** selecionar e confirmar um nó, cura, consumível.
  - **Interface:** toque em botões, abrir e fechar janelas (inclusive a de detalhes da entidade), transição de tela.
  - **Resultado:** vitória e derrota da run, classe liberada.
- **Locução/Voz:** sem voz. Os personagens se expressam por efeitos sonoros curtos, o que evita gravar e traduzir falas para os três idiomas (ver 8.1).
- **Controles de volume:** o jogador pode ajustar o volume da música e dos efeitos nas Opções (ver 4.3). ❓ Se há botão rápido de silenciar e como o áudio se comporta durante os anúncios (ver seção 7).

## 6. Skills

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando.

Uma skill de classe é uma ação ativa liberada por talentos. Os inimigos têm skills exclusivas de cada tipo (ver [[inimigos]]). Uma skill é reutilizável (cooldown) e tem tipo, área de efeito, tamanho, alcance e dano fixo. O personagem carrega no máximo 6. Definição completa e tabelas por classe (direção de design, ainda não implementadas): [[classes_e_skills]].

## 7. Monetização (se aplicável)

Definição mínima: anúncios e compra de remoção de anúncios.

- **Modelo:** anúncios mais uma compra para removê-los. ❓ Se o modelo é o mesmo no PC e no mobile.
- **Anúncios forçados:** aparecem de tempos em tempos no meio da run, em pausas naturais, como ao sair de uma batalha ou ao sair de um nó sem luta (por exemplo, depois do nó de talento). Nunca durante uma batalha em andamento, o que inclui a escolha de talento feita no meio da batalha ao subir de nível: o anúncio só aparece depois que a batalha termina. ❓ Quais outros momentos de transição.
- **Rampa de frequência:** no início (as primeiras X vezes) os anúncios aparecem com frequência baixa, para dar tempo de o jogador gostar do jogo e não ser atrapalhado. Depois de um tempo, passam à frequência normal, a cada X nós. ❓ Valores de X, duração da fase inicial e se a contagem é por nós, por batalhas vencidas ou por tempo (em avaliação: contar por batalhas e impor um tempo mínimo entre anúncios, porque os nós de cura e de consumível são instantâneos).
- **Anúncio recompensado (opcional):** recarrega as ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3 em 2.3). Só é exibido se o jogador pedir. ❓ Limite de recargas e se ele pode ser pedido durante a pausa da escolha de talento no meio da batalha (é uma ação iniciada pelo jogador, mas ocorre em batalha).
- **Remoção de anúncios:** compra que remove os anúncios forçados e recarrega as ferramentas de oferta de talento sem precisar assistir a anúncio (ver Mecânica 3). ❓ Preço e se qualquer compra também remove os anúncios forçados.
- **Ética de monetização:** evitar pay-to-win. ❓ Definir um teto de recargas igual para quem assiste a anúncios e para quem comprou a remoção, para que o dinheiro não compre poder.
- **Em avaliação, fora da definição mínima:** classes adicionais (além das três, que são liberadas jogando; ver 3.2), cosméticos e expansões de conteúdo.

---
## 8. Plataforma Técnica

### 8.1 Especificações
- **Engine:** Unity
- **Renderização:** URP 2D, com câmera Pixel Perfect (PPU 100) e interface em UI Toolkit.
- **Resolução alvo:** resolução de referência de 216×384 em retrato, escalada para a tela do dispositivo (ver 5.1). ❓ Como a escala se comporta em telas de proporção diferente de 9:16 e no PC (por exemplo, escala inteira com bordas).
- **Orientação:** retrato no mobile. ❓ Janela e proporção no PC (sugestão: janela em retrato ou com a interface centralizada).
- **FPS alvo:** 60 FPS no mobile (já configurado no projeto).
- **Estado da run:** a run é salva automaticamente entre as ações do jogador, tanto no meio da batalha quanto na tela do mapa. Se o app for para segundo plano ou for fechado, o jogador retoma exatamente do ponto em que parou. O estado salvo inclui a posição dos sorteios da *seed* (por exemplo, críticos e ofertas de talento), para que recarregar não mude o resultado das ações. A escolha de talento pendente (a oferta de 3 talentos, quando o jogo está pausado por uma subida de nível) também é salva, com a mesma oferta, para que fechar e reabrir o app não permita sortear de novo. Não há salvamento manual. ❓ Detalhes do formato do salvamento e o que acontece se um anúncio estiver pendente ao fechar.
- **Idiomas:** o jogo suporta vários idiomas. Os idiomas iniciais são inglês, espanhol e português do Brasil. Todo texto exibido ao jogador (interface, nomes e descrições de talentos, skills, estados e inimigos, glossário do jogo e mensagens) deve ser traduzível, e não fixo no código ou nos assets. ❓ Idioma padrão na primeira abertura (sugestão: o idioma do sistema, com inglês como padrão quando não houver tradução), se a troca de idioma vale na hora ou só ao reabrir, e quem faz as traduções. Valores iniciais: idioma do sistema na primeira abertura, com inglês como padrão, e a troca vale na hora (ver [[valores_padrao_em_aberto]]).
- **Perfil do jogador:** as classes liberadas, o contador de batalhas vencidas e o glossário persistem entre runs, em um salvamento de perfil separado do da run (detalhes em [[meta_progressao_e_perfil]]).

### 8.2 Requisitos mínimos
- **Android:** Android 8.0 (API 26) ou superior (configuração atual do projeto).
- **iOS:** iOS 15.0 ou superior (configuração atual do projeto).
- ❓ Requisitos mínimos de PC (sistema operacional, memória e placa de vídeo) e a lista de aparelhos de teste.

### 8.3 Plataformas de publicação
- Steam
- App Store
- Google Play
---

## 9. Métricas de Sucesso

Esta seção cobre apenas as métricas de design, usadas para balancear o jogo. Métricas de negócio (vendas, conversão, receita por usuário) e metas qualitativas (notas nas lojas, feedback da comunidade) não serão definidas neste documento.

- **Dificuldade e progressão:**
  - Em que profundidade o jogador morre (distribuição por classe).
  - Taxa de vitória por classe e por build.
  - Nível do jogador na morte e no chefe final.
- **Escolha de risco e recompensa:**
  - Proporção de batalhas fáceis, médias e difíceis escolhidas no mapa, e a vida do jogador ao entrar em cada tipo.
  - Se quem escolhe só as fáceis chega ao chefe final e vence (para avaliar a faixa de níveis, ver Mecânica 2).
  - Uso dos nós de cura, de talento e de consumível.
- **Talentos e builds:**
  - Quais talentos são escolhidos e quais são ofertados e recusados.
  - Uso de reroll, banir e pular.
  - Combinações de talentos mais frequentes e as que mais vencem.
- **Inimigos:**
  - Quais inimigos mais causam dano e mais matam o jogador.
  - Tempo e turnos por batalha (para identificar batalhas que se arrastam).
- **Liberação de classes:** quantas runs o jogador leva para liberar o Mago e o Ladino (ver 3.2).

❓ Como os dados são coletados e armazenados (ferramenta de análise), o consentimento de privacidade necessário e se os dados são só locais (para teste interno) ou enviados para um servidor. Valor inicial: só locais, gravados em `metrics.jsonl` e ativados (ver [[valores_padrao_em_aberto]]).

---

## 10. Fora do Escopo

- Itens ativos e equipáveis — fora do escopo por enquanto; podem voltar em uma versão futura. Os consumíveis (nó de consumível no mapa) entram no escopo.
- Skills detalhadas — serão revistas no futuro, após o jogo base funcionar.
- Acessibilidade — nenhum recurso planejado no momento; será revista quando o jogo base estiver funcionando.
- Cronograma e riscos — não serão tratados neste documento.

---

## 11. Glossário

| Termo | Definição                                                                                                                         | Tags   |
| ----- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| Grid  | Tabuleiro de células onde ocorre o combate                                                                                        | #grid  |
| Trait | São mecânicas obtidas a cada nível alcançado pelo jogador, eles concedem estados que alteram características, comportamentos e mecânicas de jogo, e liberam skills. | #trait |
| Talento | Sinônimo de trait na linguagem do jogo (o jogador escolhe talentos ao alcançar um nível). | #trait |
| Skill | Ação ativa do personagem, vinculada à classe ou a traits. Possui área de efeito, dano, alcance e cooldown. Pode ser reutilizada. | #skill |
| Atributo | Valor base que configura o personagem no início da run (vida máxima, dano básico, defesa etc.). | #atributo |
| Estado | Condição com nome que concede efeitos ao personagem (incrementa atributos ou modifica comportamentos do jogo), permanente ou por X turnos conforme a situação ou o talento que o aplica. | #estado |
| Turno | Turno global: o jogador joga primeiro e depois jogam os inimigos; termina depois que a última entidade do grid faz sua ação. Cada entidade tem o seu turno dentro dele. | #turno |
| Run | Uma partida completa: da escolha da classe até a vitória (chefe final derrotado) ou a morte. | #run |
| Classe | Personagem jogável, com atributos e talentos próprios (Guerreiro, Mago e Ladino). | #classe |
| Seed | Número que define todos os sorteios da run (mapa, batalhas, ofertas de talento). A mesma seed e as mesmas ações repetem a run. | #seed |
| Consumível | Item de uso único, obtido no nó de consumível. | #consumível |
| Nó | Ponto do mapa da run. Pode ser uma batalha, um nó de cura, de talento (com custo), de consumível, entre outros. | #nó |
| Mapa | Estrutura de nós da run, no estilo de Slay the Spire. O jogador escolhe o caminho até o chefe final. | #mapa |
| Batalha | Combate em grid por turnos iniciado ao escolher um nó de batalha. Os inimigos derrotados nela concedem XP, que pode levar a um novo nível e à escolha de um talento. | #batalha |
| Profundidade | Quão longe o jogador está no mapa. Define a escala dos inimigos. | #profundidade |
| Nível | Nível do jogador, obtido com XP de batalhas. Cada nível alcançado concede a escolha de um talento. | #nível |
| XP | Valor de cada inimigo, somado ao jogador na hora em que ele é derrotado. O total possível da batalha é mostrado na prévia do nó. Leva ao próximo nível. | #xp |
