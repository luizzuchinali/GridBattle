---
tags:
  - design
  - talentos
status: em definição (implementado, com talentos provisórios e valores iniciais configuráveis)
updated: 2026-10-06
---
# Talentos e Oferta

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 3 (seção 2.3) e em 3.5. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Escolha de talentos ao subir de nível (Mecânica 3)
- **Descrição:** O jogador ganha XP a cada inimigo derrotado. A cada nível alcançado, no momento em que ele ocorre (inclusive no meio da batalha), o jogo oferece 3 talentos sorteados entre os que a classe escolhida pode pegar naquele momento (pré-requisitos cumpridos). O jogador escolhe 1. A build nasce da adaptação ao que aparece, e não de um plano fechado desde o início. O sorteio é ponderado por sinergia: talentos ligados aos já escolhidos têm mais chance de aparecer, para que as builds tendam a se formar sem serem garantidas. Detalhes do sorteio (por exemplo, evitar repetição excessiva ou proteção contra azar) ❓ a definir. Valor inicial configurável: sorteio sem repetição dentro da oferta, com peso base × (1 + 0,5 × número de tags em comum com os talentos já escolhidos) e sem proteção contra azar (ver [[valores_padrao_em_aberto]]).
- **Configuração da escolha de talentos:** o número de opções oferecidas (base: 3), de rerolls e de banimentos são configurações da run, não características de classe. Estados ativos podem alterá-las (por exemplo, um estado que concede mais rerolls). ❓ Quantidades base. Valores iniciais: 3 opções por oferta (até 5 com estados), 2 rerolls, 1 banimento e 1 pulo por run (ver [[valores_padrao_em_aberto]]).
- **Ferramentas do jogador:** o RNG deve ser controlável pelo jogador (pilar 3). Para isso, o jogador pode **rerrolar** a oferta, **banir** um talento da pool e **pular** a oferta. Custo de uso ❓ a definir. Valor inicial: sem custo além do limite da run; banir tira o talento da run e a mesma posição recebe um novo sorteio. ❓ O que acontece com o nível ao pular a oferta (sugestão: o nível é perdido, sem talento). Valor inicial: o talento daquele nível é perdido. Possibilidade em avaliação: recarregar essas ferramentas assistindo a anúncio recompensado, de forma gratuita para quem comprar a remoção de anúncios (ver seção 7).
- **Fora do escopo desta mecânica:** escolha livre periódica na árvore inteira (descartada). A *seed* governa também as ofertas de talento (ver [[balanceamento_e_geracao]]).
- **Entrada do jogador:** Tela de escolha que aparece no momento em que o jogador alcança um nível (inclusive no meio da batalha, com o jogo pausado) e no nó de talento, com as 3 opções e as ferramentas. Toque para escolher.
- **Feedback visual/sonoro:** Destaque nos talentos que combinam com a build atual; efeito visual de aquisição.
- **Interação com outras mecânicas:** Talentos concedem estados (que alteram atributos, comportamentos e regras do jogo) e liberam skills ativas. As batalhas, cada vez mais difíceis, testam a build.

## Talentos (3.5)
- **Definição:** talentos (traits) são o que o jogador escolhe ao alcançar um nível. Cada talento concede estados e pode liberar skills (3.1 e Mecânica 3). O talento é o principal meio de construir a build.
- **Pool por classe:** cada classe tem a sua pool de talentos, maior que o número de escolhas de uma run (exemplo: cerca de 50 talentos, com nível máximo 30 mais o nó de talento; Mecânica 2). Cada run usa só uma parte da pool.
- **Estrutura (sugestão, não decidida):** a proposta em [[proposta_conteudo_classes_skills_estados_talentos]] organiza cada classe em três ramos de cinco talentos (entrada, reforços, transição e um talento-chave que muda a forma de jogar), cinco talentos avulsos e talentos compartilhados entre as classes. Os ramos propostos reaproveitam as traits do GDD: Guerreiro (Muralha, Berserker, Comandante), Mago (Grimório, Criomancia, Conjurador de Tempestades) e Ladino (Mestre do Veneno, Mestre das Sombras, Arsenal Oculto).
- **Pré-requisitos:** talentos têm nível requerido e pré-requisitos (2.5.2). Na proposta, o talento-chave só entra na oferta depois de pelo menos três talentos do ramo.
- **Limite de talentos de skill:** em uma run, o jogador só pode liberar 6 talentos que liberam skills, o número de botões da barra de skills (3.2 e 4.2). ❓ Como a oferta se comporta depois do sexto (sugestão: talentos que liberam skills deixam de ser oferecidos). Valor inicial: deixam de ser oferecidos.
- **Oferta:** três talentos sorteados entre os disponíveis, com peso maior para os que combinam com a build (Mecânica 3).
- **Descrição técnica (sugestão):** cada talento é descrito por uma gramática de gatilho, condição e ação, e por tags de sinergia que alimentam o peso do sorteio (proposta em `docs/propostas`).
- **Glossário:** os talentos já escolhidos pelo jogador aparecem no glossário, e os demais como "?" (2.7).
- **Aviso:** a proposta em `docs/propostas` ainda descreve ondas e o modelo de XP antigo. A parte de talentos, estados e inimigos continua válida como ponto de partida; a parte de ondas deve ser refeita para o modelo de nós.
- ❓ Lista final de talentos e seus valores, a pool compartilhada, se um talento pode ter mais de um nível (o mesmo talento escolhido de novo), e quantos talentos por classe. Valores iniciais, todos provisórios: 8 talentos que liberam as skills da tabela 6.2 (um nível cada) e 15 talentos genéricos compartilhados (2 a 3 níveis cada) (ver [[valores_padrao_em_aberto]] e [[sistema_combate]]).

## Identidade de classe nos talentos (balanceamento G3, 2026-10-06)
- **Problema:** as classes tinham poucos talentos próprios (2 a 5, quase todos "libera uma skill") e os 15 talentos genéricos, um por atributo, dominavam as ofertas; as classes pareciam iguais e o Ladino era muito mais fraco que as outras.
- **Decisão do usuário:** "modificadores + passivas exclusivas". Cada classe ganha talentos que **modificam as próprias skills** (dano, área, alcance, cooldown, distância de empurrão, duração dos estados e efeitos extras) e passivas exclusivas que dependem da situação; os genéricos passam a pesar menos.
- **Estrutura (valores provisórios, tabelas em [[sistema_combate]]):**
  - Guerreiro (brigão da linha de frente): 14 talentos, 24 postos. Modificam Golpe (dano, largura, empurra 1), Investida com Escudo (recarga, dano, alcance) e Golpe de Escudo (empurra mais, expõe, atordoa). Passivas: bônus por inimigo adjacente, menos dano recebido por inimigo adjacente, cura ao matar e dano de colisão maior.
  - Mago (controle de área): 17 talentos, 25 postos. Modificam Nova de Gelo (congela, recarga, área), Bola de Fogo (dano, queima, área), Tempestade Elétrica (dano, cone) e Rajada de Vento (área, empurra mais). Passivas: bônus contra alvos com estado nocivo, bônus de skills ao ficar parado e cooldowns menores ao matar.
  - Ladino (assassino e veneno): 23 talentos, 42 postos. O veneno (estado que acumula, com dano que cresce com a vida máxima do inimigo e que enfraquece o envenenado) vem das skills (Lâminas Envenenadas) e do ataque básico (Arma Envenenada); modificam as adagas, a Bomba de Fumaça, o Gancho (puxa mais, atordoa) e a Linha Venenosa. Passivas: bônus contra envenenados, depois de andar e contra inimigos isolados, cura ao matar, redução de dano contra inimigos com estado nocivo e vida máxima.
- **Pools:** cada classe passa de 2–5 para 14–23 talentos próprios (24–42 postos) além dos 41 postos genéricos; com ~20 escolhas por run, a oferta nunca seca (conferido do nível 2 ao 30 nas três classes). Os talentos que liberam skill continuam sendo a porta de entrada: a cadeia de pré-requisitos faz a skill liberada abrir os talentos que a melhoram, e os níveis mínimos vão de 2 a 13 para ainda haver opções novas no fim da run.
- **Peso dos genéricos:** `TalentOfferSettings.sharedPoolWeightMultiplier` (0,5) multiplica o peso dos talentos que só chegam à classe pelo pool compartilhado; os talentos da classe passam a ser 61% a 83% do peso de uma oferta (nível 12, build vazia).
- **Tags de sinergia novas:** Brigão (Guerreiro) e Elemental (Mago); o Ladino usa Veneno e Sombra.
- **Ainda provisório:** nomes, textos, ícones, valores e pré-requisitos; o ajuste fino da dificuldade é do G6.
