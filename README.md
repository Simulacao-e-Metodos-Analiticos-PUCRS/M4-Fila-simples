# QueueSimulator

Simulador de filas de espera G/G/c/K com eventos discretos.

## Pré-requisitos

- [.NET SDK 10+](https://dotnet.microsoft.com/download)

Para instalar via Homebrew (macOS):
```bash
brew install --cask dotnet-sdk
```

## Como executar

**1. Criar o arquivo de parâmetros:**
```bash
dotnet run -- --create-model
```

**2. Editar o `model.json`** com os parâmetros da sua fila:
```json
{
    "Servers": 2,
    "MaxCapacity": 2,
    "NumberOfEvents": 100,
    "FirstArrivalTime": 1.0,
    "MinArrivalTime": 1.0,
    "MaxArrivalTime": 2.0,
    "MinServiceTime": 2.0,
    "MaxServiceTime": 3.0
}
```

| Campo | Descrição |
|---|---|
| `Servers` | Número de servidores paralelos |
| `MaxCapacity` | Capacidade máxima do sistema (`null` para ilimitado) |
| `NumberOfEvents` | Número de eventos a processar |
| `FirstArrivalTime` | Tempo da primeira chegada |
| `MinArrivalTime` / `MaxArrivalTime` | Intervalo entre chegadas |
| `MinServiceTime` / `MaxServiceTime` | Tempo de atendimento |

**3. Executar a simulação:**
```bash
dotnet run -- model.json
```

**4. Executar com debug** (exibe cada evento):
```bash
dotnet run -- model.json --debug
```

**5. Menu interativo** (sem argumentos):
```bash
dotnet run
```
