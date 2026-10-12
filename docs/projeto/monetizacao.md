---
tags:
  - projeto
  - guia
  - monetizacao
created: 2026-10-12
---
# Monetização: anúncios e remoção de anúncios

Implementa a seção 7 do [[GDD]]:
- anúncios forçados em pausas naturais da run;
- anúncio recompensado que recarrega as ferramentas de talento;
- compra que remove os anúncios forçados.

Os anúncios vêm do **Unity LevelPlay** (pacote *Ads Mediation*) e a compra do **Unity IAP 5**
(Google Play e App Store). Ver também [[guia_sistemas]] e [[valores_padrao_em_aberto]].

## O que o jogador vê

- **Anúncio forçado (intersticial):**
  - **Quando:** ao voltar ao mapa depois de uma batalha vencida, ao sair de um nó sem luta (cura, talento,
    consumível) e na tela de fim de run.
  - **Nunca:** durante uma batalha, inclusive na escolha de talento que abre no meio dela.
  - **Aviso:** antes do anúncio aparece "Pausa para anúncio — começa em 3...".
- **Frequência:** pouca no começo e normal depois. Os valores estão em [Valores iniciais](#valores-iniciais).
- **Anúncio recompensado (decisão de 2026-10-12):** cada uso das ferramentas de talento (trocar, banir ou
  pular) mostra antes um anúncio recompensado.
  - **O uso só acontece** se o anúncio for assistido até o fim, e conta no limite da run como sempre (2
    trocas, 1 banimento, 1 pulo). Fechado antes do fim, nada acontece e o uso não é gasto.
  - **Banir:** o anúncio aparece ao tocar a carta que será banida, não ao entrar no modo de banir.
  - **Pular:** primeiro a confirmação, depois o anúncio.
  - **Aviso:** uma linha embaixo das ferramentas diz "Cada troca, banimento ou pulo mostra um anúncio".
  - **Sem anúncio disponível** (sem internet, sem anúncio carregado): a ferramenta funciona sem anúncio
    (configurável).
  - **Na batalha:** vale também na escolha de talento do meio da batalha, porque é o jogador quem pede.
- **Remover anúncios:** botão no menu principal, nas Opções e na tela de derrota.
  - **A janela mostra:** o que a compra dá, o preço que vem da loja, "Comprar" e "Restaurar compras".
  - **Oferta automática:** a cada 3 anúncios forçados, a janela abre sozinha depois do anúncio (no máximo
    uma vez por sessão).
- **Depois da compra:** sem anúncios forçados, e as ferramentas funcionam sem anúncio, com o **mesmo
  limite** de quem assiste. Assim o dinheiro não compra poder (GDD 7, ética).
- **No PC:** não há anúncios nem loja, e as ferramentas funcionam sem anúncio.

## Onde configurar

| O quê | Onde |
|---|---|
| Tudo da monetização | `Settings/Monetization/MonetizationSettings` (menu `GridBattle > Monetization > Select Settings`) |
| Usos das ferramentas por run | `Settings/Talents/TalentOfferSettings` (trocas, banimentos e pulos; regra de jogo, vale para todos) |
| Janelas (remover anúncios, aviso, anúncio de teste) | `Settings/Modals/ModalsSettings` |
| Permissão AD_ID do Android | `Assets/LevelPlay/Resources/LevelPlayMediationSettings` (já ligada) |

Campos do `MonetizationSettings`:
- **Platforms:**
  - liga a monetização no Android e no iOS;
  - **Editor Mode** escolhe o que o Editor usa:
    - *Simulated* (padrão): anúncios e loja de teste do próprio jogo;
    - *Sdk*: os SDKs reais, com anúncios simulados do LevelPlay (exige alvo de build Android ou iOS) e a
      loja de teste do Unity IAP;
    - *Off*: como no PC.
- **LevelPlay:**
  - *App Key* e os ids dos blocos de anúncio (intersticial e recompensado) de cada plataforma;
  - *Launch Test Suite* e *Adapters Debug*, que só valem em build de desenvolvimento.
- **Privacy:** consentimento GDPR/LGPD, CCPA e público infantil (COPPA), repassados ao LevelPlay antes de ele
  iniciar. *Not Set* não repassa nada.
- **Unity IAP:** id do produto (`remove_ads`) e ids diferentes por loja, se precisar.
- **Forced ads:**
  - em que momentos;
  - período de carência;
  - fase inicial e frequência normal;
  - tempo mínimo entre anúncios;
  - segundos do aviso.
- **Remove ads offer:** a cada quantos anúncios e quantas vezes por sessão.
- **Rewarded:**
  - *Talent Tools Need Ad*: cada uso das ferramentas mostra um anúncio;
  - *Allow Tools Without Ad*: sem anúncio disponível, a ferramenta funciona mesmo assim. Desligado, ela
    espera um anúncio e a janela avisa que ele está indisponível.
- **Ad playback:**
  - pausa do som durante o anúncio;
  - duração do anúncio de teste;
  - espera e preço da loja de teste;
  - se a "conta" de teste já tem a compra (para testar a restauração).

## Valores iniciais

São as respostas neutras para as perguntas em aberto do GDD 7. Todas podem ser mudadas no asset.

| Pergunta | Valor |
|---|---|
| Modelo no PC | Sem anúncios e sem loja; ferramentas sem anúncio, com o mesmo limite |
| Momentos do anúncio forçado | Depois de batalha vencida, depois de nó sem luta, fim de run |
| Contagem | Por pausa (nó concluído ou run encerrada), guardada no perfil |
| Carência | Nenhum anúncio nas 10 primeiras pausas do jogador |
| Fase inicial | Os 3 primeiros anúncios a cada 6 pausas |
| Frequência normal | A cada 4 pausas |
| Tempo mínimo entre anúncios | 3 minutos (um anúncio devido espera a próxima pausa) |
| Aviso antes do anúncio | 3 segundos, sem como fechar |
| Ferramentas de talento | Cada uso mostra um anúncio recompensado e só acontece se ele for assistido até o fim (decisão do usuário); o limite por run é o de sempre, igual para todos |
| Ferramentas na escolha de talento no meio da batalha | Com anúncio também (é o jogador quem pede) |
| Ferramenta sem anúncio disponível | Funciona sem anúncio (`allowToolsWithoutAd`) |
| Som durante o anúncio | Pausado |
| Anúncio pendente ao fechar o app | Nada é salvo; a ferramenta só é usada depois que o anúncio entrega a recompensa |
| Oferta de remover anúncios | A cada 3 anúncios forçados, no máximo 1 vez por sessão |
| Preço | Definido nas lojas (o jogo mostra o preço que a loja informa) |

## Antes de publicar: o que falta fazer por você

Enquanto os campos do LevelPlay estiverem vazios, o jogo roda **sem anúncios** no celular. O validador de
conteúdo (`GridBattle > Validate Content`) avisa com uma nota.

O passo a passo completo está em [[habilitar_anuncios_e_compras]]:
- contas e identificadores;
- painel do LevelPlay;
- produto no Google Play e na App Store;
- chave de licença;
- builds e testes no celular;
- privacidade (consentimento, ATT, público infantil);
- checklist antes de publicar e problemas comuns.

## Testar no Editor

- Com *Editor Mode = Simulated* (padrão):
  - o anúncio é uma tela preta "Anúncio de teste" que dura 2,5 s, e o botão **Fechar** encerra antes (sem
    recompensa);
  - a compra dá certo depois de 0,6 s.
- **Menu `GridBattle > Monetization > Testing`** (mexe no `profile.json` de quem joga no Editor):
  - *Mark Ads Removed*: marca a compra no perfil;
  - *Show Ads Again*: desmarca a compra;
  - *Reset Forced Ad Counters*: o jogador volta a ser novo (carência e fase inicial de novo).
- Para ver o anúncio forçado logo, baixe a carência e as frequências no asset enquanto testa.
- Com *Editor Mode = Sdk*, os anúncios simulados são do próprio LevelPlay e a loja é a de teste do Unity IAP.
- **Começar do zero:** em build de desenvolvimento (e no Editor), *Opções > Desenvolvimento > Resetar o jogo*
  apaga a run, o perfil (com a compra e os contadores de anúncios) e as métricas. No Editor, isso apaga o
  **seu** `profile.json` e `run.json`. Uma compra feita com uma conta de loja volta na próxima consulta à
  loja: para comprar de novo, estorne o pedido de teste na loja.

## Como funciona por dentro

- **Lógica:** `Gameplay/Monetization`:
  - `MonetizationService` se cria sozinho, como o `AudioManager`, e escolhe os provedores da plataforma;
  - `ForcedAdRules` tem as regras puras da frequência;
  - `LevelPlayAdsProvider` e `UnityIapRemoveAdsStore` são os SDKs reais;
  - as versões simuladas e as vazias ficam em `AdProviders.cs` e `RemoveAdsStores.cs`.
- **O que fica salvo:**
  - no perfil (`profile.json`, `Monetization`): a compra e os contadores;
  - na run: os usos das ferramentas, como sempre (o anúncio não muda nada na regra).
- **Compra:**
  - a loja informa o pedido pendente;
  - o jogo marca a compra no perfil e salva;
  - só então confirma o pedido na loja. Se salvar falhar, o pedido não é confirmado e a loja o entrega de
    novo no próximo início.
  - Na abertura, o jogo pergunta à loja se a compra existe (troca de aparelho, reinstalação).
- **Interface:**
  - o `GameFlowController` mostra o anúncio forçado pendente quando o mapa, a tela de fim de run ou o menu
    estão parados, sem transição nem janela aberta;
  - a janela de talento chama `MonetizationService.PayForTalentToolAsync` antes de trocar, banir ou pular.
    O bot do simulador usa as ferramentas direto pelo `TalentService`, sem anúncio.
- **Simulador de balanceamento e partida de referência:** a monetização fica desligada (`SimMode`). Os
  resultados continuaram idênticos.
