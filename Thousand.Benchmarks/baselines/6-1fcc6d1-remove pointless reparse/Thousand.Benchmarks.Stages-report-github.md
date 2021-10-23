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
|    **Parse** |  **connectors.1000** |  **8,968.2 μs** |  **76.36 μs** |  **71.43 μs** |
| Evaluate |  connectors.1000 |    127.7 μs |   2.12 μs |   1.98 μs |
|  Compose |  connectors.1000 |  1,194.8 μs |  16.97 μs |  14.17 μs |
|   Render |  connectors.1000 | 23,225.0 μs | 210.75 μs | 186.82 μs |
|    **Parse** |      **tetris.1000** |  **4,257.9 μs** |  **51.60 μs** |  **48.27 μs** |
| Evaluate |      tetris.1000 |    227.3 μs |   0.94 μs |   0.83 μs |
|  Compose |      tetris.1000 |    265.0 μs |   4.00 μs |   3.74 μs |
|   Render |      tetris.1000 | 17,927.3 μs | 197.44 μs | 175.03 μs |
|    **Parse** | **underground.1000** | **13,200.3 μs** | **155.39 μs** | **145.35 μs** |
| Evaluate | underground.1000 |    429.9 μs |   2.37 μs |   2.10 μs |
|  Compose | underground.1000 |  2,538.4 μs |  16.79 μs |  14.02 μs |
|   Render | underground.1000 | 50,591.2 μs | 140.28 μs | 109.52 μs |
