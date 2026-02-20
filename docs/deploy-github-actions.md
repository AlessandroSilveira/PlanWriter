# Esteira de Deploy (Backend) com GitHub Actions

Pipeline:

- `.github/workflows/pipeline.yml`

## O que ela faz

1. `Build and Test` em `push`/`pull_request` da `main`.
2. `Security config guard` para bloquear segredos hardcoded/default inseguros.
3. Deploy automatico em `staging` no `push` da `main`.
4. Deploy manual em `staging` ou `production` via `workflow_dispatch`.
5. Rollback manual via `workflow_dispatch`.
6. Deploy local com hostnames:
   - `http://planwriter.staging.test`
   - `http://planwriter.test`

## Environments (staging e production)

Crie no GitHub:

- `Settings > Environments > New environment`
- `staging`
- `production`

Em cada ambiente, configure:

- Secret `DEPLOY_ROOT`

Exemplo:

```text
/Users/alessandrosilveira/planwriter-deploy
```

Se quiser separar infraestrutura, use caminhos diferentes por ambiente.

## Como o deploy sobe containers separados

O pipeline chama:

```bash
./scripts/deploy/deploy-target.sh staging
./scripts/deploy/deploy-target.sh production
```

Esse script:
- sobe `docker-compose.staging.yml` ou `docker-compose.production.yml`
- garante o proxy `docker-compose.proxy.yml`
- publica os hostnames locais `.test`

## Como rodar deploy manual

No workflow `PlanWriter Pipeline`, clique em `Run workflow` e escolha:

- `target`: `staging` ou `production`
- `action`: `deploy`
- `deploy_ref` (opcional): branch/tag/SHA

Se `deploy_ref` ficar vazio, usa a branch atual do run.

Depois confirme no navegador:
- `http://planwriter.staging.test`
- `http://planwriter.test`

## Como rodar rollback

No workflow `PlanWriter Pipeline`:

- `target`: `staging` ou `production`
- `action`: `rollback`
- `rollback_ref`: branch/tag/SHA para voltar

Obs.: o `rollback_ref` deve existir nos dois repositórios (`PlanWriter` e `PlanWriter-Frontend`).

## Branch protection (main)

No GitHub:

1. `Settings > Branches > Add branch protection rule`
2. Branch name pattern: `main`
3. Marcar `Require a pull request before merging`
4. Marcar `Require status checks to pass before merging`
5. Selecionar check obrigatório:
   - `Build and Test`
6. Salvar

Para `production`, recomenda-se também exigir aprovação no Environment `production`.

Opcional via comando (API):

```bash
cd /Users/alessandrosilveira/Documents/Repos/PlanWriter
export GITHUB_TOKEN=seu_token_com_admin_repo
./scripts/ci/apply-branch-protection.sh
```

## /etc/hosts (clientes da rede local)

Em cada máquina cliente:

```text
192.168.15.182 planwriter.staging.test
192.168.15.182 planwriter.test
```
