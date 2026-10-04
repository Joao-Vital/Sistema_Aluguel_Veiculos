# Sistema de Aluguel de Veículos — Etapas 1 e 2 (Modelagem + Backend)

- **Modelo conceitual / entidades C#**:
**`Fabricante`** representa a marca/montadora responsável por um veículo (por exemplo, Chevrolet, Fiat, Toyota). É uma entidade simples, mas essencial, pois todo veículo do sistema precisa estar vinculado a um fabricante — essa relação é obrigatória e não pode ficar em branco.

**`CategoriaVeiculo`** é a quinta entidade do modelo, incluída além das quatro citadas originalmente no enunciado (item 1.5). Ela classifica os veículos por porte/perfil — como Popular, SUV ou Luxo — e carrega um valor de diária base para cada categoria, o que ajuda a padronizar a precificação e a organizar o catálogo de veículos da locadora.

**`Veiculo`** concentra as informações de cada automóvel disponível para locação: modelo, placa, ano de fabricação e quilometragem atual. Cada veículo está sempre associado a um `Fabricante` e a uma `CategoriaVeiculo`, o que permite tanto identificar a marca quanto classificar seu porte/faixa de preço.

**`Cliente`** guarda os dados de quem aluga os veículos: nome, CPF e e-mail, ambos únicos no sistema, além de um telefone de contato opcional. Essas informações são a base para identificar o locatário em cada contrato de aluguel.

**`Aluguel`** é a entidade central do processo de negócio: ela conecta um `Cliente` a um `Veiculo` durante um período determinado, com data de início e data prevista para devolução. Também registra a data em que a devolução de fato aconteceu, a quilometragem do veículo no início e no fim da locação, o valor da diária cobrada e o valor total apurado ao final do contrato — permitindo acompanhar tanto o uso do veículo quanto o faturamento gerado por cada locação.

- **DbContext** (`Data/LocadoraContext.cs`) traduzindo o modelo conceitual para o esquema relacional via Fluent API: chaves primárias (`[Key]`), chaves estrangeiras (`[ForeignKey]` + `HasOne/WithMany`), índices únicos (placa, CPF, e-mail, nomes) e `DeleteBehavior.Restrict` nas FKs para evitar múltiplos caminhos de cascade delete no SQL Server.

### Relacionamento das entidades:

```
Fabricante  1:N Veiculo
Veiculo     N:1 CategoriaVeiculo
Cliente     1:N Aluguel
Veiculo     1:N Aluguel
```

## O que está implementado (Etapa 2 — Backend)

- **Controllers RESTful** (`Controllers/`) com CRUD completo para as 5 entidades: `FabricantesController`, `CategoriasVeiculoController`, `ClientesController`, `VeiculosController` e `AlugueisController`. Cada um expõe `GET` (lista e por id), `POST`, `PUT` e `DELETE`.

- **DTOs** (`DTOs/`) separando o que a API recebe (`*CreateDto`/`*UpdateDto`) do que ela devolve (`*ReadDto`), evitando expor as entidades do EF diretamente e problemas de ciclo de referência no JSON.

- **Validação de entrada** via Data Annotations nos DTOs (`[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[RegularExpression]` para CPF) e validação cruzada de datas do aluguel (`IValidatableObject`). Erros de validação retornam `400` com um corpo padronizado listando os campos inválidos.

- **Validações de negócio** nos Controllers: CPF/e-mail/placa/nome únicos (`409 Conflict`), existência de fabricante/categoria/cliente/veículo antes de vincular (`404 Not Found`), impedir alugar veículo indisponível, impedir excluir registros com vínculos (fabricante com veículos, veículo/cliente com aluguéis), impedir excluir ou devolver duas vezes um aluguel já finalizado.

- **Regra de negócio do aluguel**: ao criar (`POST /api/alugueis`), o veículo fica marcado como indisponível e o valor total é calculado (diária × dias); ao registrar a devolução (`PATCH /api/alugueis/{id}/devolucao`), o veículo volta a ficar disponível, a quilometragem é atualizada e o valor total é recalculado com base no período efetivamente utilizado.

- **Tratamento global de exceções** (`Middleware/ExceptionHandlingMiddleware.cs`): qualquer erro não tratado é convertido em uma resposta JSON padronizada, sem expor stack trace.

- **5 rotas de filtro com JOIN**:

  | # |                             Rota                                    |           Junção usada         |            O que retorna              |
  |---|---------------------------------------------------------------------|--------------------------------|---------------------------------------|
  | 1 | `GET /api/veiculos/filtro/disponiveis-por-categoria/{categoriaId}`  | JOIN por navegação (`Include`) | Veículos disponíveis de uma categoria |
  | 2 | `GET /api/veiculos/filtro/fabricante/{fabricanteId}`                | JOIN explícito (`join` do LINQ)| Veículos de um fabricante com quantidade de aluguéis e valor arrecadado |
  | 3 | `GET /api/veiculos/filtro/sem-aluguel`                              | JOIN externo (`GroupJoin` + `DefaultIfEmpty`, equivalente a LEFT JOIN) | Veículos que nunca foram alugados |
  | 4 | `GET /api/alugueis/filtro/cliente/{clienteId}`                      | JOIN explícito encadeando 3 tabelas (Aluguel, Veículo, Fabricante) | Histórico de aluguéis de um cliente |
  | 5 | `GET /api/alugueis/filtro/periodo?inicio=AAAA-MM-DD&fim=AAAA-MM-DD` | JOIN por navegação (`Include`) | Aluguéis iniciados dentro de um período |

## O que está implementado (Etapa 3 — Testes e Documentação)

- **Swagger documentado (3.1)**: todas as actions dos Controllers agora têm comentários `///` (`<summary>`, `<param>`, `<response>`), e o `Program.cs`/`.csproj` foram configurados para gerar o XML de documentação e carregá-lo no Swagger (`IncludeXmlComments`). Ao rodar o projeto, cada endpoint no Swagger UI aparece com descrição, parâmetros e códigos de resposta explicados — não só o nome da rota.
- **Documento de endpoints (3.2)**: `Documentacao/Documentacao_Endpoints_API.docx` — lista todos os 30 endpoints (5 controllers de CRUD + 5 rotas de filtro), com método HTTP, parâmetros, corpo da requisição e códigos de resposta possíveis.
- **Template do relatório de testes (3.3)**: `Documentacao/Template_Relatorio_Testes.docx` — um bloco por teste (29 testes cobrindo todos os endpoints, incluindo casos de erro como CPF duplicado, veículo indisponível e exclusão com vínculo), já com a requisição sugerida e o resultado esperado preenchidos. **Este arquivo precisa ser completado por você**: rode o projeto, execute cada teste no Swagger, preencha o código/corpo da resposta obtido e cole o print de tela no espaço indicado.


## Estrutura de pastas

```
LocadoraVeiculos.sln
LocadoraVeiculos.API/
 ├─ LocadoraVeiculos.API.csproj
 ├─ Program.cs
 ├─ appsettings.json
 ├─ Models/
 │   ├─ Fabricante.cs
 │   ├─ CategoriaVeiculo.cs
 │   ├─ Veiculo.cs
 │   ├─ Cliente.cs
 │   └─ Aluguel.cs
 ├─ DTOs/
 │   ├─ FabricanteDto.cs
 │   ├─ CategoriaVeiculoDto.cs
 │   ├─ VeiculoDto.cs
 │   ├─ ClienteDto.cs
 │   └─ AluguelDto.cs
 ├─ Controllers/
 │   ├─ FabricantesController.cs
 │   ├─ CategoriasVeiculoController.cs
 │   ├─ VeiculosController.cs
 │   ├─ ClientesController.cs
 │   └─ AlugueisController.cs
 ├─ Middleware/
 │   └─ ExceptionHandlingMiddleware.cs
 └─ Data/
     └─ LocadoraContext.cs
```
