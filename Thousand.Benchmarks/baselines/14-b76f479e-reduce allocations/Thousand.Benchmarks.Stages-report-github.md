``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |       Mean |    Error |   StdDev |
|----------- |----------------- |-----------:|---------:|---------:|
|   **Tokenize** |  **connectors.1000** |   **376.6 μs** |  **1.87 μs** |  **1.75 μs** |
| Preprocess |  connectors.1000 | 4,743.1 μs | 14.35 μs | 13.43 μs |
|  Typecheck |  connectors.1000 |   513.8 μs |  0.64 μs |  0.57 μs |
|   Evaluate |  connectors.1000 |   105.0 μs |  0.19 μs |  0.18 μs |
|    Compose |  connectors.1000 | 1,194.8 μs |  6.62 μs |  6.20 μs |
|     Render |  connectors.1000 |   149.9 μs |  0.70 μs |  0.62 μs |
|   **Tokenize** |      **tetris.1000** |   **294.9 μs** |  **0.18 μs** |  **0.16 μs** |
| Preprocess |      tetris.1000 | 1,715.6 μs |  8.03 μs |  7.51 μs |
|  Typecheck |      tetris.1000 |   141.9 μs |  0.91 μs |  0.85 μs |
|   Evaluate |      tetris.1000 |   157.6 μs |  0.64 μs |  0.60 μs |
|    Compose |      tetris.1000 |   318.7 μs |  1.44 μs |  1.34 μs |
|     Render |      tetris.1000 |   166.8 μs |  0.66 μs |  0.62 μs |
|   **Tokenize** | **underground.1000** |   **810.3 μs** |  **3.30 μs** |  **3.09 μs** |
| Preprocess | underground.1000 | 7,557.5 μs |  5.59 μs |  5.23 μs |
|  Typecheck | underground.1000 |   770.1 μs |  0.60 μs |  0.53 μs |
|   Evaluate | underground.1000 |   354.0 μs |  4.89 μs |  4.57 μs |
|    Compose | underground.1000 | 2,680.0 μs | 52.77 μs | 58.65 μs |
|     Render | underground.1000 |   523.4 μs |  1.63 μs |  1.45 μs |
