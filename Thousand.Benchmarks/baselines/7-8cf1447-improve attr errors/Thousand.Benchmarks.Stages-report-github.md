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
|    **Parse** |  **connectors.1000** | **11,340.1 μs** | **153.06 μs** | **143.17 μs** |
| Evaluate |  connectors.1000 |    128.6 μs |   1.75 μs |   1.55 μs |
|  Compose |  connectors.1000 |  1,162.6 μs |  10.17 μs |   9.01 μs |
|   Render |  connectors.1000 | 23,100.0 μs |  84.99 μs |  79.50 μs |
|    **Parse** |      **tetris.1000** |  **4,540.0 μs** |  **56.02 μs** |  **52.40 μs** |
| Evaluate |      tetris.1000 |    229.3 μs |   1.53 μs |   1.27 μs |
|  Compose |      tetris.1000 |    262.3 μs |   1.30 μs |   1.22 μs |
|   Render |      tetris.1000 | 17,901.9 μs | 261.20 μs | 244.33 μs |
|    **Parse** | **underground.1000** | **15,961.0 μs** | **153.40 μs** | **143.49 μs** |
| Evaluate | underground.1000 |    424.3 μs |   2.16 μs |   2.02 μs |
|  Compose | underground.1000 |  2,559.0 μs |  25.62 μs |  23.97 μs |
|   Render | underground.1000 | 51,072.7 μs | 348.46 μs | 325.95 μs |
