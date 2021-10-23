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
|    **Parse** |  **connectors.1000** | **13,986.6 μs** |  **64.08 μs** |  **50.03 μs** |
| Evaluate |  connectors.1000 |    123.1 μs |   1.76 μs |   1.64 μs |
|  Compose |  connectors.1000 |  1,202.2 μs |  23.94 μs |  24.58 μs |
|   Render |  connectors.1000 | 23,088.6 μs | 173.12 μs | 161.93 μs |
|    **Parse** |      **tetris.1000** |  **6,865.4 μs** |  **29.76 μs** |  **26.38 μs** |
| Evaluate |      tetris.1000 |    203.8 μs |   1.42 μs |   1.33 μs |
|  Compose |      tetris.1000 |    266.2 μs |   2.13 μs |   1.99 μs |
|   Render |      tetris.1000 | 17,735.2 μs | 190.83 μs | 169.17 μs |
|    **Parse** | **underground.1000** | **20,362.3 μs** |  **84.60 μs** |  **70.65 μs** |
| Evaluate | underground.1000 |    424.9 μs |   6.41 μs |   6.00 μs |
|  Compose | underground.1000 |  2,578.8 μs |  28.03 μs |  26.22 μs |
|   Render | underground.1000 | 50,820.7 μs | 296.28 μs | 247.40 μs |
