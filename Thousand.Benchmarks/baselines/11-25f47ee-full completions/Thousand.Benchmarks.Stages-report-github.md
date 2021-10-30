``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |        Mean |    Error |   StdDev |
|----------- |----------------- |------------:|---------:|---------:|
|   **Tokenize** |  **connectors.1000** |  **1,372.2 μs** |  **6.08 μs** |  **5.69 μs** |
| Preprocess |  connectors.1000 |  6,689.7 μs | 32.48 μs | 30.38 μs |
|  Typecheck |  connectors.1000 |    532.5 μs |  6.56 μs |  6.14 μs |
|   Evaluate |  connectors.1000 |    132.6 μs |  2.53 μs |  2.81 μs |
|    Compose |  connectors.1000 |  1,295.9 μs | 12.97 μs | 12.13 μs |
|     Render |  connectors.1000 |    155.4 μs |  1.86 μs |  1.74 μs |
|   **Tokenize** |      **tetris.1000** |  **1,041.6 μs** | **11.05 μs** | **10.33 μs** |
| Preprocess |      tetris.1000 |  2,229.2 μs | 25.06 μs | 23.44 μs |
|  Typecheck |      tetris.1000 |    144.6 μs |  1.76 μs |  1.64 μs |
|   Evaluate |      tetris.1000 |    198.2 μs |  0.57 μs |  0.53 μs |
|    Compose |      tetris.1000 |    318.4 μs |  1.10 μs |  0.97 μs |
|     Render |      tetris.1000 |    169.5 μs |  0.39 μs |  0.36 μs |
|   **Tokenize** | **underground.1000** |  **3,063.8 μs** | **14.63 μs** | **12.97 μs** |
| Preprocess | underground.1000 | 10,044.1 μs | 22.61 μs | 21.15 μs |
|  Typecheck | underground.1000 |    772.9 μs |  0.72 μs |  0.64 μs |
|   Evaluate | underground.1000 |    389.6 μs |  0.78 μs |  0.65 μs |
|    Compose | underground.1000 |  2,699.5 μs | 52.16 μs | 48.79 μs |
|     Render | underground.1000 |    529.3 μs |  1.73 μs |  1.62 μs |
