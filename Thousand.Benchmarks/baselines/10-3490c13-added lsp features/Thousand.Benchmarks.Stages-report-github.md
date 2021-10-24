``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |       Mean |    Error |   StdDev |
|----------- |----------------- |-----------:|---------:|---------:|
|   **Tokenize** |  **connectors.1000** | **1,328.2 μs** |  **2.28 μs** |  **2.14 μs** |
| Preprocess |  connectors.1000 | 4,469.2 μs | 27.72 μs | 25.93 μs |
|  Typecheck |  connectors.1000 |   448.2 μs |  1.73 μs |  1.61 μs |
|   Evaluate |  connectors.1000 |   122.3 μs |  2.38 μs |  2.54 μs |
|    Compose |  connectors.1000 | 1,182.2 μs |  8.98 μs |  8.40 μs |
|     Render |  connectors.1000 |   147.9 μs |  2.87 μs |  3.74 μs |
|   **Tokenize** |      **tetris.1000** |   **995.4 μs** | **19.39 μs** | **16.19 μs** |
| Preprocess |      tetris.1000 | 2,212.1 μs |  8.81 μs |  8.24 μs |
|  Typecheck |      tetris.1000 |   119.9 μs |  0.43 μs |  0.40 μs |
|   Evaluate |      tetris.1000 |   205.0 μs |  0.70 μs |  0.58 μs |
|    Compose |      tetris.1000 |   262.0 μs |  2.22 μs |  2.08 μs |
|     Render |      tetris.1000 |   158.8 μs |  0.45 μs |  0.39 μs |
|   **Tokenize** | **underground.1000** | **2,966.8 μs** | **11.58 μs** | **10.27 μs** |
| Preprocess | underground.1000 | 6,965.4 μs | 21.67 μs | 16.92 μs |
|  Typecheck | underground.1000 |   688.4 μs |  1.93 μs |  1.71 μs |
|   Evaluate | underground.1000 |   410.3 μs |  2.75 μs |  2.57 μs |
|    Compose | underground.1000 | 2,558.4 μs |  9.81 μs |  8.70 μs |
|     Render | underground.1000 |   511.6 μs |  2.28 μs |  1.90 μs |
