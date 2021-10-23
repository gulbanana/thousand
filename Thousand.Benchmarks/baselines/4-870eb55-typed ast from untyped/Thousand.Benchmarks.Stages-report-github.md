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
|    **Parse** |  **connectors.1000** | **11,760.3 μs** |  **33.54 μs** |  **28.01 μs** |
| Evaluate |  connectors.1000 |    129.0 μs |   0.71 μs |   0.66 μs |
|  Compose |  connectors.1000 |  1,177.7 μs |   6.48 μs |   5.74 μs |
|   Render |  connectors.1000 | 23,009.9 μs | 180.23 μs | 168.59 μs |
|    **Parse** |      **tetris.1000** |  **5,886.7 μs** |  **19.92 μs** |  **18.64 μs** |
| Evaluate |      tetris.1000 |    228.8 μs |   0.83 μs |   0.77 μs |
|  Compose |      tetris.1000 |    269.1 μs |   1.40 μs |   1.31 μs |
|   Render |      tetris.1000 | 17,635.0 μs |  52.77 μs |  49.36 μs |
|    **Parse** | **underground.1000** | **18,226.3 μs** | **241.16 μs** | **225.58 μs** |
| Evaluate | underground.1000 |    439.1 μs |   3.17 μs |   2.81 μs |
|  Compose | underground.1000 |  2,618.0 μs |  26.39 μs |  24.69 μs |
|   Render | underground.1000 | 51,378.0 μs | 569.19 μs | 504.58 μs |
