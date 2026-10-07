# Queue Simulator

Simulador de eventos discretos para **redes de filas com qualquer topologia**, desenvolvido para a disciplina de Simulação e Métodos Analíticos (2026/2) — Escola Politécnica, PUCRS. Prof. Afonso Sales.

**Equipe:** Augusto Sanhudo da Silva Knob · Carlos Eduardo Brito Mascarello · Matheus Hrymalak Souza · Olivia Maite Furquim Araujo Livak

O modelo é lido de um arquivo `.yml` no mesmo estilo do simulador do módulo 3 (`!PARAMETERS`, `arrivals`, `queues`, `network`, `rndnumbersPerSeed`/`seeds` ou `rndnumbers`). Filas simples e filas em tandem são casos particulares de rede.

## Como testar (passo a passo)

1. Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download) e confira com `dotnet --version`.
2. Abra um terminal na pasta que contém `QueueSimulator.csproj`.
3. Rode o modelo do T1:

   ```bash
   dotnet run t1.yml
   ```

   A primeira execução baixa a biblioteca de leitura de YAML (YamlDotNet) e compila o projeto; as seguintes são imediatas. O relatório sai no terminal; para salvá-lo:

   ```bash
   dotnet run t1.yml > resultado-t1.txt
   ```

4. Para simular outro modelo, crie um modelo de exemplo comentado e edite-o:

   ```bash
   dotnet run --create-model meu-modelo.yml
   dotnet run meu-modelo.yml
   ```

Outras formas de uso:

| Comando | O que faz |
|---|---|
| `dotnet run t1` | A extensão é opcional: procura `t1.yml`, `t1.yaml` e `t1.json`, nessa ordem |
| `dotnet run t1.yml --debug` | Imprime cada evento (tipo, tempo, origem → destino e estado de todas as filas) antes do relatório |
| `dotnet run` | Abre um menu interativo (opção 1 executa um modelo, opção 2 cria um modelo de exemplo) |

## Formato do modelo (`.yml`)

Exemplo: [`t1.yml`](t1.yml).

```yaml
!PARAMETERS

arrivals:              # primeira chegada externa de cada fila que recebe clientes de fora
   Q1: 2.0

queues:
   Q1:
      servers: 1       # c
                       # capacity omitida = capacidade infinita (G/G/1)
      minArrival: 2.0  # intervalo entre chegadas externas (só nas filas com chegada externa)
      maxArrival: 4.0
      minService: 1.0  # tempo de atendimento
      maxService: 2.0
   Q2:
      servers: 2
      capacity: 5      # K
      minService: 4.0
      maxService: 6.0

network:               # roteamento ao fim do atendimento
-  source: Q1
   target: Q2
   probability: 1.0
-  source: Q2
   target: Q1
   probability: 0.3
-  source: Q2
   target: Exterior    # opcional: o que falta para 1 sai da rede
   probability: 0.7

rndnumbersPerSeed: 100000   # aleatórios usados em cada execução
seeds:                      # uma execução por semente
- 98765
```

Regras:

- **Filas:** `servers`, `minService` e `maxService` são obrigatórios. `capacity` é opcional e, se informada, deve ser ≥ `servers`. `minArrival` e `maxArrival` vêm juntos, e a fila precisa de uma entrada em `arrivals`.
- **Roteamento:** como no simulador do módulo 3, o que faltar para 1 nas rotas de uma fila vai para o exterior. Uma fila sem rotas manda 100% para o exterior. A saída também pode ser escrita explicitamente com `target: Exterior`; nesse caso as rotas da fila precisam somar exatamente 1. Soma acima de 1, destino inexistente ou destino repetido são erros.
- **Sorteio do destino:** um número `u ∈ [0,1)` é comparado com as faixas acumuladas, na ordem do arquivo. Na Fila 2 do T1, por exemplo: `u < 0,3` → Fila 1, `u < 0,8` → Fila 3, senão → exterior. Filas com uma única rota não consomem número aleatório.
- **Parada:** a simulação começa com as filas vazias e termina quando o último número aleatório disponível é usado (`rndnumbersPerSeed`, ou o tamanho da lista `rndnumbers`).
- **Várias sementes:** com mais de uma semente em `seeds`, cada execução é impressa e, no fim, a média das execuções (tempos acumulados, perdas e tempo global).
- **Lista fixa:** no lugar de `seeds`/`rndnumbersPerSeed`, é possível informar `rndnumbers:` com uma lista fixa de números em `[0,1)`.
- **Chaves desconhecidas:** uma chave digitada errado (por exemplo `capacty`) gera erro, em vez de ser ignorada.

Gerador: congruencial linear `x = (1664525·x + 1013904223) mod 2³²`, `u = x / 2³²`.

## Outros arquivos de entrada

O simulador também aceita os formatos JSON das etapas anteriores:

- **Rede em JSON** (arquivo com a chave `Queues`): [`tandem.json`](tandem.json) (o mesmo modelo do T1, com resultado idêntico ao do `t1.yml`) e [`rede.json`](rede.json) (rede de validação com 3 filas). Cada fila informa `Name`, `Servers`, `Capacity`, `MinArrival`/`MaxArrival`, `MinService`/`MaxService` e `Routes` (`To` = nome da fila ou `"Exterior"`, `Probability`; as rotas devem somar 1). `FirstArrivalTime` é a primeira chegada externa.
- **Fila simples (M4)** (arquivo sem a chave `Queues`): [`gg15.json`](gg15.json) e [`gg25.json`](gg25.json), com `Servers`, `MaxCapacity`, `FirstArrivalTime`, `MinArrivalTime`, `MaxArrivalTime`, `MinServiceTime` e `MaxServiceTime`.

## Estrutura do código

| Arquivo | Responsabilidade |
|---|---|
| [`Program.cs`](Program.cs) | Linha de comando, menu interativo e criação do modelo de exemplo |
| [`Modelo.cs`](Modelo.cs) | Leitura do `.yml` (formato do módulo 3) e das execuções (sementes ou lista de aleatórios) |
| [`Rede.cs`](Rede.cs) | Lista de filas, rotas, sorteio por faixas acumuladas, validação e leitura do JSON |
| [`Simulador.cs`](Simulador.cs) | Laço de eventos da rede (chegada, passagem e saída), resultados e média de várias execuções |
| [`Evento.cs`](Evento.cs) | Eventos com origem e destino (`-1` = exterior) |
| [`Escalonador.cs`](Escalonador.cs) | Fila de prioridade dos eventos (empates em ordem de agendamento) |
| [`Fila.cs`](Fila.cs) | Parâmetros G/G/c/K, estado, perdas e tempo acumulado por estado |
| [`RandomGen.cs`](RandomGen.cs) | Gerador congruencial linear (com semente) e lista fixa de aleatórios |
| [`ConsolePrinter.cs`](ConsolePrinter.cs) | Relatórios |
| [`Simulator.cs`](Simulator.cs) | Simulador de fila simples da etapa M4 (formato JSON antigo) |
