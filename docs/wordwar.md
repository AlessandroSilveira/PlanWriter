# Word War (Backend): funcionamento completo

Este documento descreve como o Word War funciona hoje no backend do PlanWriter.

## Objetivo

Word War e uma rodada curta dentro de um evento, focada em disputa por contagem de palavras em tempo real (sem chat).

## Modelo de estados

Status da rodada:

- `Waiting` (0): aguardando inicio
- `Running` (1): em andamento
- `Finished` (2): finalizada

Transicoes validas:

- `Waiting` -> `Running` (start)
- `Running` -> `Finished` (finish)

Regras de transicao:

- so pode iniciar se estiver `Waiting`
- so pode finalizar se estiver `Running`
- so pode entrar/sair da rodada enquanto `Waiting`
- checkpoint so e aceito enquanto `Running`

## Fluxo funcional

1. Usuario cria rodada no detalhe do evento, informando `durationMinutes`.
2. Rodada nasce em `Waiting`.
3. Participantes entram na rodada escolhendo um projeto proprio.
4. Usuario autorizado inicia a rodada (`Running`), definindo inicio/fim efetivos.
5. Participantes enviam checkpoints (`wordsInRound`).
6. Ao terminar o tempo (ou finalizacao manual), rodada vira `Finished`.
7. Ranking final e persistido (`FinalRank`) para consolidar resultado.

## Regras de negocio principais

### Criacao (`Create`)

- `durationMinutes` deve ser `> 0`
- evento precisa existir
- evento precisa estar ativo (`IsActive = true`)
- data atual precisa estar dentro da janela do evento
- nao pode existir outra rodada `Waiting` ou `Running` no mesmo evento

### Entrada (`Join`)

- rodada precisa existir
- rodada precisa estar `Waiting`
- projeto precisa pertencer ao usuario
- operacao idempotente:
  - se usuario ja participa, retorna sucesso sem duplicar
  - se houve corrida e outro request inseriu antes, tambem retorna sucesso

### Saida (`Leave`)

- rodada precisa existir
- rodada precisa estar `Waiting`
- se usuario nao participa, retorna sucesso (idempotente)

### Checkpoint (`SubmitCheckpoint`)

- `wordsInRound >= 0`
- rodada precisa existir e estar `Running`
- usuario precisa participar da rodada
- nao permite reduzir valor (`wordsInRound` menor que anterior)
- mesmo valor e tratado como idempotente (sucesso sem alterar)
- se tempo acabou (`UtcNow >= EndsAtUtc`):
  - executa auto-finish
  - persiste ranking final
  - rejeita checkpoint da requisicao atual

### Placar (`Scoreboard`)

- se rodada estiver `Running` e o tempo acabou, executa auto-finish antes de responder
- retorna metadados da rodada + participantes ordenados

Ordenacao do ranking:

1. `WordsInRound DESC`
2. `LastCheckpointAtUtc ASC`
3. `JoinedAtUtc ASC`

## Endpoints HTTP

Base route: `/api/events`

### Criar rodada

- `POST /api/events/{eventId}/wordwars`
- body:

```json
{
  "durationMinutes": 15
}
```

### Rodada ativa por evento

- `GET /api/events/{eventId}/wordwars/active`

### Entrar na rodada

- `POST /api/events/wordwars/{warId}/join`
- body:

```json
{
  "projectId": "GUID_DO_PROJETO"
}
```

### Sair da rodada

- `POST /api/events/wordwars/{warId}/leave`

### Iniciar rodada

- `POST /api/events/wordwars/{warId}/start`

### Finalizar rodada

- `POST /api/events/wordwars/{warId}/finish`

### Enviar checkpoint

- `POST /api/events/wordwars/{warId}/checkpoint`
- body:

```json
{
  "wordsInRound": 1234
}
```

### Obter placar

- `GET /api/events/wordwars/{warId}/scoreboard`

## Persistencia no banco

Tabela de rodada: `EventWordWars`

- `Id`
- `EventId`
- `CreatedByUserId`
- `Status`
- `DurationInMinutes`
- `StartAtUtc`
- `EndAtUtc`
- `CreatedAtUtc`
- `FinishedAtUtc`

Tabela de participantes: `EventWordWarParticipants`

- `Id`
- `WordWarId`
- `UserId`
- `ProjectId`
- `JoinedAtUtc`
- `WordsInRound`
- `LastCheckpointAtUtc`
- `FinalRank`

Observacao de compatibilidade:

- o backend ja trata ambientes onde `Status` esta como inteiro ou texto
- no `Join`, `LastCheckpointAtUtc` e preenchido para evitar falha em bancos legados com `NOT NULL`

## Erros e retorno esperado

Mapeamento geral no middleware:

- regra de negocio invalida -> `400`
- recurso nao encontrado -> `404`
- erro nao tratado -> `500` com mensagem generica

Mensagens de regra sao intencionais para a UI exibir feedback amigavel.

## Troubleshooting rapido (staging)

Ver logs do backend:

```bash
docker logs --tail=200 planwriter-stg-api
```

Ver schema real de participantes no SQL Server:

```bash
docker exec -it planwriter-stg-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Str0ng!Senha2024' -d PlanWriterDb \
  -Q "SELECT c.name, t.name AS type_name, c.is_nullable FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID('dbo.EventWordWarParticipants') ORDER BY c.column_id;"
```

## Checklist de teste manual

1. Criar rodada com duracao valida.
2. Entrar com projeto valido.
3. Iniciar rodada e confirmar timer.
4. Enviar checkpoints crescentes.
5. Validar ordenacao do placar.
6. Finalizar rodada e conferir `FinalRank`.
7. Testar idempotencia (entrar de novo e checkpoint igual).
