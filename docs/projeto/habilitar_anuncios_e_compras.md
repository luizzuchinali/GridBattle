---
tags:
  - projeto
  - guia
  - monetizacao
created: 2026-10-12
---
# Como habilitar os anúncios e a compra (passo a passo)

O código da monetização já está pronto ([[monetizacao]]). Este documento lista **o que só você pode fazer**:
- criar as contas e os produtos nos painéis;
- copiar as chaves para o projeto;
- configurar os builds;
- testar no celular.

**Sem estes passos, o celular roda o jogo normalmente, só que sem anúncios e sem a compra.** No Editor
continua tudo funcionando com os anúncios e a loja de teste.

Ordem sugerida: [[#1. Contas e decisões]] → [[#2. LevelPlay (anúncios)]] → [[#3. Google Play (Android)]] →
[[#5. Build e teste no Android]] → [[#4. App Store (iOS)]] → [[#6. Build e teste no iOS]] →
[[#7. Privacidade e regras das lojas]] → [[#9. Checklist antes de publicar]].

> Os nomes de menus dos painéis (LevelPlay, Play Console, App Store Connect) mudam de tempos em tempos. Se
> algum não bater, procure pelo assunto dentro do painel.

## 1. Contas e decisões

1. **Contas:**
   - **Unity:** a mesma conta do Unity Hub serve para o LevelPlay e para a Unity Cloud.
   - **Google Play Console:** conta de desenvolvedor, com taxa de registro única.
   - **Apple Developer Program:** inscrição anual. Só é necessária para o iOS.
2. **Identificadores do app** (precisam ser iguais em todos os painéis):
   - Android: `com.zuchinalisoftworks.gridbattle` (`Project Settings > Player > Android > Package Name`).
   - iOS: hoje está `com.Zuchinali-Softworks.GridBattle` (`Player > iOS > Bundle Identifier`). Decida o
     definitivo **antes** de criar o app na Apple e no LevelPlay: ele não pode ser trocado depois que o
     app é publicado.
3. **Política de privacidade:** uma página pública (pode ser um site simples) dizendo que o jogo mostra
   anúncios e que as redes de anúncio coletam identificadores do aparelho. As duas lojas pedem esse link.

## 2. LevelPlay (anúncios)

Painel: https://platform.ironsrc.com/ (entre com a conta Unity).

1. **Adicionar os apps:**
   - crie um app para **Android** e outro para **iOS**;
   - se ainda não estão nas lojas, escolha a opção de app não publicado e informe o package name / bundle id
     do passo 1.
2. **Copiar a App Key:** na lista de apps, cada app mostra a *App Key* (um código alfanumérico abaixo do nome).
3. **Criar os blocos de anúncio (Ad Units)** em cada app:
   - um **Interstitial**, usado pelos anúncios forçados;
   - um **Rewarded**, mostrado antes de cada uso de trocar, banir ou pular.

   O valor da recompensa configurado no painel não importa: a recompensa é o próprio uso da ferramenta.
4. **Redes de anúncio:**
   - o pacote já vem com o adaptador do **Unity Ads**;
   - no painel, confira se os blocos têm **instâncias ativas** (redes configuradas). Sem isso, o celular não
     recebe anúncios (*no fill*);
   - outras redes são opcionais. Para adicionar uma, use `Ads Mediation > Network Manager` no Editor e o
     painel do LevelPlay.
5. **Colar no projeto** (`GridBattle > Monetization > Select Settings`):

   | Campo do `MonetizationSettings` | Valor |
   |---|---|
   | Android App Key | App Key do app Android |
   | Android Interstitial Ad Unit Id | id do bloco Interstitial do app Android |
   | Android Rewarded Ad Unit Id | id do bloco Rewarded do app Android |
   | Ios App Key / Ios Interstitial / Ios Rewarded | os mesmos do app iOS |

6. **Conferir:** `GridBattle > Validate Content`. As notas "App Key or ad unit ids are empty" somem quando
   tudo estiver preenchido.
7. **(Recomendado) app-ads.txt:** o LevelPlay indica publicar um arquivo `app-ads.txt` no site do
   desenvolvedor (o mesmo domínio informado nas lojas). Algumas redes pagam menos ou nem compram anúncios sem
   ele. O conteúdo é gerado no painel do LevelPlay.

## 3. Google Play (Android)

No Google Play Console:

1. **Criar o app:** use o package name do passo 1.
2. **Enviar um primeiro build para uma faixa de teste interno:**
   - a loja só mostra o produto e o preço para um app que já foi enviado;
   - o build precisa usar o mesmo package name e ser assinado com a mesma chave das versões seguintes.
3. **Criar o produto:**
   - na área de produtos do app, escolha uma **compra única** (não assinatura);
   - **ID do produto:** `remove_ads`, o mesmo do campo *Remove Ads Product Id* do `MonetizationSettings`. Se
     usar outro id, coloque em *Google Play Product Id*;
   - preencha o nome, a descrição e o **preço**;
   - **ative** o produto.
4. **Testadores:**
   - adicione as contas de teste em **Teste de licença**: elas compram sem cobrança real;
   - adicione também as contas à faixa de teste interno e instale o app pelo link de participação da faixa.
5. **Chave de licença (recomendado):**
   - a chave fica em *Configuração de monetização* (ou *Licenciamento*): é a chave pública RSA em Base64;
   - ela é usada pela Unity Cloud para verificar as transações do Google Play;
   - sem ela, o build Android mostra um aviso do Unity IAP. O jogo funciona do mesmo jeito.
   - Como cadastrar:
     1. no Editor, `Edit > Project Settings > Services` → ligue o projeto a um projeto da Unity Cloud;
     2. em `Services > In-App Purchasing > Google Play`, cole a chave e use **Push License Key to Cloud**.
   - Para esconder o aviso sem a chave: na mesma tela, marque *Ignore Google license key warning in the
     editor logs*.
6. **Declarações do app** (seção de conteúdo do app):
   - **Contém anúncios:** sim;
   - **Segurança dos dados:** declare os dados coletados pelas redes de anúncio (identificadores do aparelho,
     dados de uso) e o uso para publicidade;
   - **ID de publicidade:** o app usa (a permissão AD_ID já está declarada no build);
   - **Público-alvo:** veja [[#7. Privacidade e regras das lojas]];
   - **Classificação indicativa (IARC):** responda que há anúncios e compras no app.

## 4. App Store (iOS)

No App Store Connect:

1. **Acordo de apps pagos:** em *Acordos, impostos e banco*, assine o acordo de **apps pagos** e preencha os
   dados bancários e fiscais. **Sem isso, nenhuma compra funciona, nem no sandbox.**
2. **Criar o app:** use o bundle id do passo 1.
3. **Criar o produto:**
   - em *Compras dentro do app*, crie um **Não consumível**;
   - **ID do produto:** `remove_ads`, ou outro posto em *Apple Product Id*;
   - preencha o nome, a descrição, o preço e a captura de tela para a revisão.
   - Atenção: a primeira compra dentro do app é enviada para revisão **junto com uma versão do app**.
4. **Conta de teste:** em *Usuários e acesso > Sandbox*, crie uma conta de teste.
5. **Privacidade do app:** declare os dados coletados pelas redes de anúncio, o identificador de publicidade
   e o rastreamento, se pedir ATT (ver item 7).

## 5. Build e teste no Android

1. **Modo do Editor:** deixe o *Editor Mode* do `MonetizationSettings` em **Simulated** no dia a dia. Use
   **Sdk** só para ver os anúncios simulados do próprio LevelPlay no Editor (com o alvo Android).
2. **Dependências nativas:** o Mobile Dependency Resolver resolve tudo sozinho no build. Para resolver
   antes, ou se o build reclamar de bibliotecas faltando: `Assets > Mobile Dependency Resolver > Android
   Resolver > Resolve`. Os arquivos ficam em `Assets/Plugins/Android/`; não apague.
3. **Build de teste dos anúncios:**
   1. no `MonetizationSettings`, marque **Launch Test Suite** (só vale em build de desenvolvimento);
   2. em `File > Build Profiles`, marque **Development Build** e gere o APK/AAB;
   3. no celular, o LevelPlay abre a tela de teste: confira se cada rede carrega e mostra intersticial e
      recompensado;
   4. **desmarque Launch Test Suite** quando terminar.
4. **Teste no jogo:**
   - jogue uma run (o primeiro anúncio forçado vem depois de 10 pausas; para testar mais rápido, baixe
     *Grace Nodes* e os intervalos no asset e volte aos valores depois);
   - use trocar, banir e pular: cada um mostra o anúncio e só acontece depois dele;
   - teste também com o celular **sem internet**: o jogo não pode travar; as ferramentas funcionam sem
     anúncio (com o padrão do asset) e os anúncios forçados simplesmente não aparecem.
5. **Teste da compra** (app instalado pela faixa de teste, com uma conta de *Teste de licença*):
   1. abra "Remover anúncios": o preço deve vir da loja;
   2. compre e confira que os anúncios forçados param e as ferramentas deixam de mostrar anúncio;
   3. desinstale, instale de novo e use **Restaurar compras**. A compra também volta sozinha na abertura;
   4. para testar a compra de novo: estorne o pedido de teste no Play Console e use *Opções >
      Desenvolvimento > Resetar o jogo* (só em build de desenvolvimento).

## 6. Build e teste no iOS

1. **Perfil de build:** crie o perfil **iOS** em `File > Build Profiles` (o projeto hoje só tem Android).
2. **CocoaPods:** rode `Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods`. É preciso
   um Mac com Xcode para compilar.
3. **Depois do build, no Xcode:**
   - o `Info.plist` deve ter a lista `SKAdNetworkItems`; o pacote do LevelPlay a coloca sozinho, então só
     confira;
   - os frameworks *AdSupport*, *AppTrackingTransparency* e *StoreKit* devem aparecer (também automáticos).
4. **ATT:** veja o item 7. Sem ele, os anúncios funcionam, mas sem personalização (paga menos).
5. **Testes:**
   - os mesmos do Android (Test Suite, run, ferramentas com anúncio, sem internet);
   - a compra com a conta de **Sandbox**, num aparelho físico.

## 7. Privacidade e regras das lojas

Estas são **decisões suas, ainda pendentes**. Não são orientação jurídica: dependem de onde o jogo será
publicado.

- **Consentimento (GDPR na Europa, LGPD no Brasil):**
  - normalmente pede uma tela de consentimento antes dos anúncios personalizados;
  - o projeto tem os campos *Privacy* no `MonetizationSettings` e passa o valor ao LevelPlay antes de
    iniciar, **mas o valor é fixo**;
  - uma tela de consentimento por jogador exige uma pequena mudança no código: perguntar antes de iniciar o
    LevelPlay e guardar a resposta no perfil. Peça quando decidir.
- **ATT (iOS):**
  - para anúncios personalizados, a Apple exige pedir permissão de rastreamento antes de iniciar o SDK;
  - isso exige um plugin nativo pequeno, um texto de permissão no `Info.plist` e esperar a resposta antes de
    iniciar o LevelPlay. Também é mudança de código: peça quando decidir.
- **Público infantil (COPPA, política Famílias do Google):**
  - se no questionário de público-alvo você incluir crianças (menores de 13), as regras de anúncio ficam
    bem mais restritas;
  - nesse caso, marque *Child Directed = Yes* no `MonetizationSettings`;
  - classificação "Livre" não quer dizer público infantil.
- **CCPA (Califórnia):** quem pede "não vender meus dados" precisa ter isso respeitado. Hoje é um campo fixo
  (*Ccpa Do Not Sell*).

## 8. Onde cada coisa entra no projeto

| O quê | Onde |
|---|---|
| App Key e blocos do LevelPlay | `Settings/Monetization/MonetizationSettings` (seção LevelPlay) |
| ID do produto | mesmo asset (seção Unity IAP) |
| Test Suite e logs dos adaptadores | mesmo asset (só em build de desenvolvimento) |
| Consentimento fixo (GDPR, CCPA, COPPA) | mesmo asset (seção Privacy) |
| Frequência dos anúncios, aviso, oferta | mesmo asset (seções Forced ads e Remove ads offer) |
| Ferramentas com anúncio (ligar/desligar, uso sem anúncio disponível) | `MonetizationSettings` (seção Rewarded); limites por run em `Settings/Talents/TalentOfferSettings` |
| Permissão AD_ID (Android) | `Assets/LevelPlay/Resources/LevelPlayMediationSettings` (já ligada) |
| Chave de licença do Google Play | `Project Settings > Services > In-App Purchasing > Google Play` |
| Redes de anúncio e versões dos adaptadores | `Ads Mediation > Network Manager` |

## 9. Checklist antes de publicar

- [ ] App Key e blocos (Interstitial e Rewarded) preenchidos para cada plataforma publicada.
- [ ] `GridBattle > Validate Content` sem notas de monetização.
- [ ] Blocos com instâncias ativas no LevelPlay; Test Suite mostrou anúncios no aparelho.
- [ ] **Launch Test Suite desmarcado** e build sem *Development Build*.
- [ ] Produto `remove_ads` ativo nas lojas, com o mesmo id do asset; compra e restauração testadas.
- [ ] (Apple) Acordo de apps pagos assinado; a compra enviada junto com a versão.
- [ ] Política de privacidade publicada; declarações de dados, anúncios e público-alvo preenchidas.
- [ ] Decisão sobre consentimento e ATT tomada (e implementada, se for o caso).
- [ ] Teste sem internet: o jogo segue sem travar.
- [ ] (Recomendado) `app-ads.txt` publicado; chave de licença do Google Play na Unity Cloud.

## 10. Problemas comuns

| Sintoma | O que verificar |
|---|---|
| Nenhum anúncio no celular | App Key e ids preenchidos? Instâncias ativas no painel? Internet? Em build de desenvolvimento, o log mostra "LevelPlay SDK initialized successfully"? |
| Anúncio carrega pouco (*no fill*) | Normal no começo e em apps novos; confira as redes no painel e o `app-ads.txt` |
| Build Android falha com dependências | `Assets > Mobile Dependency Resolver > Android Resolver > Resolve` e gere de novo |
| Build iOS falha com CocoaPods | `Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods` (no Mac) |
| "A loja não está disponível agora" | App instalado pela loja (faixa de teste ou TestFlight)? Produto ativo com o mesmo id? Conta de teste logada? (Apple) acordo de apps pagos assinado? |
| Preço não aparece | O produto ainda está sendo processado pela loja (pode levar horas depois de criado) ou o id não bate |
| Compra feita, mas os anúncios voltaram | Use **Restaurar compras**; o jogo também confere com a loja ao abrir |
| No Editor nada aparece com *Editor Mode = Sdk* | O alvo de build precisa ser Android ou iOS; use *Simulated* para o dia a dia |
