---
tags:
  - design
  - consumiveis
status: em definição (não implementado)
updated: 2026-10-03
---
# Consumíveis

> Subdocumento do [[GDD]]. Resumo e decisões principais em 2.8. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## 2.8 Consumíveis
- **Descrição:** um consumível é um item de uso único que não consome o turno, obtido no nó de consumível do mapa (ver [[mapa_e_nos]]). Itens ativos e equipáveis ficam fora do escopo (ver seção 10); só os consumíveis entram.
- **Exemplos (a refinar):** poção de cura, bomba de dano em área, tônico que concede um estado temporário.
- **Armazenamento:** o jogador carrega os consumíveis até um limite, mostrados na barra de itens da batalha (4.2). ❓ Número de espaços (a barra comporta 6 botões de 32 px na resolução de referência) e o que acontece quando o jogador recebe um consumível com os espaços cheios.
- **Uso:** usar um consumível não consome a ação do turno, então o jogador pode usá-lo e ainda mover, atacar ou usar uma skill no mesmo turno. Só um consumível pode ser usado por turno. Isso torna os consumíveis muito valiosos: cada um dá uma vantagem sem custo de ação, e o limite de um por turno evita encadear vários de uma vez. O jogo pode usar essa regra para trabalhar recompensas baseadas em consumíveis (por exemplo, no nó de consumível e como recompensa ou custo de outros nós). Consumíveis só podem ser usados em batalha, nunca no mapa. ❓ Se o consumível precisa de alvo na batalha (por exemplo, a bomba de área) e como o alvo é escolhido.
- **Obtenção:** ❓ Se o jogador recebe um consumível sorteado pela seed ou escolhe entre opções, e se consumíveis também podem vir de outras fontes (por exemplo, como custo ou recompensa do nó de talento).
- **Interação com outras mecânicas:** o consumível de cura divide espaço com o nó de cura (Mecânica 2); estados temporários seguem a regra de duração (3.1). ❓ Lista final, raridade e se os consumíveis aparecem no glossário do jogo (2.7).
