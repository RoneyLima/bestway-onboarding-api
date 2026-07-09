# Transaction CRUD com CQRS Lógico Specification

## Problem Statement

O projeto já possui uma estrutura base de contas com CQRS aplicado de forma simples na camada de aplicação, mas ainda não implementa a entidade de transações solicitadas pelo solicitante. A feature precisa introduzir o fluxo de movimentação entre contas sem quebrar a separação entre regras de domínio e de aplicação, mantendo a persistência simples com o mesmo `DbContext` e o mesmo banco.

## Goals

- [ ] Permitir criar, consultar e cancelar transações entre contas usando Commands e Queries separados na camada de aplicação
- [ ] Garantir integridade de saldo e de estado da transação sem introduzir bancos separados, read models dedicados ou projeções assíncronas
- [ ] Cobrir regras de domínio, casos de uso e cenários críticos de avaliação com testes automatizados


## Out of Scope

Explicitamente fora do escopo desta feature.

| Feature | Reason |
| --- | --- |
| Banco separado para leitura/escrita | O solicitante vedou separação física |
| Read models dedicados | O solicitante vedou projeções/modelos de leitura dedicados |
| Event Sourcing e replicação | Fora do escopo e proibido pelo enunciado |
| Atualização livre de transação | As especificações não pedem edição de transações |
| Exclusão física de transação | As especificações pedem cancelamento, não remoção |
| Histórico/auditoria avançada | Não é necessário para o escopo do teste |

---

## Assumptions & Open Questions

Todas as ambiguidades identificadas foram resolvidas ou registradas abaixo.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| "CRUD" será atendido com criar, listar, buscar por id e cancelar | Sim | É o conjunto de operações explicitamente descrito no README | y |
| Criar transação atualiza saldos de origem e destino | Sim | Sem esse comportamento, a transação não cumpre o objetivo de movimentação entre contas | y |
| Cancelar transação reverte os saldos exatamente uma vez | Sim | Garante integridade financeira mínima | y |
| Contas de origem e destino devem existir e estar ativas | Sim | Evita operação inválida sobre contas inativas ou inexistentes | y |
| Origem e destino não podem ser a mesma conta | Sim | Mantém consistência da regra de negócio e simplifica a feature | y |
| Valor deve ser estritamente maior que zero | Sim | Regra básica de domínio | y |
| Transação cancelada continua visível nas consultas com `Cancelada = true` | Sim | Necessário para rastrear o efeito do cancelamento | y |
| Segunda tentativa de cancelamento deve falhar | Sim | Preserva integridade de transição de estado | y |

Open questions: none. As decisões não confirmadas pelo usuário foram assumidas para fechar a especificação e devem ser preservadas até eventual ajuste explícito.

---

## User Stories

### P1: Registrar transação entre contas ⭐ MVP

**User Story**: Como operador da API, quero registrar uma transação entre uma conta de origem e uma conta de destino para movimentar saldo entre contas cadastradas.

**Why P1**: É a capacidade central da feature e concentra a maior parte das regras de negócio.

**Acceptance Criteria**:

1. WHEN uma requisição de criação informar `OriginAccountId`, `DestinationAccountId` e `Amount` válidos THEN o sistema SHALL persistir uma nova transação com `Cancelada = false`.
2. WHEN uma transação válida for criada THEN o sistema SHALL debitar o valor da conta de origem e creditar o mesmo valor na conta de destino na mesma unidade transacional lógica do caso de uso.
3. WHEN a conta de origem não existir THEN o sistema SHALL rejeitar a operação com erro de aplicação equivalente a recurso não encontrado.
4. WHEN a conta de destino não existir THEN o sistema SHALL rejeitar a operação com erro de aplicação equivalente a recurso não encontrado.
5. WHEN a conta de origem ou destino estiver desativada THEN o sistema SHALL rejeitar a operação sem persistir transação nem alterar saldos.
6. WHEN a conta de origem for igual à conta de destino THEN o sistema SHALL rejeitar a operação por violação de regra de negócio.
7. WHEN o valor informado for menor ou igual a zero THEN o sistema SHALL rejeitar a operação por violação de regra de validação/domínio.
8. WHEN o saldo da conta de origem for insuficiente THEN o sistema SHALL rejeitar a operação sem persistir transação nem alterar saldos.

**Independent Test**: Criar duas contas válidas com saldo suficiente, enviar o command de criação e verificar a persistência da transação, o `Cancelada = false` e a atualização exata dos saldos.

---

### P1: Consultar transações

**User Story**: Como operador da API, quero listar todas as transações e consultar uma transação específica por id para inspecionar as movimentações realizadas.

**Why P1**: A leitura faz parte do escopo explícito do teste e demonstra a separação CQRS entre escrita e consulta.

**Acceptance Criteria**:

1. WHEN houver transações registradas THEN o sistema SHALL retornar todas as transações em uma query de listagem.
2. WHEN uma transação existir para o id informado THEN o sistema SHALL retornar seus dados de identificação, contas de origem e destino, valor, estado `Cancelada` e metadados herdados da entidade base.
3. WHEN uma transação estiver cancelada THEN o sistema SHALL retorná-la nas consultas com `Cancelada = true`.
4. WHEN nenhuma transação existir para o id informado THEN o sistema SHALL responder com erro equivalente a recurso não encontrado.

**Independent Test**: Persistir transações ativas e canceladas e validar que a listagem e a consulta por id projetam corretamente o estado salvo.

---

### P1: Cancelar transação

**User Story**: Como operador da API, quero cancelar uma transação existente para desfazer seu efeito financeiro sem apagar o histórico.

**Why P1**: O cancelamento substitui o delete físico no escopo do teste e exige integridade de transição de estado.

**Acceptance Criteria**:

1. WHEN uma transação ativa existente for cancelada THEN o sistema SHALL marcar `Cancelada = true`.
2. WHEN uma transação ativa existente for cancelada THEN o sistema SHALL reverter exatamente uma vez o débito/crédito aplicado na criação, restaurando os saldos anteriores.
3. WHEN a transação informada não existir THEN o sistema SHALL responder com erro equivalente a recurso não encontrado.
4. WHEN uma transação já cancelada receber nova solicitação de cancelamento THEN o sistema SHALL rejeitar a operação por violação de estado.
5. WHEN o cancelamento falhar em qualquer etapa THEN o sistema SHALL não persistir estado parcial de saldos ou da transação.

**Independent Test**: Criar uma transação, cancelar pelo command e validar `Cancelada = true` e restauração exata dos saldos; repetir o cancelamento e validar falha.

---

### P2: Manter a separação arquitetural e o padrão do projeto

**User Story**: Como avaliador técnico, quero ver a feature implementada dentro do padrão do projeto para verificar aderência a CQRS, SOLID, tratamento de exceções e organização do código.

**Why P2**: É diretamente ligado aos critérios de avaliação do teste.

**Acceptance Criteria**:

1. WHEN a feature for implementada THEN o sistema SHALL manter Commands e Queries em tipos e handlers separados na camada `Application`.
2. WHEN a feature persistir ou consultar dados THEN o sistema SHALL reutilizar o mesmo `FestpayContext`, sem banco separado, read model dedicado ou replicação.
3. WHEN regras de consistência da transação forem aplicadas THEN o sistema SHALL manter regras intrínsecas da entidade em `Domain` e regras dependentes de consulta/estado externo em `Application`.
4. WHEN ocorrer erro de validação, negócio ou ausência de recurso THEN o sistema SHALL reutilizar o mecanismo atual de exceções e middleware global da API.

**Independent Test**: Revisão automatizada/manual da organização dos tipos e execução dos testes de aplicação/domínio cobrindo separação de responsabilidades e tratamento de falhas.

---

## Edge Cases

- WHEN o valor da transação possuir casas decimais válidas THEN o sistema SHALL preservar o valor exato persistido e aplicado aos saldos
- WHEN a listagem não possuir registros THEN o sistema SHALL retornar coleção vazia com sucesso
- WHEN a transação for cancelada THEN o sistema SHALL permanecer consultável por id e na listagem
- WHEN uma criação inválida falhar após carregar contas THEN o sistema SHALL não alterar o saldo de nenhuma conta
- WHEN houver tentativa de cancelar transação inexistente THEN o sistema SHALL não alterar saldo de nenhuma conta

---

## Regras por Camada

| Regra | Camada esperada |
| --- | --- |
| Valor maior que zero | Domain |
| Estrutura e estado inicial da entidade `Transaction` | Domain |
| Transição de cancelamento e proteção contra dupla reversão | Domain |
| Existência das contas de origem/destino | Application |
| Conta ativa/inativa | Application |
| Saldo suficiente na origem | Application |
| Persistência e consulta de transações | Infra/Application |
| Orquestração de atualização de saldo + transação | Application |

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| --- | --- | --- | --- |
| TX-01 | P1: Registrar transação entre contas | Design | Pending |
| TX-02 | P1: Registrar transação entre contas | Design | Pending |
| TX-03 | P1: Registrar transação entre contas | Design | Pending |
| TX-04 | P1: Registrar transação entre contas | Design | Pending |
| TX-05 | P1: Registrar transação entre contas | Design | Pending |
| TX-06 | P1: Registrar transação entre contas | Design | Pending |
| TX-07 | P1: Registrar transação entre contas | Design | Pending |
| TX-08 | P1: Consultar transações | Design | Pending |
| TX-09 | P1: Consultar transações | Design | Pending |
| TX-10 | P1: Consultar transações | Design | Pending |
| TX-11 | P1: Cancelar transação | Design | Pending |
| TX-12 | P1: Cancelar transação | Design | Pending |
| TX-13 | P1: Cancelar transação | Design | Pending |
| TX-14 | P1: Cancelar transação | Design | Pending |
| TX-15 | P2: Manter a separação arquitetural e o padrão do projeto | Design | Pending |
| TX-16 | P2: Manter a separação arquitetural e o padrão do projeto | Design | Pending |
| TX-17 | P2: Manter a separação arquitetural e o padrão do projeto | Design | Pending |
| TX-18 | P2: Manter a separação arquitetural e o padrão do projeto | Design | Pending |

Coverage: 18 requisitos totais, 18 exigem desenho/implementação, 0 sem mapeamento esperado.

### Requirement Mapping

| Requirement ID | Acceptance criterion resumido |
| --- | --- |
| TX-01 | Persistir transação válida com `Cancelada = false` |
| TX-02 | Atualizar saldos de origem/destino na criação |
| TX-03 | Rejeitar origem inexistente |
| TX-04 | Rejeitar destino inexistente |
| TX-05 | Rejeitar conta inativa |
| TX-06 | Rejeitar origem e destino iguais |
| TX-07 | Rejeitar valor inválido ou saldo insuficiente |
| TX-08 | Listar todas as transações |
| TX-09 | Buscar transação por id com projeção completa |
| TX-10 | Expor corretamente transações canceladas |
| TX-11 | Marcar transação como cancelada |
| TX-12 | Reverter saldos no cancelamento |
| TX-13 | Rejeitar cancelamento de transação inexistente |
| TX-14 | Rejeitar cancelamento duplicado e evitar estado parcial |
| TX-15 | Separar commands e queries na `Application` |
| TX-16 | Compartilhar mesmo `FestpayContext` |
| TX-17 | Separar regras de domínio e aplicação |
| TX-18 | Reutilizar exceções e middleware existentes |

---

## Test Strategy

### Testes de Domínio

| Test ID | Tipo | Requisito(s) | Cenário |
| --- | --- | --- | --- |
| D-TX-01 | Unit | TX-01 | Criar `Transaction` válida com origem, destino e valor |
| D-TX-02 | Unit | TX-07 | Rejeitar valor zero |
| D-TX-03 | Unit | TX-07 | Rejeitar valor negativo |
| D-TX-04 | Unit | TX-06 | Rejeitar origem e destino iguais |
| D-TX-05 | Unit | TX-11 | Cancelar transação ativa muda `Cancelada` para `true` |
| D-TX-06 | Unit | TX-14 | Rejeitar segundo cancelamento da mesma transação |

### Testes de Aplicação

| Test ID | Tipo | Requisito(s) | Cenário |
| --- | --- | --- | --- |
| A-TX-01 | Unit/Application | TX-01, TX-02 | `CreateTransactionCommandHandler` persiste transação e atualiza saldos corretamente |
| A-TX-02 | Unit/Application | TX-03 | Falha ao criar quando origem não existe |
| A-TX-03 | Unit/Application | TX-04 | Falha ao criar quando destino não existe |
| A-TX-04 | Unit/Application | TX-05 | Falha ao criar com conta inativa |
| A-TX-05 | Unit/Application | TX-07 | Falha ao criar com saldo insuficiente |
| A-TX-06 | Unit/Application | TX-08, TX-10 | `GetTransactionsQueryHandler` lista ativas e canceladas |
| A-TX-07 | Unit/Application | TX-09 | `GetTransactionByIdQueryHandler` projeta transação existente |
| A-TX-08 | Unit/Application | TX-09 | `GetTransactionByIdQueryHandler` lança `NotFoundException` para id inexistente |
| A-TX-09 | Unit/Application | TX-11, TX-12 | `CancelTransactionCommandHandler` marca cancelada e reverte saldos |
| A-TX-10 | Unit/Application | TX-13 | Falha ao cancelar transação inexistente |
| A-TX-11 | Unit/Application | TX-14 | Falha ao cancelar transação já cancelada |

### Testes Importantes para os Critérios de Avaliação

| Test ID | Critério de avaliação | Evidência esperada |
| --- | --- | --- |
| E-TX-01 | Separação entre domínio e aplicação | Regras intrínsecas da entidade validadas em testes de domínio; regras dependentes de banco/estado externo validadas em testes de aplicação |
| E-TX-02 | Uso correto da arquitetura definida | Commands e Queries testados por handlers distintos e persistindo via mesmo `FestpayContext` |
| E-TX-03 | Tratamento de exceções | Testes cobrindo `NotFoundException`, violações de domínio e falhas de validação |
| E-TX-04 | Código limpo e organizado | Estrutura de arquivos espelhando o padrão `Features/V1/...` e testes separados por camada |
| E-TX-05 | Estrutura e funcionalidade do código | Fluxos de criação, consulta e cancelamento executando sem estado parcial |

---

## Success Criteria

- [ ] A API expõe os quatro casos de uso pedidos no README para `Transaction`
- [ ] A feature usa CQRS lógico com Commands e Queries separados e mesma persistência
- [ ] Criar e cancelar transações preserva consistência de saldo e estado
- [ ] Os testes de domínio e aplicação cobrem happy path, erros e cenários críticos dos critérios de avaliação
