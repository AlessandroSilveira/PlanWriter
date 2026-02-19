# Rodando PlanWriter com Docker (staging + production local)

## Pré-requisitos
- Docker Desktop em execução

## 1) Configure variáveis
Na raiz do backend (`PlanWriter`):

```bash
cp .env.docker.example .env
```

Variáveis principais:
- `MSSQL_SA_PASSWORD`
- `STAGING_API_PORT` (padrão `5003`)
- `STAGING_FRONTEND_PORT` (padrão `5175`)
- `PROD_API_PORT` (padrão `5001`)
- `PROD_FRONTEND_PORT` (padrão `5173`)
- `PROXY_PORT` (padrão `80`)
- `AUTH_BOOTSTRAP_ENABLED` (padrão `false`)
- `AUTH_BOOTSTRAP_ADMIN_EMAIL`
- `AUTH_BOOTSTRAP_ADMIN_PASSWORD` (somente senha forte, minimo 12 com maiuscula/minuscula/numero/simbolo)

## 2) Subir os dois ambientes + proxy local

```bash
docker network create planwriter_gateway || true
docker compose -f docker-compose.staging.yml up -d --build
docker compose -f docker-compose.production.yml up -d --build
docker compose -f docker-compose.proxy.yml up -d
```

## 3) Configurar hostnames locais

Em cada máquina cliente da sua rede, adicione no `/etc/hosts`:

```text
192.168.15.182 planwriter.staging.test
192.168.15.182 planwriter.test
```

Depois acesse:
- `http://planwriter.staging.test` (staging)
- `http://planwriter.test` (production)

## 4) Verificar status/logs

```bash
docker compose -f docker-compose.staging.yml ps
docker compose -f docker-compose.production.yml ps
docker compose -f docker-compose.proxy.yml ps
```

```bash
docker compose -f docker-compose.staging.yml logs -f api
docker compose -f docker-compose.production.yml logs -f api
docker compose -f docker-compose.proxy.yml logs -f proxy
```

## 5) Parar ambientes

```bash
docker compose -f docker-compose.staging.yml down
docker compose -f docker-compose.production.yml down
docker compose -f docker-compose.proxy.yml down
```

Para remover também os dados:

```bash
docker compose -f docker-compose.staging.yml down -v
docker compose -f docker-compose.production.yml down -v
```

## 6) Deploy por script

Deploy de um ambiente específico:

```bash
./scripts/deploy/deploy-target.sh staging
./scripts/deploy/deploy-target.sh production
```

## CI/CD no GitHub

Guia da esteira:
- `docs/deploy-github-actions.md`

Documentacao do Word War (regras, endpoints e fluxo):
- `docs/wordwar.md`
