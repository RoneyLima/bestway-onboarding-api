# Descoberta da Arquitetura Existente

Documento de leitura da solução atual para servir de base às próximas etapas. O conteúdo abaixo descreve o que existe no repositório, sem propor nova arquitetura.

## Estrutura da Solução

### Projetos existentes

- `Festpay.Onboarding.Api`
- `Festpay.Onboarding.Application`
- `Festpay.Onboarding.Domain`
- `Festpay.Onboarding.Infra`
- `tests/Festpay.Onboarding.Domain.Tests`
- `tests/Festpay.Onboarding.Application.Tests`

### Responsabilidade de cada projeto

- `Festpay.Onboarding.Api`: ponto de entrada da aplicação ASP.NET Core, configuração do pipeline HTTP, Swagger, CORS, session e middleware global de exceções. A composição das rotas ocorre via Carter.
- `Festpay.Onboarding.Application`: camada de casos de uso. Contém commands, queries, handlers, validators, constants, pipeline behavior e contratos de resposta usados pelos endpoints.
- `Festpay.Onboarding.Domain`: camada de domínio. Contém entidades, exceções de domínio e extensões de validação.
- `Festpay.Onboarding.Infra`: camada de persistência e infraestrutura. Contém `DbContext`, factory de contexto, configurações do EF Core, migrations e registro de dependências.
- `tests/Festpay.Onboarding.Domain.Tests`: testes unitários de domínio.
- `tests/Festpay.Onboarding.Application.Tests`: testes de aplicação, atualmente focados em um handler.

### Dependências entre projetos

- `Festpay.Onboarding.Api` referencia `Festpay.Onboarding.Application`.
- `Festpay.Onboarding.Application` referencia `Festpay.Onboarding.Domain` e `Festpay.Onboarding.Infra`.
- `Festpay.Onboarding.Infra` referencia `Festpay.Onboarding.Domain`.
- `Festpay.Onboarding.Domain` não referencia outros projetos internos.
- Os projetos de teste referenciam os projetos que validam.

## Arquitetura

### Como o CQRS foi implementado

- O padrão CQRS aparece de forma prática e direta, sem uma abstração adicional de `Command`/`Query` base.
- Cada caso de uso é modelado como um `record` que implementa `IRequest<T>`.
- Os handlers implementam `IRequestHandler<TRequest, TResponse>`.
- Os endpoints recebem a request HTTP, criam ou recebem o `record` do MediatR e encaminham para `ISender.Send(...)`.

Referências:

- `Festpay.Onboarding.Application/Features/V1/Account/CreateAccount.cs`
- `Festpay.Onboarding.Application/Features/V1/Account/ChangeAccountStatus.cs`
- `Festpay.Onboarding.Application/Features/V1/Account/GetAccounts.cs`

### Como Commands são organizados

- Commands ficam dentro da camada `Application`, em arquivos por feature.
- A organização atual é por versão e por contexto funcional, por exemplo `Features/V1/Account`.
- Em cada arquivo do command normalmente existem, no mesmo arquivo:
  - o `record` do command;
  - o validator;
  - o handler;
  - o endpoint Carter.

### Como Queries são organizadas

- Queries seguem o mesmo padrão dos commands.
- O query atual é `GetAccountsQuery`.
- A resposta é modelada em um `record` separado: `GetAccountsQueryResponse`.
- O handler faz leitura direta do `DbSet`, projeta para DTO/response e devolve coleção materializada.

### Como Handlers funcionam

- Handlers recebem dependências por injeção no construtor primário.
- O MediatR resolve os handlers automaticamente após o registro feito em `AppModules`.
- Há um pipeline behavior de validação executado antes do handler.
- Os handlers acessam diretamente o `FestpayContext`.

### Como ocorre a injeção de dependências

- A aplicação registra:
  - validators da assembly da `Application`;
  - handlers do MediatR da assembly da `Application`;
  - o `ValidationBehaviour<,>`.
- A infraestrutura registra o `FestpayContext` e um `FestpayContextFactory`.
- Os endpoints Carter recebem `ISender` por `[FromServices]`.
- O `DbContext` é injetado diretamente nos handlers.

Referências:

- `Festpay.Onboarding.Application/Modules/AppModules.cs`
- `Festpay.Onboarding.Infra/DependencyInjection.cs`
- `Festpay.Onboarding.Api/Program.cs`

### Como o Domain conversa com a Application

- A Application depende do Domain para:
  - instanciar `Account`;
  - validar regras do domínio;
  - usar exceções de domínio;
  - reutilizar extensões de validação.
- A conversa acontece de forma direta, sem service layer intermediária.
- O builder de `Account` chama `Validate()` antes de retornar a entidade.
- Os handlers da Application criam e manipulam entidades do domínio diretamente.

### Como ocorre o acesso a dados

- O acesso a dados é feito diretamente via `FestpayContext` e `DbSet<Account>`.
- Não há repositório abstrato na solução atual.
- O handler de criação usa `AddAsync(...)` e `SaveChangesAsync(...)`.
- O handler de consulta usa `ToListAsync(...)`.
- O handler de mudança de status usa `Update(...)` e `SaveChangesAsync(...)`.

## Domínio

### Entidade Base

- `EntityBase` contém:
  - `Id`
  - `CreatedUtc`
  - `DeactivatedUtc`
- Há um comportamento comum de alternância de status com `EnableDisable()`.
- `Validate()` é virtual e vazio por padrão.

Referência:

- `Festpay.Onboarding.Domain/Entities/EntityBase.cs`

### Aggregate Roots

- Não há aggregate roots explicitamente marcados.
- Na prática, `Account` é a entidade central e o único aggregate observado no código atual.

### Value Objects

- Não há value objects explícitos no repositório atual.
- As regras de validação estão concentradas em extensão de string e na própria entidade.

### Repositórios

- Não existem interfaces ou implementações de repositório no código atual.
- O acesso ao banco ocorre diretamente via `DbContext`.

### Serviços de domínio

- Não há serviços de domínio explícitos.
- As regras de domínio estão concentradas em:
  - `Account.Validate()`
  - `EntityBase.EnableDisable()`
  - extensões de validação

### Entidade Account

- `Account` herda `EntityBase`.
- Campos observados:
  - `Name`
  - `Email`
  - `Phone`
  - `Document`
  - `Balance`
  - `CreatedAt`
- A entidade valida:
  - nome obrigatório;
  - documento válido;
  - email válido;
  - telefone válido.
- A criação é feita via `Account.Builder`, que encapsula a construção e chama `Validate()`.

Referência:

- `Festpay.Onboarding.Domain/Entities/Account.cs`

## Persistência

### ORM utilizado

- Entity Framework Core 9.0.4.
- Provider configurado no factory: SQLite.

### Estratégia de mapeamento

- O `DbContext` aplica configurações por assembly com `ApplyConfigurationsFromAssembly(...)`.
- Existe uma configuração para `Account` herdando de uma configuração base.
- A configuração da entidade base define chave e propriedades compartilhadas.

### Configurações das entidades

- `AccountConfiguration` apenas reaproveita o mapeamento da entidade base.
- O mapeamento explícito observado cobre:
  - chave primária `Id`;
  - `CreatedUtc`;
  - `DeactivatedUtc`.
- Demais propriedades são mapeadas por convenção do EF Core.

### Migrations

- Há migrations versionadas em `Festpay.Onboarding.Infra/Migrations`.
- O histórico observado indica:
  - migração inicial;
  - remoção de `AlternateId`;
  - remoção de `DisabledAt`.
- O snapshot atual do modelo mostra a tabela `Accounts` com:
  - `Id`
  - `Balance`
  - `CreatedAt`
  - `CreatedUtc`
  - `DeactivatedUtc`
  - `Document`
  - `Email`
  - `Name`
  - `Phone`

Referências:

- `Festpay.Onboarding.Infra/Context/FestpayContext.cs`
- `Festpay.Onboarding.Infra/Context/FestpayContextFactory.cs`
- `Festpay.Onboarding.Infra/Configurations/ConfigurationBase.cs`
- `Festpay.Onboarding.Infra/Configurations/AccountConfiguration.cs`
- `Festpay.Onboarding.Infra/Migrations/FestpayContextModelSnapshot.cs`

## API

### Organização dos Controllers

- Não há controllers tradicionais no repositório atual.
- As rotas estão concentradas em módulos Carter dentro da camada `Application`.
- `Program.cs` ainda registra `AddControllers()` e `MapControllers()`, mas não há controllers implementados no código analisado.

### Convenções REST

- Endpoints observados:
  - `GET /api/v1/accounts`
  - `POST /api/v1/accounts`
  - `PATCH /api/v1/accounts/{id:guid}`
- Os endpoints usam verbos HTTP alinhados ao tipo de operação.
- As rotas são montadas com `EndpointConstants`.

### Tratamento de erros

- O pipeline usa `ExceptionMiddleware` global.
- O middleware converte exceções em respostas JSON com o formato `Result.Failure(...)`.
- Mapeamento observado:
  - `DomainException` -> 400
  - `NotFoundException` -> 404
  - `ValidationException` -> 422
  - `ApplicationExceptions` -> 400
  - demais exceções -> 500
- Erros 500 são logados.

### Responses

- As rotas retornam respostas embrulhadas em `Result`.
- O payload padrão é:
  - `data`
  - `success`
  - `message`
- O wrapper é usado tanto para sucesso quanto para falha.

Referências:

- `Festpay.Onboarding.Api/Program.cs`
- `Festpay.Onboarding.Api/Middlewares/ExceptionMiddleware.cs`
- `Festpay.Onboarding.Application/Common/Models/Result.cs`

## Validação

### Commands

- Commands têm validators com FluentValidation.
- A validação é feita antes do handler via `ValidationBehaviour<,>`.
- Regras observadas:
  - `CreateAccountCommandValidator`
  - `ChangeAccountStatusCommandValidator`

### Requests

- Os próprios records recebidos pela API servem como requests do MediatR.
- A validação fica na Application, não na camada API.

### Domínio

- O domínio valida na construção da entidade.
- `Account.Builder.Build()` chama `Account.Validate()`.
- Há também extensões de validação para documento, email e telefone.
- As exceções levantadas no domínio são específicas de regra/valor inválido.

Referências:

- `Festpay.Onboarding.Application/Common/Behaviours/ValidationBehaviour.cs`
- `Festpay.Onboarding.Application/Features/V1/Account/CreateAccount.cs`
- `Festpay.Onboarding.Application/Features/V1/Account/ChangeAccountStatus.cs`
- `Festpay.Onboarding.Domain/Entities/Account.cs`
- `Festpay.Onboarding.Domain/Extensions/ValidationExtension.cs`

## Testes

### Framework utilizado

- xUnit
- Moq está disponível no projeto de Application Tests, embora não tenha sido observado uso nos arquivos lidos.
- EF Core InMemory é usado nos testes de aplicação.

### Organização

- Testes de domínio estão em `tests/Festpay.Onboarding.Domain.Tests`.
- Testes de aplicação estão em `tests/Festpay.Onboarding.Application.Tests`.
- A organização segue a estrutura por feature/entidade.

### Padrão AAA ou outro

- Os testes de aplicação seguem explicitamente AAA em pelo menos um caso:
  - Arrange
  - Act
  - Assert
- Os testes de domínio seguem um padrão mais direto, sem seções nomeadas, mas ainda com separação clara entre montagem, execução e asserção.

### Estratégia de mocks

- No material inspecionado não há mocks explícitos em uso.
- Os testes de aplicação usam `FestpayContext` com provider InMemory em vez de mockar o `DbContext`.

### Cobertura observada

- Domínio:
  - criação válida de `Account`
  - nome obrigatório
  - documento inválido
  - email inválido
  - telefone inválido
- Aplicação:
  - toggle de status em `ChangeAccountStatusCommandHandler`
  - exceção `NotFoundException` quando a conta não existe

Referências:

- `tests/Festpay.Onboarding.Domain.Tests/Entities/AccountTests.cs`
- `tests/Festpay.Onboarding.Application.Tests/Features/V1/Account/ChangeAccountStatusTest.cs`

## Convenções

### Nomenclatura

- Namespaces seguem o padrão `Festpay.Onboarding.<Layer>`.
- Types usam nomes descritivos e verbosos:
  - `CreateAccountCommand`
  - `GetAccountsQuery`
  - `ChangeAccountStatusCommandHandler`
  - `ValidationBehaviour<TRequest, TUnit>`
- Exceções seguem nome funcional, por exemplo `NotFoundException`, `EntityAlreadyExistsException`.

### Namespaces

- A camada `Application` concentra features sob `Features/V1`.
- Os módulos de suporte ficam em `Common`.
- O domínio usa `Entities`, `Exceptions` e `Extensions`.

### Organização de pastas

- A estrutura é por camada e depois por feature.
- Commands, queries, handlers e endpoints ficam agrupados no mesmo arquivo.
- As migrations ficam isoladas na Infra.

### Padrões arquiteturais

- CQRS com MediatR.
- Minimal APIs via Carter.
- Validação com FluentValidation e pipeline behavior.
- Persistência com EF Core.
- Resposta padronizada via wrapper `Result`.

### Princípios recorrentes

- Forte uso de imutabilidade parcial por `record`.
- Construção de entidade via builder para forçar validação.
- Regras de domínio centralizadas na entidade.
- Exceções específicas para erro de negócio e de validação.

## Riscos

- O handler de `ChangeAccountStatusCommand` está com lógica mockada e não atualiza a conta real. Isso quebra a consistência do caso de uso e invalida o comportamento esperado.
- `CreateAccountCommandHandler` consulta existência por `Document` antes de inserir, mas não há garantia de unicidade no banco observada no mapeamento atual.
- `Application` depende diretamente de `Infra`, o que aumenta o acoplamento entre caso de uso e persistência concreta.
- O acesso ao banco é feito diretamente dentro dos handlers, sem camada de repositório para isolar regras de consulta/persistência.
- `EntityBase.Id` é somente leitura e depende de materialização/configuração do EF; qualquer ajuste de persistência pode afetar a identidade da entidade.
- Existem sinais de divergência entre modelo e histórico de migrations, como campos removidos no histórico e propriedades ainda presentes no domínio.
- `Program.cs` registra controllers mesmo sem controllers presentes, o que indica configuração redundante ou legado.
- `ExceptionMiddleware` captura todas as exceções depois de `MapControllers`/`MapCarter`; qualquer mudança na ordem do pipeline pode alterar o tratamento.

## Recomendações de Reutilização

- Reutilizar o padrão de `record` + validator + handler + endpoint por feature.
- Reutilizar a validação de domínio no `Builder.Build()` para novas entidades com regras obrigatórias.
- Reutilizar `Result` como envelope de resposta para manter consistência na API.
- Reutilizar `ExceptionMiddleware` e o mapeamento atual de exceções para respostas HTTP.
- Reutilizar o `ValidationBehaviour<,>` para manter validação fora dos handlers.
- Reutilizar a estratégia de `ApplyConfigurationsFromAssembly(...)` para novas entidades de persistência.

## Observação de Verificação

- A leitura está baseada no código-fonte e nas migrations.
- A execução de `dotnet test` foi iniciada, mas foi interrompida antes de concluir nesta sessão.
