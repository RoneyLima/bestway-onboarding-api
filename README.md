# 🧪 Teste Técnico - Dev Fullstack (.NET/C#) - Festpay

## 🎯 Objetivo

Construir e manter uma api em .NET 9 utilizando o padrão CQRS afim de manter um sistema de contas e transações da Festpay. Utilizando dos métodos já existentes, construa a entidade de Transações e o seu respectivo CRUD.
A entidade deve herdar a entidade base e possuir os seguintes dados:

- **Conta de destino**
- **Conta de origem**
- **Valor**
- **Cancelada**

Deverá ser desenvolvido métodos para:

- **Buscar todas as transações**
- **Buscar uma transação pelo Id**
- **Inserir uma transação**
- **Cancelar uma transação**

---

**ATENÇÃO** - Não se esqueça de desenvolver os testes de domínio e testes de aplicação.

---

## 🧱 Critérios de Avaliação

- Separação das regras de domínio e regras de aplicação
- Estrutura e funcionalidade do código existente e do código redigido
- Uso correto da arquitetura definida no projeto
- Princípios SOLID
- Tratamento de exceções
- Código limpo e organizado

---

## 📤 Entrega

- Criar um fork do projeto e submetê-lo com as implementações
- Atualizar o README com:
  - Tecnologias utilizadas
  - Instruções para rodar o projeto
- As instruções para envio do projeto deverão seguir as orientações enviadas pelo recrutador.

---

## 🌟 Release Notes

### Feature: Adicionando endpoint de Transações entre Contas

  - Adicionado suporte à realização de cargas em contas ativas.
  - O saldo é atualizado automaticamente após uma carga bem-sucedida.
  - Cargas com valores iguais ou inferiores a zero são rejeitadas.
  - Contas inativas não podem receber cargas.
  - Tentativas de carga em contas inexistentes retornam uma mensagem de erro apropriada.
  - Novos testes unitários garantem os principais cenários de sucesso e validação.
  
---
## Tecnologias Utilizadas

### Ferramentas de desenvolvimento

- VS Code 1.127.0
- C# Dev Kit
- REST Client
- .NET SDK 9
- Codex para o Spec e Implement - modelo gpt-5.4 (Low)
- Antigravity para ajustes, validação manual e LLM as a Judge - modelo Gemini 3.5 flash (Medium) 
- Microsoft.CodeAnalysis.Common

## Pre-requisitos

- .NET SDK 9 instalado
- Microsoft.CodeAnalysis.Common:
- Visual Studio Code com
   - Newtonsoft.Json
  - C# Dev Kit
  - REST Client

---

## Como Rodar o Projeto

### 1. Restaurar dependencias

```powershell
dotnet restore
```

### 2. Aplicar migrations no banco SQLite

Se o banco ainda nao existir ou se quiser garantir o schema atual:

```powershell
dotnet ef database update --project .\Festpay.Onboarding.Infra\Festpay.Onboarding.Infra.csproj --startup-project .\Festpay.Onboarding.Api\Festpay.Onboarding.Api.csproj
```

### 3. Build da Solucao

```powershell
dotnet build .\Festpay.Onboarding.Api.sln --verbosity normal
```

### 4. Executar a API

```powershell
dotnet run --project .\Festpay.Onboarding.Api\Festpay.Onboarding.Api.csproj
```

A API sobe localmente em uma porta semelhante a `https://localhost:7266`.

---

## Como Testar Manualmente

### Opção 1: Swagger

Com a API em execucao, acessar:

- `https://localhost:7266/swagger`

Se a porta mudar, usar a porta exibida no terminal do `dotnet run`.

### Opção 2: REST Client

O arquivo [Festpay.Onboarding.Api.http](/abs/path/D:/Code/bestway-onboarding-api/Festpay.Onboarding.Api/Festpay.Onboarding.Api.http) contém:

- criacao de contas de massa de teste
- listagem de contas para obter ids
- criacao de transacao valida
- cenarios invalidos de transacao
- listagem de transacoes
- busca por transacao por id
- cancelamento de transacao

Fluxo recomendado:

1. executar as duas requests de criacao de conta
2. executar `GET /api/v1/accounts`
3. copiar os ids retornados para `@originAccountId` e `@destinationAccountId`
4. executar `POST /api/v1/transactions`
5. executar `GET /api/v1/transactions`
6. copiar o id retornado para `@transactionId`
7. testar busca por id e cancelamento

---

## Como Rodar os Testes Unitários

### Testes de dominio

```powershell
dotnet test .\tests\Festpay.Onboarding.Domain.Tests\Festpay.Onboarding.Domain.Tests.csproj --verbosity normal
```

### Testes de aplicacao

```powershell
dotnet test .\tests\Festpay.Onboarding.Application.Tests\Festpay.Onboarding.Application.Tests.csproj --verbosity normal
```

### Solucao completa

```powershell
dotnet test .\Festpay.Onboarding.Api.sln --verbosity normal
```

---

## Observacoes

- a feature de transacoes reutiliza o mesmo `FestpayContext`, sem read model separado
- o cancelamento de transacao é lógico, preservando histórico
- os handlers seguem CQRS logico com commands e queries separados
- as regras intrinsecas da entidade ficam no dominio; regras dependentes de estado externo ficam na aplicacao
- o pacote `Microsoft.CodeAnalysis.Common` foi referenciado diretamente na camada de `Infra` para resolver um conflito transitivo de versão (Roslyn 4.11.0 vs 4.8.0) introduzido ao acoplar as dependencias do `Carter` e ferramentas de design do EF Core.

---
## 👨‍⚖️ LLM as a Judge

### Avaliacao do Spec Driven Development & Ajustes de Engenharia 

O desenvolvimento orientado a especificacoes (Spec Driven Development) com os arquivos na pasta [D:\Code\bestway-onboarding-api\.specs](file:///D:/Code/bestway-onboarding-api/.specs) foi fundamental para guiar o desenho de banco de dados, regras de negocio e a separacao de responsabilidades da arquitetura. 

### Desempenho do Modelo de Implementacao Base (Codex - gpt-5.4)

O modelo base realizou a implementacao estrutural do fluxo principal:
* Criou a entidade `Transaction` e estendeu `Account` seguindo o modelo rico de dominio.
* Mapeou corretamente as dependencias do Entity Framework no context e aplicou migrations.
* Implementou commands e queries com validadores FluentValidation seguindo CQRS.
* Escreveu testes unitarios cobrindo cenarios felizes e tristes no dominio e aplicacao.

No entanto, deixou escapar falhas criticas de integracao e tempo de execucao que impediam o funcionamento pratico do sistema:

### Ajustes e Melhorias de Engenharia Implementados (Antigravity - Gemini 3.5)

Para sanar as falhas de integracao e aprimorar a robustez do software, foram implementados os seguintes ajustes:

1. **Correcao na Descoberta de Modulos do Carter (SOLID / Encapsulamento)**:
   * **Problema**: O modelo base declarou classes de endpoint como `internal sealed`. Pelo comportamento padrao, o Carter varre apenas tipos publicos (`GetExportedTypes`), ocultando completamente as rotas de criacao de contas e transacoes no runtime e no Swagger.
   * **Solucao**: Em vez de simplesmente expor os tipos como `public` (o que enfraqueceria o encapsulamento do assembly da `Application`), criamos o metodo de extensao [AddCarterModules](file:///D:/Code/bestway-onboarding-api/Festpay.Onboarding.Infra/DependencyInjection.cs#L29-L39) em `Infra` que varre todos os tipos (publicos e internos) do assembly por reflexao (`GetTypes`) e os registra explicitamente.


2. **Correcao de Sintaxe de UUID na Massa de Testes**:
   * **Problema**: O arquivo [Festpay.Onboarding.Api.http](file:///D:/Code/bestway-onboarding-api/Festpay.Onboarding.Api/Festpay.Onboarding.Api.http#L3) continha o valor `@destinationAccountId` com 38 caracteres (9 no primeiro bloco e 13 no final), quebrando a desserializacao de JSON para `Guid` (erro 400 Bad Request).
   * **Solucao**: Corrigimos para um Guid de formato valido com 36 caracteres.


3. **Inclusao de Feature de Carga de Saldo (Charge - SOLID & CQRS)**:
   * **Problema**: Contas recem-criadas nasciam com saldo `0.0`, impossibilitando simular transacoes validas por estouro de saldo insuficiente.
   * **Solucao**: Desenvolvemos de forma nativa a feature [ChargeAccount.cs](file:///D:/Code/bestway-onboarding-api/Festpay.Onboarding.Application/Features/V1/Account/ChargeAccount.cs) (`POST /api/v1/accounts/{id}/charge`), implementando validador, comando e handler que manipula de forma rica o saldo chamando `account.Credit(amount)`.


4. **Validacao Anti-Duplicidade de Transacoes (Regra de Negocio)**:
   * **Problema**: Ausencia de protecao contra disparos acidentais duplicados (ex: clique duplo).
   * **Solucao**: Adicionamos validacao no [CreateTransaction.cs](file:///D:/Code/bestway-onboarding-api/Festpay.Onboarding.Application/Features/V1/Transaction/CreateTransaction.cs#L55-L68) que impede o envio de transacoes identicas (mesmo valor e contas) ocorridas dentro de um intervalo de 5 minutos.


5. **Estrategia de Depuracao via Scripts SQL (Evitando Endpoints fora do Padrao)**:
   * **Problema**: Necessidade de endpoints para reiniciar a base de dados de teste localmente, sem vazamento desses privilegios em builds de producao.
   * **Solucao**: Adotamos a abordagem correta de criar um script SQL dedicado, o [delete_debug_data.sql](file:///D:/Code/bestway-onboarding-api/delete_debug_data.sql), com as consultas estruturadas para limpeza direta da base SQLite local. Isso garante que a API mantenha-se pura, sem metodos de delecao provisorios inseridos fora do padrao de arquitetura.


## Agradecimentos
Se você chegou ler até aqui, muito obrigado !

---

## 🌟 Release Notes

### Feature: Adi