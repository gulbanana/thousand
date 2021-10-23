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
|    **Parse** |  **connectors.1000** | **12,552.2 μs** |  **35.22 μs** |  **27.49 μs** |
| Evaluate |  connectors.1000 |    125.1 μs |   1.36 μs |   1.27 μs |
|  Compose |  connectors.1000 |  1,170.3 μs |  11.82 μs |  10.48 μs |
|   Render |  connectors.1000 | 23,060.1 μs | 113.09 μs | 100.25 μs |
|    **Parse** |      **tetris.1000** |  **6,128.2 μs** |  **15.87 μs** |  **14.85 μs** |
| Evaluate |      tetris.1000 |    208.5 μs |   0.69 μs |   0.65 μs |
|  Compose |      tetris.1000 |    257.3 μs |   2.58 μs |   2.41 μs |
|   Render |      tetris.1000 | 17,792.4 μs | 155.96 μs | 138.25 μs |
|    **Parse** | **underground.1000** | **18,320.1 μs** |  **77.70 μs** |  **72.68 μs** |
| Evaluate | underground.1000 |    414.7 μs |   3.23 μs |   3.02 μs |
|  Compose | underground.1000 |  2,553.7 μs |  17.86 μs |  16.71 μs |
|   Render | underground.1000 | 51,677.2 μs | 836.50 μs | 741.53 μs |
