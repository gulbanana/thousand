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
|    **Parse** |  **connectors.1000** | **12,625.8 μs** |  **97.79 μs** |  **91.47 μs** |
| Evaluate |  connectors.1000 |    125.2 μs |   2.02 μs |   1.89 μs |
|  Compose |  connectors.1000 |  1,168.9 μs |  10.51 μs |   9.32 μs |
|   Render |  connectors.1000 | 23,251.2 μs | 188.22 μs | 166.86 μs |
|    **Parse** |      **tetris.1000** |  **6,207.1 μs** |  **37.05 μs** |  **34.66 μs** |
| Evaluate |      tetris.1000 |    211.7 μs |   1.34 μs |   1.25 μs |
|  Compose |      tetris.1000 |    273.6 μs |   5.16 μs |   5.52 μs |
|   Render |      tetris.1000 | 17,837.7 μs | 257.24 μs | 240.63 μs |
|    **Parse** | **underground.1000** | **17,840.8 μs** |  **50.93 μs** |  **45.14 μs** |
| Evaluate | underground.1000 |    410.9 μs |   1.21 μs |   1.01 μs |
|  Compose | underground.1000 |  2,527.8 μs |   8.52 μs |   7.97 μs |
|   Render | underground.1000 | 50,689.5 μs | 399.36 μs | 373.56 μs |
