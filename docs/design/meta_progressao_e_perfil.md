---
tags:
  - design
  - meta
status: em definição (implementado, com valores iniciais configuráveis)
updated: 2026-10-05
---
# Meta-progressão e Perfil do Jogador

> Subdocumento do [[GDD]]. Resumo e decisões principais em 2.2, 2.7, 3.2 e 8.1. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Liberação das classes (3.2)
Apenas uma classe está aberta desde o início; as outras duas são liberadas jogando.
- **Guerreiro (Knight):** disponível desde o início.
- **Mago (Mage):** liberado ao vencer 15 batalhas.
- **Ladino (Rogue):** liberado ao vencer 30 batalhas.
- Só contam batalhas vencidas (nós de cura, talento e consumível não entram na contagem).
- A contagem é acumulada entre runs (soma de todas as batalhas vencidas em todas as runs) e fica guardada no perfil do jogador (8.1).
- ❓ Se batalhas vencidas em runs que terminam em derrota contam (sugestão: sim, para o progresso nunca ser perdido) e como avisar o jogador da nova classe liberada (sugestão: tela de fim de run). Valores iniciais configuráveis: contam, e o aviso é dado na tela de fim de run (ver [[valores_padrao_em_aberto]]).

## Glossário do jogo (2.7)

> Não confundir com o glossário de termos da seção 11 do GDD. Este é um recurso dentro do jogo.

- **Descrição:** o jogo tem um glossário que o jogador preenche conforme joga. Ele registra o que o jogador já descobriu, e o que ainda não descobriu aparece escondido. Também serve de referência para o jogador experiente planejar a build (ver Mecânica 6 em 2.3).
- **Seção de classes:** mostra as classes já liberadas. Em cada classe aparecem os talentos que o jogador já escolheu em algum momento do jogo. Os talentos que nunca foram escolhidos aparecem como "?".
- **Seção de inimigos:** mostra todos os inimigos que o jogador já enfrentou. De cada um é possível ver os atributos, as skills e como o tipo de inimigo age (seu papel e comportamento).
- **Persistência:** o glossário vale entre runs. O que foi descoberto fica registrado no perfil do jogador, e não só na run atual.
- ❓ O que conta como descoberto para um talento (apenas escolhido, ou também apenas oferecido), se os inimigos ainda não enfrentados aparecem como "?" na lista, se as informações de cada inimigo são reveladas por etapas (por exemplo, mais detalhes depois de enfrentá-lo mais vezes), quais valores de atributo são exibidos (base ou escalados pela profundidade) e onde o glossário fica no menu (ver 4.3). Valores iniciais configuráveis: só o talento escolhido conta como descoberto, e os inimigos ainda não enfrentados aparecem como "?" (ver [[valores_padrao_em_aberto]]).

## Perfil do jogador (8.1)
- **Perfil do jogador:** as classes liberadas, o contador de batalhas vencidas (3.2) e o glossário (2.7) persistem entre runs, em um salvamento de perfil separado do salvamento da run. ❓ Sincronização entre dispositivos.

## Registro de resultados (2.2)
- **Registro:** vitória, nível do jogador e profundidade alcançados, registrados por classe. ❓ Detalhes do registro (recordes, dificuldades adicionais). Hoje o perfil registra, por classe, as partidas jogadas, as vitórias, o melhor nível e a melhor profundidade.
