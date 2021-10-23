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
| **Preprocess** |  **connectors.1000** |  **6,520.5 μs** | **35.84 μs** | **31.77 μs** |
|  Typecheck |  connectors.1000 |  4,364.3 μs | 50.14 μs | 46.90 μs |
|   Evaluate |  connectors.1000 |    128.9 μs |  1.74 μs |  1.63 μs |
|    Compose |  connectors.1000 |  1,162.9 μs |  9.31 μs |  8.25 μs |
|     Render |  connectors.1000 |    145.6 μs |  1.53 μs |  1.35 μs |
| **Preprocess** |      **tetris.1000** |  **3,576.2 μs** | **26.85 μs** | **25.11 μs** |
|  Typecheck |      tetris.1000 |    908.6 μs |  9.88 μs |  9.24 μs |
|   Evaluate |      tetris.1000 |    225.8 μs |  1.98 μs |  1.85 μs |
|    Compose |      tetris.1000 |    278.5 μs |  5.35 μs |  5.95 μs |
|     Render |      tetris.1000 |    162.4 μs |  2.22 μs |  2.07 μs |
| **Preprocess** | **underground.1000** | **11,022.0 μs** | **75.29 μs** | **70.43 μs** |
|  Typecheck | underground.1000 |  4,384.9 μs | 72.94 μs | 68.23 μs |
|   Evaluate | underground.1000 |    429.3 μs |  6.08 μs |  5.68 μs |
|    Compose | underground.1000 |  2,614.6 μs | 33.75 μs | 29.92 μs |
|     Render | underground.1000 |    503.3 μs |  1.66 μs |  1.55 μs |
