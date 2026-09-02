# Queue Simulator

Simulador de fila simples desenvolvido para a disciplina de Simulação e Métodos Analíticos (2026/2) — PUCRS.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Como rodar

```bash
# G/G/1/5
dotnet run gg15

# G/G/2/5
dotnet run gg25

# arquivo customizado
dotnet run <nome-do-arquivo>   # com ou sem .json

# modo debug (imprime cada evento)
dotnet run <nome-do-arquivo> --debug

# menu interativo
dotnet run

# criar arquivo de modelo
dotnet run -- --create-model <nome-do-arquivo>
```

## Configuração

Os parâmetros da simulação são definidos em um arquivo `.json`:

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

| Campo | Descrição |
|---|---|
| `Servers` | Número de servidores |
| `MaxCapacity` | Capacidade máxima da fila |
| `NumberOfEvents` | Não utilizado — simulação encerra ao consumir 100.000 aleatórios |
| `FirstArrivalTime` | Tempo da primeira chegada |
| `MinArrivalTime` / `MaxArrivalTime` | Intervalo de tempo entre chegadas |
| `MinServiceTime` / `MaxServiceTime` | Intervalo de tempo de atendimento |
