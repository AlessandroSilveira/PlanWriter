# Esteira de Deploy com GitHub Actions

Este projeto agora tem uma pipeline em:

- `.github/workflows/pipeline.yml`

Ela faz:

1. Build e testes (`dotnet test`) no push/PR da `main`.
2. Deploy automático em runner self-hosted (Docker Compose local) quando houver push na `main`.
3. Deploy manual com `workflow_dispatch` (botão "Run workflow" no GitHub).

## 1) Pré-requisitos na máquina de deploy (self-hosted runner)

Na máquina de deploy, você precisa:

- Docker + Docker Compose instalados
- GitHub Actions self-hosted runner registrado no repositório com labels:
  - `self-hosted`
  - `macOS`
  - `deploy`
- Dois repositórios clonados como irmãos no mesmo diretório:
  - `PlanWriter`
  - `PlanWriter-Frontend`

Exemplo:

```bash
/opt/planwriter/PlanWriter
/opt/planwriter/PlanWriter-Frontend
```

## 2) Secrets no GitHub

No repositório `PlanWriter`, configure em `Settings > Secrets and variables > Actions`:

- `DEPLOY_ROOT`: pasta raiz onde estão os 2 repositórios (ex.: `/opt/planwriter`)

## 3) Como a pipeline publica

No job de deploy, a pipeline:

1. Roda no runner self-hosted da sua máquina de deploy.
2. Atualiza backend e frontend (`git pull --ff-only origin main`).
3. Executa:

```bash
docker compose -f /SEU_DEPLOY_ROOT/PlanWriter/docker-compose.yml up -d --build
docker compose -f /SEU_DEPLOY_ROOT/PlanWriter/docker-compose.yml ps
```

## 4) Como rodar o primeiro deploy manual

1. Faça merge na `main` (ou use o botão manual).
2. Vá em `Actions > PlanWriter Pipeline`.
3. Clique em `Run workflow`.
4. Acompanhe os logs do job `Deploy`.

## 5) Dica para aprendizado

Comece com deploy manual (`Run workflow`) e depois confie no deploy automático por push na `main`.
