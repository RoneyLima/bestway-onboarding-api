# Contexto da Feature: Transaction CRUD com CQRS Lógico

## Decisões Confirmadas pelo Solicitante

| Tópico | Decisão |
| --- | --- |
| Padrão arquitetural | Aplicar CQRS lógico na camada de aplicação |
| Commands e Queries | Separados em handlers distintos |
| Persistência | Mesmo modelo e mesmo banco de dados |
| Read model dedicado | Não usar |
| Projeções assíncronas | Não usar |
| Event Sourcing | Não usar |
| Replicação | Não usar |
| Escopo de testes | Incluir testes de unidade e testes relevantes para os critérios de avaliação |

## Interpretações Operacionais Adotadas

| Área | Interpretação adotada | Motivo |
| --- | --- | --- |
| "CRUD" de transações | O conjunto esperado é criar, listar, buscar por id e cancelar | O README enumera essas quatro operações; não há update nem delete físico |
| Cancelamento | Cancelamento é lógico e irreversível | O requisito pede o campo `Cancelada` e um método específico para cancelar |
| CQRS | A separação acontece no nível de casos de uso (`Command`/`Query`), mantendo o padrão já existente em `Account` | Mantém aderência ao projeto e ao escopo do teste |
| Persistência transacional | Criação e cancelamento devem salvar transação e saldos das contas no mesmo `SaveChangesAsync` | Necessário para integridade de estado sem introduzir complexidade extra |

## Assunções Necessárias para Fechar a Especificação

| Assunção | Default adotado | Racional |
| --- | --- | --- |
| Efeito financeiro da criação | Criar transação debita a conta de origem e credita a conta de destino | Sem esse efeito a entidade de transação não teria impacto funcional no sistema de contas |
| Efeito do cancelamento | Cancelar reverte exatamente uma vez o débito/crédito original e marca a transação como cancelada | Preserva consistência contábil e evita dupla reversão |
| Contas elegíveis | Origem e destino devem existir e estar ativas | Evita movimentação para contas inválidas ou desativadas |
| Contas repetidas | Origem e destino não podem ser a mesma conta | Evita operação sem efeito prático e simplifica as regras |
| Valor da transação | Deve ser maior que zero | Regra mínima de integridade do domínio |
| Saldo da origem | Deve ser suficiente no momento da criação | Evita saldo negativo por operação inválida |
| Visibilidade em leitura | Consultas retornam transações ativas e canceladas, expondo o estado de cancelamento | Necessário para auditoria mínima da operação de cancelamento |
