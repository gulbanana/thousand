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
|    **Parse** |  **connectors.1000** | **12,551.2 μs** | **105.02 μs** |  **93.10 μs** |
| Evaluate |  connectors.1000 |    121.5 μs |   1.29 μs |   1.20 μs |
|  Compose |  connectors.1000 |  1,184.0 μs |  11.33 μs |  10.59 μs |
|   Render |  connectors.1000 |    145.8 μs |   1.54 μs |   1.44 μs |
|    **Parse** |      **tetris.1000** |  **6,154.6 μs** |  **39.23 μs** |  **36.70 μs** |
| Evaluate |      tetris.1000 |    211.7 μs |   1.80 μs |   1.69 μs |
|  Compose |      tetris.1000 |    278.1 μs |   1.74 μs |   1.63 μs |
|   Render |      tetris.1000 |    166.3 μs |   2.38 μs |   2.23 μs |
|    **Parse** | **underground.1000** | **18,351.5 μs** | **172.08 μs** | **152.54 μs** |
| Evaluate | underground.1000 |    422.7 μs |   3.33 μs |   3.12 μs |
|  Compose | underground.1000 |  2,521.9 μs |  14.04 μs |  12.45 μs |
|   Render | underground.1000 |    519.2 μs |   5.74 μs |   5.37 μs |
