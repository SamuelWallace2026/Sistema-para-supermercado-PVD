# Sistema para Supermercado: PDV e Retaguarda

Sistema desktop para supermercado de bairro, com frente de caixa (PDV) operada por teclado e uma área de gerência para produtos, estoque e relatórios.

> Status: em desenvolvimento.

## Funcionalidades

**Frente de caixa (PDV)**
- Login separado para funcionário e administrador
- Leitura de código de barras (o leitor funciona como teclado) e pesquisa de produtos por nome
- Operação por atalhos de teclado, sem uso do mouse durante a venda (por exemplo, F2 pesquisar produto, F5 nova venda, F10 finalizar venda)
- Alterar quantidade e excluir item do cupom
- Pagamento com seleção rápida da forma de pagamento, pagamento dividido em várias formas e cálculo de troco
- Cupom com pré-visualização antes do envio para a impressora

**Retaguarda (gerente)**
- Cadastro de produtos com código de barras
- Controle de estoque
- Relatórios de vendas com impressão
- Configurações do sistema

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Linguagem e plataforma | C#, .NET 10 |
| Interface | WPF (XAML) |
| Acesso a dados | Entity Framework Core com Pomelo (MySQL) |
| Banco de dados | MySQL / MariaDB |

## Estrutura da solução

```
Supermercado.PDV/     # Interface WPF (telas de login, caixa, pagamento e gerência)
Supermercado.Data/    # Contexto do banco (AppDbContext) e carga inicial
Supermercado.Domain/  # Entidades (Produto, ItemVenda, Venda, Usuário...)
```

## Como executar

### Pré-requisitos

- Windows
- .NET SDK 10
- MySQL ou MariaDB em execução

### Passos

1. Clone o repositório.
2. Configure a conexão com o banco no arquivo `AppDbContext.cs`, em `Supermercado.Data`, com o seu servidor, usuário e senha do MySQL.
3. Execute o projeto:

```bash
dotnet run --project Supermercado.PDV
```

Na primeira execução, o banco `supermercado_db` e as tabelas são criados automaticamente, e um usuário administrador inicial é criado pela carga inicial (`DbInitializer`). Altere a senha desse usuário antes de usar o sistema de verdade.

## Segurança

- Não publique senhas de banco no código. Use variáveis de ambiente ou um arquivo de configuração fora do repositório.
- Este repositório contém apenas dados de exemplo.

## Autor

**Samuel Wallace Liberato Queiroz**
Estudante de Engenharia de Software, UniJaguaribe, Aracati/CE
[github.com/SamuelWallace2026](https://github.com/SamuelWallace2026)
