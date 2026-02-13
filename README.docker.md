# Rodando PlanWriter com Docker (Front + API + SQL Server)

## Pré-requisitos
- Docker Desktop em execução

## 1) (Opcional) Configure variáveis
Use `.env.docker.example` como base:

```bash
cp .env.docker.example .env
```

Você pode ajustar:
- `MSSQL_SA_PASSWORD`
- `API_PORT` (padrão `5001`)
- `FRONTEND_PORT` (padrão `5173`)

## 2) Subir stack completa
Na raiz do backend (`PlanWriter`):

```bash
docker compose up -d --build
```

Serviços:
- `sqlserver` (interno na rede Docker)
- `sql-init` (one-shot: cria `PlanWriterDb` e tabelas)
- `api` (`http://localhost:${API_PORT}`)
- `frontend` (`http://localhost:${FRONTEND_PORT}`)

## 2.1) Acesso de outro Mac na mesma rede (cliente)
No Mac servidor, descubra seu IP local:

```bash
ipconfig getifaddr en0
```

Se vazio, tente:

```bash
ipconfig getifaddr en1
```

No Mac cliente, acesse:

```text
http://IP_DO_MAC_SERVIDOR:${FRONTEND_PORT}
```

Exemplo:

```text
http://192.168.15.36:5173
```

O frontend agora chama a API internamente por `/api` (proxy), então não depende de `localhost` no Mac cliente.

## 3) Verificar status/logs
```bash
docker compose ps
docker compose logs -f api
```

## 4) Parar tudo
```bash
docker compose down
```

Para remover também o volume do SQL (dados):
```bash
docker compose down -v
```

## CI/CD no GitHub

Para configurar a esteira de deploy com GitHub Actions, veja:

- `docs/deploy-github-actions.md`
