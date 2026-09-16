# Queue Simulator

Simulador de eventos discretos para **filas simples** e **filas em tandem**, desenvolvido para a disciplina de Simulação e Métodos Analíticos (2026/2) — Escola Politécnica, PUCRS. Prof. Afonso Sales.

**Equipe:** Augusto Sanhudo da Silva Knob · Carlos Eduardo Brito Mascarello · Matheus Hrymalak Souza · Olivia Maite Furquim Araujo Livak

O simulador cobre as duas etapas do trabalho:

- **M4 — fila simples:** uma fila `G/G/c/K` com chegadas externas;
- **M6 — filas em tandem:** filas ligadas por probabilidades de roteamento, com o caso `Fila 1 → Fila 2` como validação.

O formato do arquivo de configuração determina qual dos dois modelos será executado.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Configuração

Abra no terminal a pasta que contém `QueueSimulator.csproj`. Os arquivos `.json` devem ficar nessa pasta.

- Arquivo **sem** a chave `Queues`: fila simples (M4).
- Arquivo **com** a chave `Queues`: filas em tandem (M6).

### Fila simples

Exemplo: [`gg15.json`](gg15.json)

```json
{
    "Servers": 1,
    "MaxCapacity": 5,
    "NumberOfEvents": 999999,
    "FirstArrivalTime": 3.0,
    "MinArrivalTime": 3.0,
    "MaxArrivalTime": 5.0,
    "MinServiceTime": 4.0,
    "MaxServiceTime": 5.0
}
```

`Servers` define os servidores; `MaxCapacity` define a capacidade (`null` para ilimitada); `FirstArrivalTime` define a primeira chegada; `MinArrivalTime` e `MaxArrivalTime` definem o intervalo de chegadas; e `MinServiceTime` e `MaxServiceTime` definem o intervalo de atendimento.

### Filas em tandem

Exemplo: [`tandem.json`](tandem.json)

```json
{
    "FirstArrivalTime": 2.5,
    "Queues": [
        {
            "Name": "Fila 1",
            "Servers": 2,
            "Capacity": 3,
            "MinArrival": 1.0,
            "MaxArrival": 5.0,
            "MinService": 4.0,
            "MaxService": 5.0,
            "Routes": [ { "To": "Fila 2", "Probability": 1.0 } ]
        },
        {
            "Name": "Fila 2",
            "Servers": 1,
            "Capacity": 5,
            "MinService": 1.0,
            "MaxService": 3.0,
            "Routes": []
        }
    ]
}
```

Cada fila informa `Name`, `Servers`, `Capacity`, `MinService` e `MaxService`. `MinArrival` e `MaxArrival` devem ser informados apenas nas filas que recebem chegadas externas. Em `Routes`, `To` indica a fila de destino e `Probability` indica a probabilidade de roteamento. Uma lista vazia significa que o cliente deixa o sistema após o atendimento.

## Execução

Na raiz do projeto, execute:

```bash
# Filas em tandem da validação do trabalho
dotnet run tandem

# Fila simples G/G/1/5
dotnet run gg15

# Fila simples G/G/2/5
dotnet run gg25
```

O sufixo `.json` é opcional. Para executar outro arquivo:

```bash
dotnet run meu-modelo
```

Para imprimir cada evento da fila simples em modo de depuração:

```bash
dotnet run gg15 --debug
```

Para salvar o resultado em um arquivo:

```bash
dotnet run tandem > resultado-tandem.txt
```

O relatório exibe os estados das filas, tempos acumulados, probabilidades, clientes perdidos, tempo global da simulação e eventos processados.

## Estrutura do código

| Arquivo | Responsabilidade |
|---|---|
| [`RandomGen.cs`](RandomGen.cs) | Gerador de números pseudoaleatórios |
| [`Fila.cs`](Fila.cs) | Configuração, estado e estatísticas de uma fila |
| [`Evento.cs`](Evento.cs) | Eventos da simulação |
| [`Escalonador.cs`](Escalonador.cs) | Fila de prioridade dos eventos |
| [`Rede.cs`](Rede.cs) | Topologia, roteamento e leitura do JSON das filas |
| [`Simulador.cs`](Simulador.cs) | Simulação de filas em tandem |
| [`Simulator.cs`](Simulator.cs) | Simulação de fila simples |
| [`ConsolePrinter.cs`](ConsolePrinter.cs) | Impressão dos relatórios |
| [`Program.cs`](Program.cs) | Linha de comando e menu interativo |
