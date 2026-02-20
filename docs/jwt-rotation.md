# JWT key rotation plan

## Objetivo
Permitir rotação de chave JWT sem derrubar sessões ativas imediatamente, usando `kid` no header do token.

## Configuração atual
- Chave ativa: `Jwt:Key`
- Identificador da chave ativa: `Jwt:CurrentKid`
- Chaves anteriores aceitas para validação: `Jwt:PreviousKeys[]` (`Kid` + `Key`)
- Janela de tolerância de relógio: `Jwt:ClockSkewSeconds` (curta, padrão 30s)

## Fluxo recomendado de rotação
1. Gerar uma nova chave forte (>= 32 chars) e novo `kid`.
2. Atualizar configuração:
   - mover a chave atual para `Jwt:PreviousKeys`.
   - definir nova chave em `Jwt:Key`.
   - definir novo `kid` em `Jwt:CurrentKid`.
3. Fazer deploy da API.
4. Monitorar autenticação por uma janela igual ao maior tempo de vida de token.
5. Após a janela de expiração dos tokens antigos, remover a chave antiga de `Jwt:PreviousKeys`.

## Observações
- Em produção, a API bloqueia startup se `Jwt:Key` for fraca/default.
- O emissor sempre assina com a chave ativa (`Jwt:CurrentKid`).
- O validador aceita tokens assinados com a chave ativa ou com chaves anteriores configuradas.
