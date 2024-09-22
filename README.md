
# Documentação do Projeto MotoRent

## Introdução

Este projeto é uma solução robusta para gerenciamento de aluguel de motos e entregadores, utilizando uma arquitetura moderna e escalável baseada em **C# Net Core 8.0**. A aplicação está configurada para funcionar com uma infraestrutura Dockerizada, e conta com integrações com **MongoDB** para persistência de dados, **RabbitMQ** para mensageria, e **Minio** para armazenamento de imagens.

## Tecnologias Utilizadas

- **C# .Net Core 8.0**: O core da aplicação. Escolhemos essa versão por sua performance, suporte a microserviços e integração com o ecossistema .Net moderno. A aplicação segue os princípios de Clean Architecture, organizando bem as responsabilidades e facilitando a manutenção e evolução do código.

- **Docker**: Toda a infraestrutura do projeto é containerizada, permitindo fácil replicação do ambiente e deploy. Usamos **Docker Compose** para orquestrar todos os serviços necessários, o que facilita o setup do ambiente.

- **MongoDB**: Base de dados NoSQL que oferece flexibilidade para a estrutura de dados que estamos utilizando.

- **RabbitMQ**: Sistema de mensageria para lidar com filas e processamentos assíncronos, garantindo a comunicação entre os microserviços de maneira eficiente e escalável.

- **Minio**: Solução de armazenamento compatível com S3 para guardar imagens de forma segura e eficiente.

- **Serilog**: Biblioteca de logging utilizada para registrar eventos e erros de forma estruturada, facilitando o monitoramento e a resolução de problemas.

## Arquitetura do Projeto

 - O projeto segue os princípios da Clean Architecture, dividindo-se em camadas bem definidas para garantir a separação de responsabilidades, facilitar a manutenção e permitir a evolução do sistema. Abaixo, detalhamos cada camada e seu propósito:

### MotoRent.API:

 - Esta é a camada de apresentação, responsável por expor os endpoints da API RESTful.
 - Aqui estão definidos os controllers que lidam com as requisições HTTP.
 - Utiliza middleware para tratamento global de exceções, garantindo respostas consistentes em caso de erros.


### MotoRent.Application:

 - Contém a lógica de negócios e os casos de uso do sistema.
 - Implementa os serviços que orquestram as operações de negócio.
 - Utiliza DTOs (Data Transfer Objects) para comunicação entre camadas.
 - Implementa validações usando FluentValidation para garantir a integridade dos dados.


### MotoRent.Infrastructure:

- Responsável pela implementação concreta de acesso a dados e serviços externos.
- Contém os repositórios que abstraem o acesso ao MongoDB.
- Implementa a integração com RabbitMQ para mensageria.
- Gerencia o acesso ao Minio para armazenamento de arquivos.


### MotoRent.MessageConsumers:

 - Lida com o processamento assíncrono de mensagens do RabbitMQ.
 - Contém os consumidores que reagem a eventos do sistema.



## Por que esta arquitetura?

 - **Separação de Responsabilidades**: Cada camada tem um propósito claro, facilitando a manutenção e evolução do código.
 - **Desacoplamento**: As camadas superiores não dependem das inferiores, permitindo mudanças na infraestrutura sem afetar a lógica de negócios.
 - **Testabilidade**: A separação em camadas facilita a escrita de testes unitários e de integração.
 - **Escalabilidade**: A arquitetura permite escalar diferentes partes do sistema independentemente.
 - **Flexibilidade**: É possível trocar implementações (por exemplo, mudar o banco de dados) com mínimo impacto no restante do sistema.

## Tratamento de Erros e Logging
 **Middleware de Tratamento de Erros**
 - Implementamos um middleware personalizado para interceptar e tratar exceções de forma global. Este middleware:

 - Captura exceções não tratadas em toda a aplicação.
 - Formata as respostas de erro de maneira consistente.
 - Registra detalhes do erro usando Serilog para facilitar a depuração.

**Serilog**
 - Utilizamos o Serilog para logging estruturado. As principais vantagens incluem:

 - Logs estruturados que facilitam a análise e busca.
Configuração flexível para diferentes níveis de log e destinos (console, arquivo, etc.).
Integração com várias plataformas de monitoramento.

 - A configuração do Serilog está no Program.cs, permitindo logs detalhados em diferentes ambientes (desenvolvimento, produção).

## Setup do Ambiente

Antes de rodar a aplicação, você precisa configurar o ambiente Docker. Todo o setup de infraestrutura está definido no arquivo `docker-compose.yml`. Para facilitar o gerenciamento de credenciais e variáveis de ambiente, estamos utilizando um arquivo `.env`. Ele contém as configurações sensíveis, como credenciais de acesso aos serviços Docker (MongoDB, RabbitMQ, Minio, etc.).

### Comando para executar o Docker Compose

```bash
docker-compose --env-file .env up -d
```

Este comando irá subir todos os containers definidos, garantindo que os serviços MongoDB, RabbitMQ e Minio estejam prontos para uso.

### Estrutura do Arquivo .env

O arquivo `.env` contém as seguintes variáveis:

```
MONGO_INITDB_ROOT_USERNAME=seu_usuario
MONGO_INITDB_ROOT_PASSWORD=sua_senha
RABBITMQ_DEFAULT_USER=seu_usuario
RABBITMQ_DEFAULT_PASS=sua_senha
MINIO_ROOT_USER=seu_usuario
MINIO_ROOT_PASSWORD=sua_senha
```

Lembre-se de preencher esses campos com suas credenciais antes de rodar o `docker-compose`.

## Estrutura do Projeto

O projeto está dividido em quatro camadas principais, organizadas para manter a separação de responsabilidades e facilitar a escalabilidade e manutenção:

1. **MotoRent.API**:
   - Esta camada é responsável por expor os endpoints da API. Aqui, estão definidos todos os controllers que lidam com as requisições HTTP, coordenando o fluxo da aplicação. A API segue o padrão RESTful, garantindo que a comunicação seja simples e previsível.

2. **MotoRent.Application**:
   - Esta camada contém toda a lógica de negócios e os casos de uso do sistema. Aqui, implementamos as regras de negócio para lidar com aluguéis, devoluções, e a gestão de entregadores. Além disso, usamos **FluentValidation** para garantir que os dados que entram na aplicação estão corretos.

3. **MotoRent.Infrastructure**:
   - Aqui ficam as implementações concretas que lidam com a persistência de dados (MongoDB) e integração com outros serviços externos, como RabbitMQ e Minio. O princípio é manter tudo desacoplado, facilitando possíveis mudanças de tecnologia no futuro.

4. **MotoRent.MessageConsumers**:
   - Esta camada lida com a integração e processamento de mensagens através do RabbitMQ. É onde estão os consumidores de fila, responsáveis por processar eventos assíncronos, como notificações de novas reservas e devoluções de motos.

## Métodos da API

### POST `/api/Rentals`
Este endpoint permite a criação de um novo aluguel de moto. Ele aceita os dados do entregador, moto e período do aluguel. É aqui que a lógica de negócio para criar uma nova reserva é aplicada.

### GET `/api/Rentals/{id}`
Retorna os detalhes de um aluguel específico, com base no ID fornecido.

### PUT `/api/Rentals/{id}/return`
Este endpoint permite finalizar um aluguel. Quando uma moto é devolvida, ele processa a lógica de retorno e calcula o valor final, considerando possíveis atrasos ou danos ao veículo.

## MotoRent.IntegrationTests

Para garantir a qualidade e a funcionalidade da aplicação, criamos testes de integração abrangentes que cobrem os principais fluxos da API. Esses testes estão no projeto **MotoRent.IntegrationTests**, onde validamos as seguintes operações:

- Criação de aluguéis.
- Consulta de um aluguel específico.
- Finalização de um aluguel e verificação das regras de negócio aplicadas.

Os testes usam um ambiente de Docker com MongoDB e RabbitMQ configurados para simular um ambiente real de produção.

## Utilizando o Postman para Testar a MotoRent API

O **Postman** é uma ferramenta amplamente utilizada para testar APIs RESTful, permitindo a criação de requisições HTTP, organização de coleções de endpoints e gestão de variáveis de ambiente para facilitar o desenvolvimento e a depuração de APIs.

### Arquivos Disponíveis

Para facilitar o teste da **MotoRent API**, disponibilizamos dois arquivos que podem ser importados no Postman:

- **Moto Rent API.postman_collection.json**: Este arquivo contém uma coleção de requisições que podem ser utilizadas para testar os principais endpoints da API, como criação de aluguéis, consulta de motos disponíveis, autenticação e muito mais.
- **Moto Rent API.postman_environment.json**: Este arquivo define o ambiente de desenvolvimento, contendo as variáveis necessárias, como URLs da API, credenciais e tokens de autenticação. Isso permite que você teste a API sem precisar alterar manualmente os valores nas requisições.

### Como Importar os Arquivos no Postman

1. **Abra o Postman**: Certifique-se de que você já tenha o Postman instalado em sua máquina. Caso contrário, faça o download e a instalação [aqui](https://www.postman.com/downloads/).

2. **Importar a Coleção**:
   - No Postman, clique em **File > Import** ou no botão de **Importar** no canto superior esquerdo.
   - Selecione o arquivo **Moto Rent API.postman_collection.json** e importe-o.
   - Após a importação, a coleção da **MotoRent API** será exibida na barra lateral esquerda, contendo os endpoints organizados por categoria.

3. **Importar o Ambiente**:
   - Ainda no Postman, clique em **File > Import** ou no botão de **Importar**.
   - Selecione o arquivo **Moto Rent API.postman_environment.json** e importe-o.
   - Após a importação, vá até o menu de ambientes no canto superior direito do Postman e selecione o ambiente "MotoRent API". Isso garantirá que as variáveis de ambiente corretas serão utilizadas durante os testes.

### Testando a API

Após importar os arquivos, você poderá testar os diferentes endpoints da API. Abaixo estão algumas dicas de como utilizar o Postman para realizar os testes:

1. **Selecione o ambiente**: Certifique-se de que o ambiente **MotoRent API** está selecionado no menu superior direito. Isso garantirá que as variáveis como `{{baseUrl}}`, `{{token}}`, entre outras, serão resolvidas corretamente.

2. **Executar Requisições**: Na coleção importada, você encontrará uma lista de requisições organizadas por categorias. Clique em qualquer requisição e, em seguida, clique em **Send** para executá-la.

3. **Autenticação**: Algumas requisições requerem autenticação. Certifique-se de que o token JWT está configurado no ambiente do Postman. O arquivo **Moto Rent API.postman_environment.json** inclui uma variável `{{token}}` que você pode preencher com o token de autenticação necessário para realizar chamadas protegidas.

4. **Alterar Variáveis**: Caso precise alterar alguma variável, como a URL base da API (`{{baseUrl}}`), você pode fazer isso indo até o menu superior e clicando em **Environments > Edit** no ambiente **MotoRent API**.


## Conclusão

Este projeto foi construído pensando em escalabilidade e performance. A integração entre MongoDB, RabbitMQ e Minio garante que temos um sistema preparado para lidar com grandes volumes de dados e processamento assíncrono eficiente. O uso do **.Net Core 8.0** nos permite aproveitar as últimas funcionalidades da plataforma, enquanto Docker facilita o setup e o deploy em diferentes ambientes.

