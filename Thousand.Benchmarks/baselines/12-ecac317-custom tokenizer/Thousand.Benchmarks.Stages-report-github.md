``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |       Mean |     Error |   StdDev |
|----------- |----------------- |-----------:|----------:|---------:|
|   **Tokenize** |  **connectors.1000** |   **384.1 μs** |   **1.54 μs** |  **1.44 μs** |
| Preprocess |  connectors.1000 | 5,765.1 μs |  68.36 μs | 63.94 μs |
|  Typecheck |  connectors.1000 |   512.1 μs |   2.00 μs |  1.87 μs |
|   Evaluate |  connectors.1000 |   122.1 μs |   0.32 μs |  0.28 μs |
|    Compose |  connectors.1000 | 1,214.8 μs |   7.69 μs |  6.82 μs |
|     Render |  connectors.1000 |   150.8 μs |   0.91 μs |  0.85 μs |
|   **Tokenize** |      **tetris.1000** |   **300.4 μs** |   **0.54 μs** |  **0.50 μs** |
| Preprocess |      tetris.1000 | 2,127.6 μs |   4.64 μs |  3.87 μs |
|  Typecheck |      tetris.1000 |   136.7 μs |   0.30 μs |  0.25 μs |
|   Evaluate |      tetris.1000 |   205.0 μs |   0.68 μs |  0.63 μs |
|    Compose |      tetris.1000 |   323.0 μs |   1.23 μs |  1.09 μs |
|     Render |      tetris.1000 |   165.1 μs |   0.96 μs |  0.90 μs |
|   **Tokenize** | **underground.1000** |   **808.5 μs** |   **3.51 μs** |  **3.29 μs** |
| Preprocess | underground.1000 | 8,886.4 μs | 103.48 μs | 96.79 μs |
|  Typecheck | underground.1000 |   768.8 μs |   2.47 μs |  2.31 μs |
|   Evaluate | underground.1000 |   399.6 μs |   1.81 μs |  1.69 μs |
|    Compose | underground.1000 | 2,703.2 μs |  51.23 μs | 52.61 μs |
|     Render | underground.1000 |   532.8 μs |   3.77 μs |  3.15 μs |
