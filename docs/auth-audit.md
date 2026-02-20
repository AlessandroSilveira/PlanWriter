# Auth audit trail

## Eventos auditados
- `Register`: sucesso/falha
- `Login`: sucesso/falha
- `Refresh`: sucesso/falha
- `Logout`: sucesso
- `Lockout`: bloqueio/ativação
- `ChangePassword`: sucesso/falha

## Metadados persistidos
- `UserId` (quando disponível)
- `IpAddress`
- `UserAgent`
- `TraceId`
- `CorrelationId` (`X-Correlation-Id`)
- `CreatedAtUtc`
- `Result`
- `Details` (somente códigos técnicos, sem senha/token)

## Consulta administrativa
- Endpoint: `GET /api/admin/security/auth-audits`
- Filtros:
  - `fromUtc`, `toUtc`
  - `userId`
  - `eventType`
  - `result`
  - `limit`

## Política de retenção
- Configuração: `AuthAudit:RetentionDays` (padrão `180`)
- Configuração: `AuthAudit:MaxReadLimit` (padrão `500`)
- Uso operacional recomendado:
  - manter dados por 180 dias para investigação de incidentes
  - anonimizar/exportar antes de descarte, quando exigido por compliance
