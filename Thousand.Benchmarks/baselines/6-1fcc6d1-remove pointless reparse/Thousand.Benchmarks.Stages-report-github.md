``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |        Mean |     Error |    StdDev |
|----------- |----------------- |------------:|----------:|----------:|
| **Preprocess** |  **connectors.1000** |  **6,371.0 μs** |  **24.06 μs** |  **20.09 μs** |
|  Typecheck |  connectors.1000 |  2,365.1 μs |  25.48 μs |  23.84 μs |
|   Evaluate |  connectors.1000 |    127.5 μs |   0.75 μs |   0.70 μs |
|    Compose |  connectors.1000 |  1,169.2 μs |   6.33 μs |   5.92 μs |
|     Render |  connectors.1000 |    141.8 μs |   0.40 μs |   0.34 μs |
| **Preprocess** |      **tetris.1000** |  **3,512.9 μs** |  **17.22 μs** |  **15.27 μs** |
|  Typecheck |      tetris.1000 |    625.2 μs |   1.46 μs |   1.22 μs |
|   Evaluate |      tetris.1000 |    226.8 μs |   1.13 μs |   0.94 μs |
|    Compose |      tetris.1000 |    264.9 μs |   1.10 μs |   1.03 μs |
|     Render |      tetris.1000 |    158.1 μs |   0.42 μs |   0.40 μs |
| **Preprocess** | **underground.1000** | **10,715.8 μs** | **130.10 μs** | **121.69 μs** |
|  Typecheck | underground.1000 |  2,094.0 μs |   6.73 μs |   6.29 μs |
|   Evaluate | underground.1000 |    419.4 μs |   1.20 μs |   1.00 μs |
|    Compose | underground.1000 |  2,546.6 μs |  10.10 μs |   8.43 μs |
|     Render | underground.1000 |    513.4 μs |   1.71 μs |   1.52 μs |
