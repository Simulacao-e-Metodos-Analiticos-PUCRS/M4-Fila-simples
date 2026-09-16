# Queue Simulator

Simulador de eventos discretos para **filas simples** e **redes de filas**, desenvolvido para a
disciplina de Simulação e Métodos Analíticos (2026/2) — Escola Politécnica, PUCRS.
Prof. Afonso Sales.

**Equipe:** Augusto Sanhudo da Silva Knob · Carlos Eduardo Brito Mascarello ·
Matheus Hrymalak Souza · Olivia Maite Furquim Araujo Livak

O simulador cobre as duas etapas do trabalho:

- **M4 — fila simples:** uma fila `G/G/c/K` com chegadas externas;
- **M5 — rede de filas:** várias filas ligadas por probabilidades de roteamento, com o caso
  em tandem `Fila 1 → Fila 2` como validação.

O formato do arquivo de configuração determina qual dos dois é executado.

---

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Nenhuma dependência externa.

---

## Como executar

Todos os comandos são rodados a partir da raiz do projeto (a pasta que contém
`QueueSimulator.csproj`).

### Simulações de validação do trabalho

```bash
# M5 — rede em tandem: Fila 1 (G/G/2/3) -> Fila 2 (G/G/1/5)
dotnet run tandem

# M4 — fila simples G/G/1/5
dotnet run gg15

# M4 — fila simples G/G/2/5
dotnet run gg25
```

### Outras formas de uso

```bash
# qualquer arquivo de configuração (a extensão .json é opcional)
dotnet run meu-modelo

# modo debug: imprime cada evento processado
# (disponível apenas para fila simples; a rede ignora a opção)
dotnet run gg15 --debug

# menu interativo
# (roda apenas modelos de FILA SIMPLES; para a rede use a linha de comando)
dotnet run

# gera um arquivo de modelo comentado no diretório atual
dotnet run -- --create-model meu-modelo.json
```

### Salvar o resultado em arquivo

A saída não pede interação no final, então basta redirecionar:

```bash
dotnet run tandem > resultado-tandem.txt
```

---

## Formato dos arquivos de configuração

### Rede de filas (M5)

Um arquivo que contenha a chave `"Queues"` é interpretado como rede.
Este é o [`tandem.json`](tandem.json), a rede exigida pelo enunciado:

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

| Campo | Descrição |
|---|---|
| `FirstArrivalTime` | Instante da primeira chegada externa |
| `Name` | Nome da fila; é por ele que as rotas se referem a ela |
| `Servers` | Número de servidores (atendimentos simultâneos) |
| `Capacity` | Capacidade total da fila. Use `null` para capacidade infinita |
| `MinArrival` / `MaxArrival` | Intervalo entre chegadas **externas**. **Omita os dois** para uma fila que só recebe clientes de outras filas |
| `MinService` / `MaxService` | Intervalo de tempo de atendimento |
| `Routes` | Para onde o cliente vai após ser atendido |

**Como declarar a topologia.** Três convenções descrevem qualquer rede:

| Situação | Como escrever |
|---|---|
| Fila sem chegadas externas | omitir `MinArrival` e `MaxArrival` |
| Cliente sai da rede após o atendimento | `"Routes": []` |
| Cliente segue para outra fila | `"Routes": [ { "To": "<nome>", "Probability": 0.7 } ]` |

As probabilidades de um mesmo `Routes` podem somar **menos que 1** — a diferença é a
probabilidade de o cliente deixar a rede. Um exemplo com topologia genérica:

```json
"Routes": [
    { "To": "Fila 2", "Probability": 0.3 },
    { "To": "Fila 3", "Probability": 0.5 }
]
```

30% dos clientes vão para a Fila 2, 50% para a Fila 3 e os 20% restantes saem da rede.
Quando há um único destino com probabilidade 1,0, o roteamento **não consome número
aleatório**.

O carregamento rejeita, com mensagem indicando a fila: destino inexistente, probabilidades
somando mais que 1, nomes duplicados e rede sem nenhuma fila com chegada externa.

### Fila simples (M4)

Um arquivo **sem** a chave `"Queues"` é interpretado como fila única.
Este é o [`gg15.json`](gg15.json):

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
| `MaxCapacity` | Capacidade máxima da fila (`null` = infinita) |
| `NumberOfEvents` | Não utilizado — a simulação encerra ao consumir 100.000 aleatórios |
| `FirstArrivalTime` | Tempo da primeira chegada |
| `MinArrivalTime` / `MaxArrivalTime` | Intervalo de tempo entre chegadas |
| `MinServiceTime` / `MaxServiceTime` | Intervalo de tempo de atendimento |

---

## Resultados das simulações de validação

Todas as execuções usam **100.000 números pseudoaleatórios** e a semente `98765`.
São reproduzíveis: o gerador é determinístico e rodar de novo dá exatamente os mesmos números.

### M5 — rede em tandem (`dotnet run tandem`)

Fila 1 — `G/G/2/3`, chegadas 1..5, atendimento 4..5.
Fila 2 — `G/G/1/5`, sem chegadas externas, atendimento 1..3, recebe 100% da Fila 1.
Primeira chegada em t = 2,5.

**Tempo global: 100.794,802663 minutos · 99.998 eventos processados**

| Estado | Fila 1 — tempo (min) | Fila 1 — prob. | Fila 2 — tempo (min) | Fila 2 — prob. |
|---:|---:|---:|---:|---:|
| 0 | 1.144,114309 | 1,135093% | 34.445,695210 | 34,174079% |
| 1 | 49.846,391542 | 49,453335% | 60.144,268882 | 59,670010% |
| 2 | 43.495,374545 | 43,152398% | 6.198,312414 | 6,149437% |
| 3 | 6.308,922267 | 6,259174% | 6,526157 | 0,006475% |
| 4 | — | — | 0,000000 | 0,000000% |
| 5 | — | — | 0,000000 | 0,000000% |
| **Total** | **100.794,802663** | **100%** | **100.794,802663** | **100%** |

**Clientes perdidos:** 381 na Fila 1 · 0 na Fila 2.

### M4 — fila simples

`dotnet run gg15` — **G/G/1/5**, chegadas 3..5, atendimento 4..5, primeira chegada em t = 3,0.
Tempo global **211.788,31 min**, **5.857 perdas**.

| Estado | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---:|---:|---:|---:|---:|---:|
| Tempo (min) | 3,00 | 23,61 | 60,34 | 721,03 | 101.904,08 | 109.076,26 |
| Probabilidade | 0,00% | 0,01% | 0,03% | 0,34% | 48,12% | 51,50% |

`dotnet run gg25` — **G/G/2/5**, chegadas 3..5, atendimento 4..5, primeira chegada em t = 3,0.
Tempo global **199.997,16 min**, **0 perdas**.

| Estado | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---:|---:|---:|---:|---:|---:|
| Tempo (min) | 4.159,75 | 166.666,88 | 29.170,53 | 0,00 | 0,00 | 0,00 |
| Probabilidade | 2,08% | 83,33% | 14,59% | 0,00% | 0,00% | 0,00% |

Os estados 3, 4 e 5 do `G/G/2/5` são **impossíveis**, não apenas raros: o terceiro cliente chega
no mínimo 6 minutos depois do primeiro (dois intervalos mínimos de 3 min) e o atendimento dura
no máximo 5 minutos, então o primeiro já saiu. Por isso essa fila também não perde clientes.

---

## Decisões de modelagem

**Gerador de números pseudoaleatórios.** Método congruente linear,
`X(n+1) = (a · X(n) + c) mod M`, com `a = 1664525`, `c = 1013904223`, `M = 2³²` e semente
`98765`. Os valores normalizados são `U = X / M`, no intervalo [0, 1). As condições de
Hull–Dobell são satisfeitas, portanto o período é completo (2³² ≈ 4,29 bilhões), muito acima
dos 100.000 usados.

**Critério de parada.** A simulação encerra assim que o **100.000º** número aleatório é
utilizado. O evento que o consumiu é processado por inteiro e contabilizado; nenhum evento
posterior é tratado.

**Consumo de aleatórios.** Uma chegada consome 1 número (intervalo até a próxima chegada) mais
1 se o cliente iniciar atendimento na hora. Um fim de atendimento consome 1 se havia alguém
esperando, mais 1 se o cliente for encaminhado para uma fila com servidor livre. Roteamento
com destino único não consome nenhum.

**Modelo de perda.** Cliente que chega a uma fila cheia é **descartado** e a perda é
contabilizada **na fila que o recusou**. Não há bloqueio: o servidor da fila de origem é
liberado normalmente, mesmo que o destino esteja lotado.

**Tempos acumulados.** Antes de cada evento, o intervalo desde o evento anterior é creditado ao
estado corrente de **todas** as filas. Ao final, o programa verifica automaticamente que a soma
dos tempos de cada fila é igual ao tempo global e que a soma das probabilidades é 1, abortando
com erro caso alguma dessas invariantes falhe.

---

## Estrutura do código

| Arquivo | Responsabilidade |
|---|---|
| [`RandomGen.cs`](RandomGen.cs) | Gerador congruente linear |
| [`Fila.cs`](Fila.cs) | Uma fila: configuração, estado, contadores e tempos por estado |
| [`Evento.cs`](Evento.cs) | Evento (`Chegada`, `Saida`, `Passagem`), ordenável por tempo |
| [`Escalonador.cs`](Escalonador.cs) | Fila de prioridade mínima dos eventos |
| [`Rede.cs`](Rede.cs) | Topologia, roteamento e leitura do JSON da rede |
| [`Simulador.cs`](Simulador.cs) | Laço principal, tratamento dos eventos e estatísticas da rede |
| [`Simulator.cs`](Simulator.cs) | Simulador de fila única (M4) |
| [`ConsolePrinter.cs`](ConsolePrinter.cs) | Relatórios |
| [`Program.cs`](Program.cs) | Linha de comando e menu interativo |
