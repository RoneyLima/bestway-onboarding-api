# Transaction CRUD com CQRS Lógico Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: activate it by name and follow its Execute flow and Critical Rules. The skill is the source of truth for the per-task cycle, verification flow, and final validation.

If the skill cannot be activated, STOP and tell the user.

---

**Design**: none - implementation derived directly from `spec.md` and `context.md`
**Status**: Draft

---

## Test Coverage Matrix

> Generated from codebase, project guidelines, and spec - confirm before Execute. Guidelines found: `.github/workflows/dotnet.yml`, `README.md`, `docs/architecture-discovery.md`. Existing test samples used: `tests/Festpay.Onboarding.Domain.Tests/Entities/AccountTests.cs`, `tests/Festpay.Onboarding.Application.Tests/Features/V1/Account/ChangeAccountStatusTest.cs`.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| ---------- | ------------------ | -------------------- | ---------------- | ----------- |
| Domain entities and domain rules | unit | All branches; 1:1 with spec ACs that belong to `Domain`; every listed edge case of entity state/change covered; new transaction scenarios must live in dedicated `TransactionTests` files, not be merged into existing `AccountTests` beyond shared helper adjustments | `tests/Festpay.Onboarding.Domain.Tests/Entities/*Tests.cs` | `dotnet test tests/Festpay.Onboarding.Domain.Tests/Festpay.Onboarding.Domain.Tests.csproj --verbosity normal` |
| Application command/query handlers | unit | Happy path + all failure paths from mapped ACs; verify persisted state and exception behavior using `EFCore.InMemory`; each transaction use case must have its own test file under `Features/V1/Transaction`, separate from existing `Account` tests | `tests/Festpay.Onboarding.Application.Tests/Features/V1/**/*Test.cs` | `dotnet test tests/Festpay.Onboarding.Application.Tests/Festpay.Onboarding.Application.Tests.csproj --verbosity normal` |
| Carter endpoint registration, DI wiring, EF configuration/migrations | none | Build gate only; correctness proven indirectly through compile/build and handler tests in this repo scope | `Festpay.Onboarding.Application/**`, `Festpay.Onboarding.Infra/**`, `Festpay.Onboarding.Api/**` | `dotnet build Festpay.Onboarding.Api.sln --verbosity normal` |

## Gate Check Commands

> Generated from codebase - confirm before Execute.

| Gate Level | When to Use | Command |
| ---------- | ----------- | ------- |
| Quick | After tasks with domain tests only | `dotnet test tests/Festpay.Onboarding.Domain.Tests/Festpay.Onboarding.Domain.Tests.csproj --verbosity normal` |
| Full | After tasks with application-handler tests or persistence changes that affect handlers | `dotnet test Festpay.Onboarding.Api.sln --verbosity normal` |
| Build | After configuration, migration, or endpoint-wiring tasks with no direct test layer | `dotnet build Festpay.Onboarding.Api.sln --verbosity normal` |

---

## Execution Plan

Phases are ordered and run sequentially - each phase completes before the next begins, and tasks within a phase execute in order.

### Phase 1: Domain Foundation

```
T1 -> T2
```

### Phase 2: Persistence Foundation

```
T3
```

### Phase 3: Write Flow

```
T4 -> T5
```

### Phase 4: Read Flow

```
T6 -> T7
```

---

## Task Breakdown

### T1: Criar a entidade `Transaction` com regras próprias e testes de domínio

**What**: Criar a entidade de domínio `Transaction`, seu estado inicial e suas regras intrínsecas de criação/cancelamento, junto com os testes unitários dessa entidade.
**Where**: `Festpay.Onboarding.Domain/Entities/Transaction.cs`, `tests/Festpay.Onboarding.Domain.Tests/Entities/TransactionTests.cs`
**Depends on**: None
**Reuses**: `Festpay.Onboarding.Domain/Entities/EntityBase.cs`, `Festpay.Onboarding.Domain/Entities/Account.cs`
**Requirement**: TX-01, TX-06, TX-07, TX-11, TX-14, TX-17

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] `Transaction` herda `EntityBase` e expõe `OriginAccountId`, `DestinationAccountId`, `Amount` e `Canceled`
- [ ] A criação válida inicia `Canceled = false`
- [ ] O domínio rejeita valor menor ou igual a zero
- [ ] O domínio rejeita origem e destino iguais
- [ ] O domínio permite cancelar uma vez e rejeita cancelamento duplicado
- [ ] Os testes da nova entidade ficam em arquivo dedicado `TransactionTests.cs`, sem misturar cenários de `Transaction` no `AccountTests.cs`
- [ ] Gate check passes: `dotnet test tests/Festpay.Onboarding.Domain.Tests/Festpay.Onboarding.Domain.Tests.csproj --verbosity normal`
- [ ] Test count: 6+ testes de domínio passam sem deletar testes existentes

**Tests**: unit
**Gate**: quick

---

### T2: Adicionar operações de saldo na entidade `Account` com testes de domínio

**What**: Introduzir operações explícitas de débito/crédito em `Account`, com regras mínimas para impedir saldo inválido e testes unitários correspondentes.
**Where**: `Festpay.Onboarding.Domain/Entities/Account.cs`, `tests/Festpay.Onboarding.Domain.Tests/Entities/AccountTests.cs`
**Depends on**: T1
**Reuses**: padrão atual de exceções de domínio em `Festpay.Onboarding.Domain/Exceptions/DomainExceptions.cs`
**Requirement**: TX-02, TX-07, TX-12, TX-17

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] `Account` expõe operações coesas para creditar e debitar saldo
- [ ] Débito insuficiente falha por regra de domínio ou regra explicitamente reutilizável pela aplicação
- [ ] Os testes de domínio de `Account` permanecem em `AccountTests.cs`; nenhum cenário novo de `Transaction` é adicionado nesse arquivo
- [ ] Os testes de domínio passam cobrindo crédito, débito e falha por saldo insuficiente
- [ ] Gate check passes: `dotnet test tests/Festpay.Onboarding.Domain.Tests/Festpay.Onboarding.Domain.Tests.csproj --verbosity normal`
- [ ] Test count: suíte de domínio passa com os testes antigos e os novos

**Tests**: unit
**Gate**: quick

---

### T3: Registrar persistência de `Transaction` no EF Core

**What**: Adicionar a persistência de `Transaction` ao `FestpayContext`, criar configuração EF e gerar a migration correspondente.
**Where**: `Festpay.Onboarding.Infra/Context/FestpayContext.cs`, `Festpay.Onboarding.Infra/Configurations/TransactionConfiguration.cs`, `Festpay.Onboarding.Infra/Migrations/*Transaction*.cs`
**Depends on**: T1, T2
**Reuses**: `Festpay.Onboarding.Infra/Configurations/ConfigurationBase.cs`, `Festpay.Onboarding.Infra/Configurations/AccountConfiguration.cs`
**Requirement**: TX-01, TX-08, TX-09, TX-10, TX-16

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] `FestpayContext` expõe `DbSet<Transaction>`
- [ ] A configuração EF mapeia a nova entidade e reaproveita a base comum
- [ ] A migration adiciona a tabela/campos necessários para transações
- [ ] A solução compila com a nova configuração
- [ ] Gate check passes: `dotnet build Festpay.Onboarding.Api.sln --verbosity normal`

**Tests**: none
**Gate**: build

---

### T4: Implementar `CreateTransactionCommand` com endpoint e testes de aplicação

**What**: Implementar o fluxo de criação da transação, incluindo validator, handler, endpoint Carter e testes de aplicação do caso de uso.
**Where**: `Festpay.Onboarding.Application/Features/V1/Transaction/CreateTransaction.cs`, `tests/Festpay.Onboarding.Application.Tests/Features/V1/Transaction/CreateTransactionCommandHandlerTests.cs`
**Depends on**: T3
**Reuses**: `Festpay.Onboarding.Application/Features/V1/Account/CreateAccount.cs`, `Festpay.Onboarding.Application/Common/Behaviours/ValidationBehaviour.cs`
**Requirement**: TX-01, TX-02, TX-03, TX-04, TX-05, TX-06, TX-07, TX-15, TX-16, TX-17, TX-18

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] O command valida ids obrigatórios e valor informado
- [ ] O handler carrega contas de origem/destino no mesmo `FestpayContext`
- [ ] O handler rejeita conta inexistente, conta inativa, mesma conta e saldo insuficiente
- [ ] O handler persiste a transação e atualiza saldos no mesmo fluxo de `SaveChangesAsync`
- [ ] O endpoint expõe o `POST /api/v1/transactions`
- [ ] Os testes ficam em arquivo próprio dentro de `Features/V1/Transaction`, sem reutilizar ou expandir o arquivo de testes de `Account`
- [ ] Os testes de aplicação cobrem happy path e falhas dos requisitos TX-03 a TX-07
- [ ] Gate check passes: `dotnet test Festpay.Onboarding.Api.sln --verbosity normal`
- [ ] Test count: 5+ testes novos de criação passam sem deletar testes existentes

**Tests**: unit
**Gate**: full

---

### T5: Implementar `CancelTransactionCommand` com endpoint e testes de aplicação

**What**: Implementar o cancelamento da transação, revertendo saldos e protegendo contra cancelamento duplicado.
**Where**: `Festpay.Onboarding.Application/Features/V1/Transaction/CancelTransaction.cs`, `tests/Festpay.Onboarding.Application.Tests/Features/V1/Transaction/CancelTransactionCommandHandlerTests.cs`
**Depends on**: T4
**Reuses**: `Festpay.Onboarding.Application/Features/V1/Account/ChangeAccountStatus.cs`, `Festpay.Onboarding.Application/Common/Exceptions/ApplicationExceptions.cs`
**Requirement**: TX-11, TX-12, TX-13, TX-14, TX-15, TX-16, TX-17, TX-18

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] O handler localiza a transação por id no mesmo `FestpayContext`
- [ ] O handler marca `Canceled = true` e reverte exatamente uma vez os saldos
- [ ] O handler falha para transação inexistente e para transação já cancelada
- [ ] O endpoint expõe o `PATCH /api/v1/transactions/{id:guid}/cancel`
- [ ] Os testes ficam em arquivo próprio dentro de `Features/V1/Transaction`, sem reutilizar ou expandir o arquivo de testes de `Account`
- [ ] Os testes de aplicação cobrem happy path, não encontrado e cancelamento duplicado
- [ ] Gate check passes: `dotnet test Festpay.Onboarding.Api.sln --verbosity normal`
- [ ] Test count: 3+ testes novos de cancelamento passam sem deletar testes existentes

**Tests**: unit
**Gate**: full

---

### T6: Implementar `GetTransactionsQuery` com endpoint e testes de aplicação

**What**: Implementar a consulta de listagem de transações com projeção de resposta e endpoint Carter.
**Where**: `Festpay.Onboarding.Application/Features/V1/Transaction/GetTransactions.cs`, `tests/Festpay.Onboarding.Application.Tests/Features/V1/Transaction/GetTransactionsQueryHandlerTests.cs`
**Depends on**: T5
**Reuses**: `Festpay.Onboarding.Application/Features/V1/Account/GetAccounts.cs`
**Requirement**: TX-08, TX-10, TX-15, TX-16, TX-18

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] A query lista transações ativas e canceladas
- [ ] A projeção inclui ids, contas, valor, estado `Canceled` e metadados necessários
- [ ] O endpoint expõe o `GET /api/v1/transactions`
- [ ] Os testes ficam em arquivo próprio dentro de `Features/V1/Transaction`, sem reutilizar ou expandir o arquivo de testes de `Account`
- [ ] Os testes de aplicação cobrem lista vazia e lista com transações ativas/canceladas
- [ ] Gate check passes: `dotnet test Festpay.Onboarding.Api.sln --verbosity normal`
- [ ] Test count: 2+ testes novos de listagem passam sem deletar testes existentes

**Tests**: unit
**Gate**: full

---

### T7: Implementar `GetTransactionByIdQuery` com endpoint e alinhamentos compartilhados da feature

**What**: Implementar a consulta por id, completar constantes compartilhadas da feature `Transaction` e fechar a cobertura dos requisitos de leitura.
**Where**: `Festpay.Onboarding.Application/Features/V1/Transaction/GetTransactionById.cs`, `Festpay.Onboarding.Application/Common/Constants/EndpointConstants.cs`, `Festpay.Onboarding.Application/Common/Constants/SwaggerTagsConstants.cs`, `tests/Festpay.Onboarding.Application.Tests/Features/V1/Transaction/GetTransactionByIdQueryHandlerTests.cs`
**Depends on**: T6
**Reuses**: `Festpay.Onboarding.Application/Common/Constants/EndpointConstants.cs`, `Festpay.Onboarding.Application/Common/Constants/SwaggerTagsConstants.cs`
**Requirement**: TX-09, TX-10, TX-15, TX-16, TX-18

**Tools**:

- MCP: NONE
- Skill: `tlc-spec-driven`

**Done when**:

- [ ] A query retorna a transação correta pelo id com projeção completa
- [ ] O handler lança `NotFoundException` quando o id não existir
- [ ] As constantes compartilhadas suportam as rotas/tags da feature `Transaction`
- [ ] O endpoint expõe o `GET /api/v1/transactions/{id:guid}`
- [ ] Os testes ficam em arquivo próprio dentro de `Features/V1/Transaction`, sem reutilizar ou expandir o arquivo de testes de `Account`
- [ ] Os testes de aplicação cobrem sucesso e não encontrado
- [ ] Gate check passes: `dotnet test Festpay.Onboarding.Api.sln --verbosity normal`
- [ ] Test count: 2+ testes novos de busca por id passam sem deletar testes existentes

**Tests**: unit
**Gate**: full

---

## Phase Execution Map

```
Phase 1 -> Phase 2 -> Phase 3 -> Phase 4

Phase 1: T1 --> T2
Phase 2: T3
Phase 3: T4 --> T5
Phase 4: T6 --> T7
```

Execution is strictly sequential - there is no intra-phase parallelism.

When the whole feature fits a single batch (<= ~8 tasks), execution happens inline in the main window with no sub-agents spawned.

---

## Task Granularity Check

| Task | Scope | Status |
| --- | --- | --- |
| T1: Criar entidade `Transaction` + testes | 1 entidade de domínio coesa | ✅ Granular |
| T2: Adicionar operações de saldo em `Account` + testes | 1 entidade existente e suas regras | ✅ Granular |
| T3: Registrar persistência de `Transaction` | 1 camada de persistência coesa | ✅ Granular |
| T4: Implementar criação de transação + testes | 1 caso de uso de escrita | ✅ Granular |
| T5: Implementar cancelamento de transação + testes | 1 caso de uso de escrita | ✅ Granular |
| T6: Implementar listagem de transações + testes | 1 caso de uso de leitura | ✅ Granular |
| T7: Implementar busca por id + alinhamentos compartilhados + testes | 1 caso de uso de leitura com suporte mínimo de rota/tag | ✅ Granular |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
| ---- | ---------------------- | ------------- | ------ |
| T1 | None | Nenhuma dependência de entrada | ✅ Match |
| T2 | T1 | T1 -> T2 | ✅ Match |
| T3 | T1, T2 | T1/T2 concluídos antes de Phase 2 | ✅ Match |
| T4 | T3 | T3 -> T4 | ✅ Match |
| T5 | T4 | T4 -> T5 | ✅ Match |
| T6 | T5 | T5 -> T6 | ✅ Match |
| T7 | T6 | T6 -> T7 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
| ---- | --------------------------- | --------------- | --------- | ------ |
| T1 | Domain entities and domain rules | unit | unit | ✅ OK |
| T2 | Domain entities and domain rules | unit | unit | ✅ OK |
| T3 | EF configuration/migrations | none | none | ✅ OK |
| T4 | Application command handler | unit | unit | ✅ OK |
| T5 | Application command handler | unit | unit | ✅ OK |
| T6 | Application query handler | unit | unit | ✅ OK |
| T7 | Application query handler + shared constants | unit | unit | ✅ OK |

---

## Tool Selection Question for Execute

Before execution, confirm tool preference per task.

**Available MCPs**: none identificados no repositório
**Available Skills**: `tlc-spec-driven`, `imagegen`, `openai-docs`, `plugin-creator`, `skill-creator`, `skill-installer`

Pergunta para execução: para cada task, devo seguir apenas com ferramentas locais do workspace e o skill `tlc-spec-driven`, ou você quer restringir/indicar alguma ferramenta específica?
