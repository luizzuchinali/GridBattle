# Sons do jogo (efeitos)

Os arquivos desta pasta são ligados à Audio Library **pelo nome**, sozinhos (`AudioFolderLinker`). Não é preciso abrir
nenhum asset para trocar um som.

- **Trocar um som:** sobrescreva o arquivo com outro de mesmo nome, ou apague os arquivos do evento e coloque os novos.
- **Nome:** `<Evento>.wav` ou `<Evento>_<qualquer coisa>.wav` (ex.: `Collision_metal.ogg`). Aceita wav, ogg, mp3, aiff;
  maiúsculas e minúsculas tanto faz.
- **Variações:** vários arquivos do mesmo evento viram variações; o jogo sorteia uma a cada vez (nunca a mesma duas
  vezes seguidas).
- **Silenciar um evento:** apague os arquivos dele.
- **Volume, pitch e limites de repetição** de cada evento ficam na Audio Library
  (menu `GridBattle/Audio/Select Audio Library`).
- **Música:** a mesma regra vale para a pasta `Audio/Music`, com os nomes `Menu`, `Map`, `Battle` e `Boss`.
- Arquivo com nome que não bate com nenhum evento é ignorado e aparece no Console. O menu
  `GridBattle/Audio/Relink Sound Folders` refaz a ligação à mão.

Origem dos sons atuais: `EnemyDeath` foi gerado por código; os demais vêm de pacotes da [Kenney](https://kenney.nl)
(Impact Sounds, RPG Audio, Interface Sounds e Music Jingles), licença CC0 (uso livre, sem crédito obrigatório),
recortados e nivelados; dano no jogador, crítico, colisão e morte do jogador ganharam uma camada grave.

| Arquivo | Quando toca |
| --- | --- |
| `Walk` | Um personagem começa a andar |
| `BasicAttack` | Um ataque básico começa (o golpe no alvo usa `EnemyHurt`/`PlayerHurt`) |
| `SkillCast` | Uma habilidade é usada |
| `PlayerHurt` | O personagem do jogador leva dano (no impacto) |
| `EnemyHurt` | Um inimigo leva dano (no impacto) |
| `CriticalHit` | Golpe crítico (no impacto) |
| `EnemyDeath` | Um inimigo morre |
| `PlayerDeath` | O personagem do jogador morre |
| `Collision` | Um personagem empurrado ou puxado bate em algo |
| `StrikePrepare` | Um inimigo prepara um golpe (casas marcadas) |
| `XpOrb` | Os orbes de XP saem de um inimigo morto |
| `LevelUp` | O jogador sobe de nível |
| `TalentOfferOpen` | Abre a escolha de talentos |
| `TalentChosen` | Um talento é escolhido |
| `Reroll` | Rerrolar a oferta de talentos |
| `Ban` | Banir um talento |
| `Skip` | Pular a oferta de talentos |
| `StateApplied` | Um estado (veneno, escudo...) é aplicado |
| `StateExpired` | Um estado acaba |
| `HazardCell` | Pisar numa casa de perigo |
| `BonusCell` | Pisar numa casa de bônus |
| `NodeSelect` | Selecionar um nó do mapa |
| `NodeConfirm` | Entrar num nó do mapa |
| `Heal` | Cura no mapa (descanso) |
| `ConsumableGained` | Ganhar um consumível |
| `ConsumableUse` | Usar um consumível |
| `ButtonTap` | Tocar num botão |
| `WindowOpen` | Abrir uma janela |
| `WindowClose` | Fechar uma janela |
| `ScreenTransition` | Troca de tela |
| `RunVictory` | Vitória na run |
| `RunDefeat` | Derrota na run |
| `ClassUnlocked` | Nova classe desbloqueada |
