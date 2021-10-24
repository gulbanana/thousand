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
|   **Tokenize** |  **connectors.1000** | **1,354.1 μs** | **15.85 μs** | **14.05 μs** |
| Preprocess |  connectors.1000 | 5,173.9 μs | 77.00 μs | 72.02 μs |
|  Typecheck |  connectors.1000 |   452.5 μs |  2.43 μs |  2.27 μs |
|   Evaluate |  connectors.1000 |   135.8 μs |  1.34 μs |  1.25 μs |
|    Compose |  connectors.1000 | 1,232.2 μs | 23.99 μs | 22.44 μs |
|     Render |  connectors.1000 |   146.9 μs |  1.80 μs |  1.69 μs |
|   **Tokenize** |      **tetris.1000** |   **979.2 μs** |  **3.95 μs** |  **3.69 μs** |
| Preprocess |      tetris.1000 | 2,466.3 μs | 13.29 μs | 12.44 μs |
|  Typecheck |      tetris.1000 |   111.1 μs |  0.51 μs |  0.47 μs |
|   Evaluate |      tetris.1000 |   223.6 μs |  1.63 μs |  1.36 μs |
|    Compose |      tetris.1000 |   280.6 μs |  1.79 μs |  1.59 μs |
|     Render |      tetris.1000 |   166.7 μs |  1.38 μs |  1.29 μs |
|   **Tokenize** | **underground.1000** | **3,008.3 μs** | **12.96 μs** | **11.49 μs** |
| Preprocess | underground.1000 | 7,760.3 μs | 87.57 μs | 81.91 μs |
|  Typecheck | underground.1000 |   666.4 μs |  3.74 μs |  3.50 μs |
|   Evaluate | underground.1000 |   420.1 μs |  2.30 μs |  2.04 μs |
|    Compose | underground.1000 | 2,621.8 μs | 51.10 μs | 52.47 μs |
|     Render | underground.1000 |   531.1 μs |  5.60 μs |  5.24 μs |
