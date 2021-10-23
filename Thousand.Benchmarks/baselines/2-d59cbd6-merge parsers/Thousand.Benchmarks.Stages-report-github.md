``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|   Method |            Input |        Mean |     Error |    StdDev |
|--------- |----------------- |------------:|----------:|----------:|
|    **Parse** |  **connectors.1000** | **12,507.5 μs** | **115.75 μs** | **108.27 μs** |
| Evaluate |  connectors.1000 |    123.0 μs |   0.82 μs |   0.77 μs |
|  Compose |  connectors.1000 |  1,205.4 μs |  13.95 μs |  11.65 μs |
|   Render |  connectors.1000 |    149.1 μs |   1.24 μs |   1.10 μs |
|    **Parse** |      **tetris.1000** |  **6,305.7 μs** |  **66.11 μs** |  **58.61 μs** |
| Evaluate |      tetris.1000 |    211.0 μs |   2.16 μs |   2.02 μs |
|  Compose |      tetris.1000 |    286.5 μs |   5.24 μs |   6.03 μs |
|   Render |      tetris.1000 |    170.0 μs |   1.08 μs |   1.01 μs |
|    **Parse** | **underground.1000** | **18,887.8 μs** |  **96.89 μs** |  **90.63 μs** |
| Evaluate | underground.1000 |    419.2 μs |   4.45 μs |   4.16 μs |
|  Compose | underground.1000 |  2,592.1 μs |  31.15 μs |  29.13 μs |
|   Render | underground.1000 |    522.7 μs |   9.94 μs |   9.30 μs |
