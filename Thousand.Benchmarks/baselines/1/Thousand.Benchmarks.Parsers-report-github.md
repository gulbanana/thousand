``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|   Method |            Input |       Mean |    Error |   StdDev |
|--------- |----------------- |-----------:|---------:|---------:|
|  **Untyped** |  **connectors.1000** |   **972.5 μs** |  **8.41 μs** |  **7.86 μs** |
| Tolerant |  connectors.1000 |   992.1 μs |  7.63 μs |  7.13 μs |
|  **Untyped** |      **tetris.1000** |   **808.3 μs** |  **2.68 μs** |  **2.37 μs** |
| Tolerant |      tetris.1000 |   807.8 μs |  5.11 μs |  4.78 μs |
|  **Untyped** | **underground.1000** | **2,177.4 μs** | **16.14 μs** | **15.10 μs** |
| Tolerant | underground.1000 | 2,220.0 μs | 14.36 μs | 13.44 μs |
