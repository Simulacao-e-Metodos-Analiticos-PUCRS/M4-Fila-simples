# DESENVOLVENDO UM SIMULADOR PARA UMA FILA

**Prof. Afonso Sales, Ph.D.**

A fim de melhor estruturar seu processo de desenvolvimento de um simulador, pense em uma estrutura simples de código e nem se preocupe inicialmente em organizar o código usando uma estrutura de classes, por exemplo. Nesse caso, utilize variáveis globais diretamente na *main()* e funções simples de desenvolvimento que venham a auxiliar seu processo de desenvolvimento.

Escolha a linguagem de programação que julgar mais apropriada e confortável para o desenvolvimento de seu simulador (usualmente, o simulador é feito em linguagem *Java* ou *Python*, mas sinta-se à vontade em escolher qualquer outra). A ideia é construir progressivamente seu simulador, começando com um código básico e ir aprimorando-o ao longo do tempo.

Para fins de simplificação do processo de desenvolvimento, você pode imaginar o desenvolvimento do seu simulador seguindo algumas etapas.

---

## Etapa 1 — Implementação do gerador de números pseudoaleatórios

O processo de desenvolvimento do simulador começa com a implementação de um gerador de números pseudoaleatórios, utilizando o **Método Congruente Linear**. Esse assunto já foi amplamente abordado no módulo 2, intitulado de **Geração de Números Pseudoaleatórios**. Lembrando que esse gerador é a base para simular a chegada e o atendimento de clientes em uma fila, permitindo a análise de comportamentos e tendências ao longo do tempo. Sendo assim, é extremamente importante que o gerador esteja bem definido (no caso, seus parâmetros) e bem implementado, garantindo a qualidade do processo de simulação.

Dito isso, comece desenvolvendo e testando seu gerador de números pseudoaleatórios. Escolha valores apropriados para os parâmetros **a**, **c**, e **M**, bem como o valor inicial para a semente (*seed*) da sequência de números. Como foi feito anteriormente no módulo 2, você pode testar a qualidade dos números gerados através de um gráfico de dispersão em uma planilha eletrônica (no *Excel*, por exemplo).

Para facilitar a implementação e o uso da função, desenvolva uma função chamada *NextRandom()* que gera números pseudoaleatórios normalizados entre 0 e 1, armazenando sempre o último número gerado da sequência para facilitar na próxima chamada da função.

Logo, você pode imaginar a função *NextRandom()* como algo do tipo:

```
double NextRandom() {
    previous = ((a * previous) + c) % M;
    return (double) previous/M;
}
```

---

## Etapa 2 — *Loop* principal da simulação

Uma vez implementado e testado seu gerador, inicie o desenvolvimento do simulador de uma fila simples em si. Para isso, pense inicialmente na estrutura do seu código e como será feita a execução do seu simulador.

Diretamente na *main()* você poderá implementar um laço de repetição de modo a executar a simulação por um determinado período. No caso, podemos escolher como critério de parada o uso de uma determinada quantidade de números pseudoaleatórios. Logo, ao inicializar nosso contador (variável *count* do pseudocódigo a seguir) com um determinado valor (por exemplo, 100.000), toda vez que "solicitarmos" um novo número pseudoaleatório, decrementamos este contador de modo que ao "não termos mais" números a serem utilizados, paramos o laço de repetição. A seguir uma sugestão de pseudocódigo do que pode ser este laço de repetição na *main()*.

```
int count = 100000;
...
while (count > 0) {
    evento = NextEvent();

    if (evento == tipo_chegada) {
        CHEGADA(evento)
    } else if (evento == tipo_saida) {
        SAIDA(evento)
    }
}
```

O processo dentro do laço de repetição é muito simples, visto que basicamente solicita-se o primeiro evento do escalonador (ou seja, o primeiro que estiver agendado no escalonador com menor tempo de simulação a ser executado) e verifica-se se ele é do tipo "chegada" ou "saída". Dessa forma, ao identificar corretamente o tipo do evento, você pode chamar apropriadamente o procedimento que corresponde a simulação deste evento.

---

## Etapa 3 — Tratamento dos eventos de chegada e saída

Conforme apresentado anteriormente no módulo **Modelagem Orientada a Eventos (Algoritmo de Simulação)**, os procedimentos de CHEGADA e SAIDA são fundamentais para o processo de simulação de uma fila, visto que eles farão todo o tratamento que simula a chegada e saída de clientes da fila. Dessa forma, esses procedimentos irão gerenciar a adição e remoção de clientes da fila utilizando o intervalo de tempo estabelecido para a chegada e atendimento de clientes na fila, através do uso dos números pseudoaleatórios calculados pelo seu gerador a fim de dar a sensação de que a dinâmica de chegada e saída de clientes da fila não é estática.

---

## Etapa 4 — Cálculo de tempos acumulados e probabilidades

Após o *loop* principal da simulação (ou seja, após sair do laço de repetição), você pode calcular e mostrar a distribuição de probabilidade dos estados da fila. Sendo assim, é possível calcular a probabilidade de se encontrar em um determinado estado durante o período de simulação pelos tempos acumulados em cada um dos estados da fila (por exemplo: vazia, com 1 cliente, 2 clientes etc.).

Para obter esta distribuição de probabilidade, basta que você divida cada um dos tempos acumulados dos estados pelo tempo global (total) de simulação. Além disso, é interessante também mostrar o tempo "puro" acumulado para cada um dos estados. Esses dados são cruciais para entender a dinâmica da fila e identificar potenciais gargalos. A seguir está desenhado um possível pseudocódigo de como ficaria esta impressão dos dados, onde a variável **K** representa a capacidade de clientes da fila.

```
for (int i=0; i<K+1; i++) {
    print i + ": " + times[i] + " (" +
          times[i]/TempoGlobal + "\%)\n";
}
```

---

## Etapa 5 — Análise e interpretação dos resultados

Por fim, após a obtenção da distribuição de probabilidades dos estados da fila, é possível compreender como a fila comporta-se sob diferentes condições, visto que sempre é possível alterar a configuração da fila que está sendo simulada para uma nova análise (perspectiva). Essa análise ajudará a entender melhor a eficiência do sistema de filas simulado e possíveis melhorias.

Não obstante, pela obtenção destas probabilidades, também é possível calcular índices de desempenho (tais como, vazão, utilização, população e tempo de reposta) desta fila. Esses índices serão estudados em detalhe no módulo **Cálculo dos Índices de Desempenho**.

---

## Parte 1 | Entrega | Fila simples [*FEEDBACK*]

**M4 | Atividade prática**

Esse módulo é dedicado para que cada equipe possa aprimorar e terminar o **simulador de uma fila simples.** Essa é a primeira etapa do trabalho. Em caso de dúvida, volte e consulte novamente o texto multimodal do módulo, onde foi descrito em detalhes a especificação desta etapa.

Para fins de validação, **além do seu código-fonte**, você **também deve entregar o resultado da simulação** das seguintes filas:

- **G/G/1/5**, chegadas entre **3...5**, atendimento entre **4...5**;
- **G/G/2/5**, chegadas entre **3...5**, atendimento entre **4...5**.

Para ambas simulações, considere inicialmente a fila vazia e o **primeiro cliente chegando no tempo 3,0**. Realize a **simulação com 100.000 aleatórios**, ou seja, ao se utilizar o 100.000° aleatório, sua simulação deve se encerrar e a **distribuição de probabilidades**, bem como os **tempos acumulados** para os estados da fila devem ser reportados. Além disso, indique o **número de perda de clientes** (caso tenha havido perda) e o **tempo global da simulação**.
