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
| **Preprocess** |  **connectors.1000** |  **6,241.2 μs** | **34.80 μs** | **30.85 μs** |
|  Typecheck |  connectors.1000 |    441.8 μs |  4.69 μs |  4.38 μs |
|   Evaluate |  connectors.1000 |    126.8 μs |  1.39 μs |  1.16 μs |
|    Compose |  connectors.1000 |  1,175.4 μs | 21.35 μs | 19.97 μs |
|     Render |  connectors.1000 |    143.5 μs |  2.47 μs |  2.31 μs |
| **Preprocess** |      **tetris.1000** |  **3,547.0 μs** | **17.27 μs** | **16.16 μs** |
|  Typecheck |      tetris.1000 |    112.6 μs |  0.16 μs |  0.13 μs |
|   Evaluate |      tetris.1000 |    226.1 μs |  0.81 μs |  0.76 μs |
|    Compose |      tetris.1000 |    268.3 μs |  0.59 μs |  0.55 μs |
|     Render |      tetris.1000 |    163.1 μs |  0.29 μs |  0.26 μs |
| **Preprocess** | **underground.1000** | **10,722.4 μs** | **77.37 μs** | **68.58 μs** |
|  Typecheck | underground.1000 |    669.1 μs |  7.50 μs |  7.01 μs |
|   Evaluate | underground.1000 |    449.9 μs |  4.35 μs |  4.07 μs |
|    Compose | underground.1000 |  2,648.7 μs | 50.44 μs | 53.97 μs |
|     Render | underground.1000 |    519.4 μs |  3.55 μs |  3.32 μs |
