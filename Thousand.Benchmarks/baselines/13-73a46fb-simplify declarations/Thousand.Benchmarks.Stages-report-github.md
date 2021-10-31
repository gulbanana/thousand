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
|   **Tokenize** |  **connectors.1000** |   **381.6 μs** |  **3.81 μs** |  **3.18 μs** |
| Preprocess |  connectors.1000 | 5,686.5 μs | 75.51 μs | 70.63 μs |
|  Typecheck |  connectors.1000 |   522.8 μs |  3.21 μs |  2.84 μs |
|   Evaluate |  connectors.1000 |   104.7 μs |  0.67 μs |  0.59 μs |
|    Compose |  connectors.1000 | 1,205.0 μs |  4.04 μs |  3.78 μs |
|     Render |  connectors.1000 |   152.3 μs |  0.47 μs |  0.44 μs |
|   **Tokenize** |      **tetris.1000** |   **298.0 μs** |  **0.65 μs** |  **0.51 μs** |
| Preprocess |      tetris.1000 | 2,122.2 μs |  8.78 μs |  7.78 μs |
|  Typecheck |      tetris.1000 |   134.8 μs |  0.59 μs |  0.52 μs |
|   Evaluate |      tetris.1000 |   163.3 μs |  0.73 μs |  0.61 μs |
|    Compose |      tetris.1000 |   324.2 μs |  2.63 μs |  2.46 μs |
|     Render |      tetris.1000 |   168.9 μs |  0.83 μs |  0.78 μs |
|   **Tokenize** | **underground.1000** |   **815.4 μs** |  **3.96 μs** |  **3.51 μs** |
| Preprocess | underground.1000 | 8,462.1 μs | 23.57 μs | 20.90 μs |
|  Typecheck | underground.1000 |   776.5 μs |  0.98 μs |  0.87 μs |
|   Evaluate | underground.1000 |   350.4 μs |  1.21 μs |  1.07 μs |
|    Compose | underground.1000 | 2,744.6 μs | 53.79 μs | 69.94 μs |
|     Render | underground.1000 |   535.8 μs |  1.89 μs |  1.77 μs |
