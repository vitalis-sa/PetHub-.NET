# Vitalis API

API RESTful desenvolvida em **ASP.NET Core (.NET 10)** responsável pelo domínio do **Responsável** no sistema PetHub.

O PetHub é um sistema veterinário composto por dois backends que compartilham o mesmo banco Oracle:

| Backend | Tecnologia | Responsabilidade |
|---|---|---|
| **Vitalis (este)** | C# .NET 10 | Cadastro e autenticação de Responsáveis, Endereços, Contatos e Lembretes |
| **pethub-java** | Java 21 + Spring Boot 3 | Veterinários, Pets, Consultas, Diagnósticos, Vacinas e Wearable IoT |

O app mobile consome ambos os backends. O Java chama a API do Vitalis para buscar responsáveis por CPF e para criar lembretes de eventos veterinários.

Na **3ª sprint** a aplicação ganhou uma camada de **monitoramento e observabilidade** (Health Checks, logging estruturado com Serilog e tracing/métricas com OpenTelemetry) e uma suíte de **testes automatizados** no padrão AAA, com 127 testes unitários e de integração.

---

## Tecnologias

**Aplicação**

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + Oracle.EntityFrameworkCore
- BCrypt.Net-Next (hash de senhas)
- Swashbuckle (Swagger / OpenAPI 3.0)

**Monitoramento e observabilidade**

- `Microsoft.Extensions.Diagnostics.HealthChecks` — verificações de saúde da API
- **Serilog** (`Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`) — logging estruturado
- **OpenTelemetry** (`Extensions.Hosting`, `Instrumentation.AspNetCore`, `Instrumentation.Http`, `Exporter.Console`, `Exporter.OpenTelemetryProtocol`) — tracing distribuído e métricas

**Testes**

- **xUnit** — framework de testes
- **Moq** — mocking de dependências
- **FluentAssertions** — asserções expressivas
- `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) — testes de integração
- `Microsoft.EntityFrameworkCore.InMemory` — banco em memória nos testes de integração
- **coverlet** — cobertura de código

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Acesso ao banco Oracle (FIAP ou local)
- `dotnet-ef` instalado globalmente:

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

---

## Instalação e execução

```bash
# 1. Clonar o repositório
git clone https://github.com/vitalis-sa/PetHub-.NET.git
cd PetHub-.NET

# 2. Restaurar dependências
dotnet restore

# 3. Configurar a string de conexão em appsettings.json
# "OracleConnection": "User Id=SEU_USER;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/orcl"

# 4. Aplicar as migrations no banco
dotnet ef database update

# 5. Rodar a aplicação
dotnet run
```

A API sobe em `http://localhost:5192`.
O Swagger fica disponível em `http://localhost:5192/swagger`.

---

## Estrutura da solução

```
Vitalis/
├── Vitalis.slnx                  # Solução: API + projetos de teste
├── Controllers/                  # Endpoints da API (instrumentados com ILogger, Activity e Counter)
├── Dados/AppDbContext.cs
├── Dto/  Models/  Repositories/  Migrations/
├── Health/                       # Health Checks customizados (3ª sprint)
│   ├── BancoDadosHealthCheck.cs        # Conectividade com o Oracle
│   ├── ServicoExternoHealthCheck.cs    # Disponibilidade do backend Java
│   └── RespostaHealthCheck.cs          # ResponseWriter JSON do endpoint /health
├── Middlewares/
│   └── CorrelationIdMiddleware.cs      # X-Correlation-ID no log estruturado
├── Observabilidade/
│   └── AplicacaoMetricas.cs            # Meter, Counters e ActivitySource da aplicação
├── Program.cs                    # Serilog, Health Checks e OpenTelemetry
└── tests/
    ├── Vitalis.Tests.Unit/             # Testes unitários (79)
    │   ├── Dominio/  Aplicacao/  Fixtures/
    └── Vitalis.Tests.Integration/      # Testes de integração (48)
        ├── Endpoints/  Monitoramento/  Fixtures/
```

---

## Monitoramento e Observabilidade

### Health Checks

A API expõe o endpoint nativo de diagnóstico em **`GET /health`**, construído sobre
`Microsoft.Extensions.Diagnostics.HealthChecks`. Ele agrega duas verificações customizadas
que implementam a interface `IHealthCheck`:

| Verificação | O que faz | Resultado |
|---|---|---|
| `banco_dados` | Testa a conexão com o Oracle através do `AppDbContext` e mede a latência | `Healthy` ou `Unhealthy`, sempre com `LatenciaMs` |
| `servico_externo` | Faz um `GET` HTTP no backend Java (`ServicosExternos:PethubJava`) | `Healthy` ou `Unhealthy`, com `LatenciaMs` e `StatusCode` |

O status agregado define o código HTTP:

| Status geral | Campo `status` | HTTP |
|---|---|---|
| Todas as verificações saudáveis | `Healthy` | `200 OK` |
| Qualquer verificação com falha | `Unhealthy` | `503 Service Unavailable` |

O corpo é escrito em **JSON** por um `ResponseWriter` customizado
(`Health/RespostaHealthCheck.cs`), registrado no `MapHealthChecks` via `HealthCheckOptions`.
Em vez do texto simples com o status agregado, ele detalha **cada verificação**:

| Campo | Conteúdo |
|---|---|
| `status` | Status agregado da API |
| `duracaoTotalMs` | Tempo total gasto no conjunto de verificações |
| `verificadoEm` | Instante (UTC) da coleta |
| `verificacoes[].nome` | Nome registrado no `AddCheck<>` (`banco_dados`, `servico_externo`) |
| `verificacoes[].status` | `Healthy`, `Degraded` ou `Unhealthy` |
| `verificacoes[].descricao` | Mensagem devolvida pelo `IHealthCheck` |
| `verificacoes[].duracaoMs` | Duração daquela verificação isolada |
| `verificacoes[].dados` | Dados coletados pelo check (`LatenciaMs`, `StatusCode`) |
| `verificacoes[].erro` | Mensagem da exceção, quando a verificação falha |

Como monitorar pelo terminal:

```bash
# Corpo completo da resposta
curl -s http://localhost:5192/health | jq

# Apenas o status agregado
curl -s http://localhost:5192/health | jq -r .status

# Apenas o código HTTP — útil para orquestradores (Kubernetes, Azure)
curl -o /dev/null -w "%{http_code}\n" http://localhost:5192/health
```

Exemplo de saída com o banco no ar e o backend Java fora:

```json
{
  "status": "Unhealthy",
  "duracaoTotalMs": 2043.51,
  "verificadoEm": "2026-09-12T22:34:07.1183920+00:00",
  "verificacoes": [
    {
      "nome": "banco_dados",
      "status": "Healthy",
      "descricao": "Conexão com o Banco de Dados estabelecida com sucesso.",
      "duracaoMs": 41.27,
      "dados": {
        "LatenciaMs": 39
      }
    },
    {
      "nome": "servico_externo",
      "status": "Unhealthy",
      "descricao": "Serviço externo (pethub-java) inacessível.",
      "duracaoMs": 2043.18,
      "dados": {
        "LatenciaMs": 2042
      },
      "erro": "Connection refused (localhost:8080)"
    }
  ]
}
```

> Para simular uma falha de conexão em sala, basta apontar a `OracleConnection`
> para um host inexistente ou parar o backend Java: o `/health` passa a responder
> `Unhealthy` com HTTP 503.

### Logging estruturado (Serilog)

O Serilog substitui o provedor de logging padrão da Microsoft (`builder.Host.UseSerilog()`)
e grava em dois **Sinks** simultâneos:

| Sink | Destino |
|---|---|
| Console | Template `[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}` |
| Arquivo | `logs/vitalis-YYYYMMDD.log`, com rotação diária |

**Níveis de log usados nos controllers:**

| Nível | Quando |
|---|---|
| `Information` | Início das consultas, cadastros e atualizações bem-sucedidas |
| `Warning` | Recurso não encontrado, credenciais inválidas, Service Token inválido, dados rejeitados |
| `Error` | Falhas inesperadas nos endpoints de escrita (`POST /api/responsavel/cadastro` e `POST /api/lembretes`): o `try/catch` chama `_logger.LogError(ex, ...)` com a exceção completa, marca o span como `Error`, contabiliza a métrica com `status=erro_interno` e devolve `500` |

**Correlation ID:** o `CorrelationIdMiddleware` lê o header `X-Correlation-ID` da requisição
(ou gera um novo GUID), devolve o valor no header da resposta e o injeta no `LogContext` do
Serilog — de modo que **todos os logs daquela requisição carregam o mesmo identificador**,
permitindo rastrear a chamada de ponta a ponta.

```bash
# A resposta ecoa o mesmo identificador enviado
curl -i -H "X-Correlation-ID: aula-42" http://localhost:5192/api/responsavel
```

Saída correspondente no console:

```
[22:34:07 WRN] [aula-42] Criação de lembrete recusada por Service Token inválido.
[22:34:09 ERR] [aula-42] Falha inesperada ao criar lembrete do tipo VACINA para o responsável 1.
Oracle.ManagedDataAccess.Client.OracleException: ORA-12541: TNS:no listener
   at Vitalis.Repositories.LembreteRepository.Add(Lembrete lembrete)
```

### Tracing distribuído e métricas (OpenTelemetry)

O OpenTelemetry é registrado em `Program.cs` com o nome de serviço `Vitalis.API` e exporta
tudo no **Console** (`AddConsoleExporter`), permitindo inspecionar traces e métricas
diretamente no terminal do Kestrel.

**Tracing** — combina auto-instrumentação com spans manuais:

| Fonte | O que rastreia |
|---|---|
| `AddAspNetCoreInstrumentation()` | Requisições HTTP recebidas pela API |
| `AddHttpClientInstrumentation()` | Chamadas HTTP de saída (ex.: verificação do serviço externo) |
| `AddSource("Vitalis.API")` | Spans manuais criados com o `ActivitySource` da aplicação |

Os endpoints de criação abrem um span próprio via `ActivitySource` e anexam tags de negócio.
Em caso de falha, o span é marcado com `ActivityStatusCode.Error`:

```
Activity.DisplayName:        CriarLembreteEndpoint
Activity.Kind:               Internal
Activity.Duration:           00:00:00.0006399
Activity.Tags:
    lembrete.tipo: VACINA
    lembrete.responsavelId: 1
```

**Métricas** — a classe `AplicacaoMetricas` centraliza o `Meter` da aplicação:

| Métrica | Tipo | Descrição |
|---|---|---|
| `responsaveis_cadastrados_total` | Counter | Responsáveis cadastrados, com a tag `status` |
| `lembretes_criados_total` | Counter | Lembretes criados, com a tag `status` |

A tag `status` distingue os desfechos (`sucesso`, `erro_validacao`, `erro_cpf_duplicado`,
`erro_token`), o que permite acompanhar a **taxa de erros** por operação de negócio:

```
Metric Name: lembretes_criados_total, Description: Contagem total de lembretes criados na API, Unit: {lembretes}
Instrumentation scope (Meter):
	Name: Vitalis.API
	Version: 1.0.0
(...) status: erro_token
Value: 1
```

Já o **tempo de resposta** de todas as rotas vem da auto-instrumentação
`AddAspNetCoreInstrumentation()`, que publica o histograma `http.server.request.duration`
particionado por rota e status HTTP:

```
Metric Name: http.server.request.duration, Description: Duration of HTTP server requests., Unit: s, Metric Type: Histogram
(...) http.request.method: GET http.response.status_code: 503 http.route: /health
Value: Sum: 16.0242643 Count: 1 Min: 16.0242643 Max: 16.0242643
```

O pacote `OpenTelemetry.Exporter.OpenTelemetryProtocol` também está instalado: basta trocar
`AddConsoleExporter()` por `AddOtlpExporter()` para enviar os dados a um coletor externo
(Jaeger, Prometheus, Grafana ou Aspire Dashboard).

### Configuração

Em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=...;Password=...;Data Source=oracle.fiap.com.br:1521/orcl"
  },
  "ServiceToken": "pethub-internal-secret-2025",
  "ServicosExternos": {
    "PethubJava": "http://localhost:8080/actuator/health"
  }
}
```

- `ServiceToken` — token compartilhado com o backend Java para proteger os endpoints de integração.
- `ServicosExternos:PethubJava` — URL consultada pelo health check `servico_externo`.

---

## Testes automatizados

A solução tem **127 testes** organizados em dois projetos separados por camada:

| Projeto | Testes | Escopo |
|---|---|---|
| `tests/Vitalis.Tests.Unit` | 79 | Camadas de **Domínio** e **Aplicação**, com dependências isoladas por Moq |
| `tests/Vitalis.Tests.Integration` | 48 | Fluxo HTTP completo via `WebApplicationFactory` |

### Como executar

```bash
# Todos os testes da solução
dotnet test

# Somente os testes unitários
dotnet test tests/Vitalis.Tests.Unit

# Somente os testes de integração
dotnet test tests/Vitalis.Tests.Integration

# Com relatório de cobertura (gera coverage.cobertura.xml em TestResults/)
dotnet test --collect:"XPlat Code Coverage"

# Saída detalhada, com o nome de cada teste executado
dotnet test --logger "console;verbosity=detailed"

# Filtrar por classe ou por nome
dotnet test --filter FullyQualifiedName~LembretesApiControllerTests
dotnet test --filter DisplayName~Unauthorized
```

No Visual Studio: **Teste → Gerenciador de Testes** (`Ctrl + E, T`) e **Executar Todos os Testes**.

Os testes **não dependem do banco Oracle**: os unitários substituem os repositórios por mocks
e os de integração trocam o `AppDbContext` por um banco em memória exclusivo de cada execução.

Para visualizar a cobertura em HTML:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Padrão AAA e nomenclatura

Todos os testes seguem o padrão **AAA (Arrange, Act, Assert)**, com as três etapas comentadas,
e a convenção **`MetodoTestado_Cenario_ResultadoEsperado`**:

```csharp
[Fact]
public void Cadastrar_DadosValidos_DeveSalvarNoRepositorioERetornarCreated()
{
    // Arrange
    var dto = NovoCadastroDto();
    _repositorioMock.Setup(r => r.GetByCpf(dto.Cpf)).Returns((Responsavel?)null);
    _repositorioMock.Setup(r => r.Add(It.IsAny<Responsavel>())).Callback<Responsavel>(r => r.Id = 10);
    var controller = CriarController();

    // Act
    var resultado = controller.Cadastrar(dto);

    // Assert
    var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
    created.RouteValues!["id"].Should().Be(10L);
    // Verifica se o método Add do repositório foi chamado exatamente 1 vez
    _repositorioMock.Verify(r => r.Add(It.Is<Responsavel>(x => x.Cpf == dto.Cpf)), Times.Once);
}
```

| Exemplo | Leitura |
|---|---|
| `GetById_ResponsavelInexistente_DeveRetornarNotFound` | Método `GetById`, cenário "responsável não existe", esperado `404` |
| `PostLembrete_SemServiceToken_DeveRetornar401` | Criação de lembrete sem o token de integração |
| `Construtor_NomeInvalido_DeveRetornarErroNoCampoNome` | Regra de validação do domínio |

### Fixtures e Collection Fixtures

O contexto compartilhado entre os testes é montado com os recursos do xUnit:

| Fixture | Tipo | Compartilha |
|---|---|---|
| `ConfiguracaoFixture` | `IClassFixture` | `IConfiguration` com o `ServiceToken` e a montagem do `HttpContext` dos controllers |
| `VitalisWebApplicationFactory` | `ICollectionFixture` | Uma única instância da API em memória para todas as classes de teste de integração |

### O que os testes cobrem

**Unitários — Domínio** (`tests/Vitalis.Tests.Unit/Dominio`)
Regras de validação de `Responsavel`, `ResponsavelEndereco`, `ResponsavelContato` e `Lembrete`:
campos obrigatórios, limites de tamanho (nome, CPF, UF, CEP), valores padrão e os enums
`TipoLembrete` / `StatusLembrete`. Usa `[Theory]` com `[InlineData]` para os cenários parametrizados.

**Unitários — Aplicação** (`tests/Vitalis.Tests.Unit/Aplicacao`)
Os quatro controllers com os repositórios simulados por Moq: caminhos de sucesso, `404`, `400`
por `ModelState` inválido, `409` de CPF duplicado, `401` de login e de `X-Service-Token`, `500`
com registro de `LogError` quando o repositório lança exceção, e verificação, com `Times.Once` e
`Times.Never`, de que o repositório é (ou não) chamado.

**Integração — Endpoints** (`tests/Vitalis.Tests.Integration/Endpoints`)
Fluxo HTTP completo: cadastro e login, garantia de que a senha nunca aparece na resposta,
`400` para JSON malformado e payload incompleto, `409` de CPF duplicado, autenticação por
`X-Service-Token` (ausente, inválido e válido), CRUD de lembretes, isolamento entre
responsáveis nos recursos aninhados e a regra de endereço/contato principal.

**Integração — Monitoramento** (`tests/Vitalis.Tests.Integration/Monitoramento`)
O endpoint `/health`: status agregado e HTTP `503`, `Content-Type` `application/json`, o
detalhamento de cada verificação no corpo (nome, status, descrição e duração) e a
identificação de qual check falhou. Cobre também o eco do header `X-Correlation-ID`, a
geração de identificadores distintos por requisição e a disponibilidade do documento OpenAPI.

---

## Documentação das rotas

### Monitoramento

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/health` | Saúde da API: conexão com o Oracle e disponibilidade do backend Java |

### Responsáveis — `/api/responsavel`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/responsavel` | Lista todos os responsáveis | — |
| `GET` | `/api/responsavel/{id}` | Busca responsável por ID com endereços e contatos | — |
| `GET` | `/api/responsavel/buscar?cpf={cpf}` | Busca responsável por CPF (chamado pelo Java) | `X-Service-Token` |
| `POST` | `/api/responsavel/cadastro` | Cadastra novo responsável | — |
| `POST` | `/api/responsavel/login` | Autentica responsável por e-mail e senha | — |
| `PUT` | `/api/responsavel/{id}` | Atualiza dados do responsável | — |
| `DELETE` | `/api/responsavel/{id}` | Remove responsável | — |

### Endereços — `/api/responsavel/{responsavelId}/enderecos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsavel/{responsavelId}/enderecos` | Lista endereços do responsável |
| `GET` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Busca endereço por ID |
| `POST` | `/api/responsavel/{responsavelId}/enderecos` | Adiciona endereço ao responsável |
| `PUT` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Atualiza endereço |
| `DELETE` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Remove endereço |
| `PATCH` | `/api/responsavel/{responsavelId}/enderecos/{id}/principal` | Define como endereço principal |

> Ao marcar um endereço como principal, os demais são automaticamente desmarcados.

### Contatos — `/api/responsavel/{responsavelId}/contatos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsavel/{responsavelId}/contatos` | Lista contatos do responsável |
| `GET` | `/api/responsavel/{responsavelId}/contatos/{id}` | Busca contato por ID |
| `POST` | `/api/responsavel/{responsavelId}/contatos` | Adiciona contato ao responsável |
| `PUT` | `/api/responsavel/{responsavelId}/contatos/{id}` | Atualiza contato |
| `DELETE` | `/api/responsavel/{responsavelId}/contatos/{id}` | Remove contato |
| `PATCH` | `/api/responsavel/{responsavelId}/contatos/{id}/principal` | Define como contato principal |

### Lembretes — `/api/lembretes`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/lembretes` | Lista todos os lembretes | — |
| `GET` | `/api/lembretes/{id}` | Busca lembrete por ID | — |
| `GET` | `/api/lembretes/responsavel/{responsavelId}` | Lista lembretes de um responsável | — |
| `GET` | `/api/lembretes/responsavel/{responsavelId}/tipo/{tipo}` | Filtra por tipo (VACINA, CONSULTA, EXAME, MEDICAMENTO, HIDRATACAO) | — |
| `POST` | `/api/lembretes` | Cria lembrete (chamado pelo Java) | `X-Service-Token` |
| `PATCH` | `/api/lembretes/{id}/status` | Atualiza status do lembrete | — |
| `DELETE` | `/api/lembretes/{id}` | Remove lembrete | — |

> No **corpo** das requisições e respostas, `tipo` e `status` trafegam como o valor numérico do enum
> (`TipoLembrete`: `0` VACINA, `1` CONSULTA, `2` EXAME, `3` MEDICAMENTO, `4` HIDRATACAO;
> `StatusLembrete`: `0` PENDENTE, `1` ENVIADO, `2` FALHOU). Na **rota** de filtro por tipo, o nome
> do enum é aceito diretamente (`.../tipo/VACINA`).

---

## Integração com o backend Java

O backend Java (`pethub-java`) chama dois endpoints deste serviço:

**Buscar responsável por CPF** — ao cadastrar um Pet na clínica:
```
GET /api/responsavel/buscar?cpf=00000000000
Header: X-Service-Token: {valor configurado}
```

**Criar lembrete** — ao registrar vacinas, consultas ou pedidos médicos:
```
POST /api/lembretes
Header: X-Service-Token: {valor configurado}
Body: { responsavelId, petId, tipo, dataAgendada, mensagem, referenciaId, referenciaTipo }
```

---

## Senhas e segurança

- Senhas dos responsáveis são armazenadas com hash **BCrypt** — nunca em texto puro
- A senha **nunca é retornada** em nenhum response da API (verificado por teste de integração)
- Endpoints de integração com o Java são protegidos por `X-Service-Token` no header

---

## Integrantes

- Pedro Chasci Puga — RM565154
- Ana Flavia Camelo — RM561489
- Gustavo kenji Terada — RM562745
- João Guilherme Carvalho Novaes — RM566234
- Lucas Figueiredo Vieira — RM561342
