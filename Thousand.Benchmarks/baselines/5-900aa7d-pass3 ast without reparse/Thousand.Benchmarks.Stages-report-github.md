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
|    **Parse** |  **connectors.1000** | **10,227.0 μs** |  **63.52 μs** |  **59.42 μs** |
| Evaluate |  connectors.1000 |    126.9 μs |   1.71 μs |   1.51 μs |
|  Compose |  connectors.1000 |  1,193.0 μs |   7.33 μs |   6.86 μs |
|   Render |  connectors.1000 | 23,054.8 μs | 132.39 μs | 123.84 μs |
|    **Parse** |      **tetris.1000** |  **5,038.9 μs** |  **17.70 μs** |  **16.56 μs** |
| Evaluate |      tetris.1000 |    226.1 μs |   1.18 μs |   1.10 μs |
|  Compose |      tetris.1000 |    264.7 μs |   2.18 μs |   1.82 μs |
|   Render |      tetris.1000 | 17,741.1 μs | 105.97 μs |  99.12 μs |
|    **Parse** | **underground.1000** | **15,513.5 μs** |  **46.09 μs** |  **40.86 μs** |
| Evaluate | underground.1000 |    432.2 μs |   4.18 μs |   3.91 μs |
|  Compose | underground.1000 |  2,595.2 μs |  22.85 μs |  21.38 μs |
|   Render | underground.1000 | 50,912.1 μs | 260.97 μs | 231.34 μs |
