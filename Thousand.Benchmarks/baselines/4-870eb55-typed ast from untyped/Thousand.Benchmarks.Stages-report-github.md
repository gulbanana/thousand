``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |        Mean |     Error |    StdDev |
|----------- |----------------- |------------:|----------:|----------:|
| **Preprocess** |  **connectors.1000** |  **9,314.7 μs** |  **96.82 μs** |  **90.56 μs** |
|  Typecheck |  connectors.1000 |  2,367.3 μs |  18.94 μs |  16.79 μs |
|   Evaluate |  connectors.1000 |    128.2 μs |   1.19 μs |   1.11 μs |
|    Compose |  connectors.1000 |  1,192.4 μs |  18.65 μs |  17.44 μs |
|     Render |  connectors.1000 |    147.8 μs |   1.35 μs |   1.27 μs |
| **Preprocess** |      **tetris.1000** |  **5,140.8 μs** |  **38.70 μs** |  **36.20 μs** |
|  Typecheck |      tetris.1000 |    641.0 μs |   4.58 μs |   4.29 μs |
|   Evaluate |      tetris.1000 |    224.2 μs |   1.44 μs |   1.35 μs |
|    Compose |      tetris.1000 |    274.1 μs |   2.69 μs |   2.25 μs |
|     Render |      tetris.1000 |    167.9 μs |   1.55 μs |   1.45 μs |
| **Preprocess** | **underground.1000** | **15,791.9 μs** | **200.61 μs** | **187.65 μs** |
|  Typecheck | underground.1000 |  2,125.5 μs |   5.50 μs |   4.59 μs |
|   Evaluate | underground.1000 |    431.8 μs |   1.70 μs |   1.50 μs |
|    Compose | underground.1000 |  2,634.6 μs |  36.16 μs |  33.82 μs |
|     Render | underground.1000 |    511.6 μs |   1.83 μs |   1.71 μs |
