``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|     Method |            Input |        Mean |    Error |   StdDev |
|----------- |----------------- |------------:|---------:|---------:|
| **Preprocess** |  **connectors.1000** |  **7,823.3 μs** | **31.12 μs** | **25.99 μs** |
|  Typecheck |  connectors.1000 |  2,341.4 μs | 19.68 μs | 18.41 μs |
|   Evaluate |  connectors.1000 |    131.3 μs |  0.63 μs |  0.56 μs |
|    Compose |  connectors.1000 |  1,168.2 μs |  6.96 μs |  6.17 μs |
|     Render |  connectors.1000 |    143.3 μs |  0.46 μs |  0.43 μs |
| **Preprocess** |      **tetris.1000** |  **4,286.3 μs** | **13.28 μs** | **11.77 μs** |
|  Typecheck |      tetris.1000 |    617.6 μs |  1.66 μs |  1.56 μs |
|   Evaluate |      tetris.1000 |    218.7 μs |  0.42 μs |  0.35 μs |
|    Compose |      tetris.1000 |    266.6 μs |  2.50 μs |  2.22 μs |
|     Render |      tetris.1000 |    161.3 μs |  0.83 μs |  0.73 μs |
| **Preprocess** | **underground.1000** | **13,257.1 μs** | **68.38 μs** | **63.97 μs** |
|  Typecheck | underground.1000 |  2,158.4 μs | 28.88 μs | 27.01 μs |
|   Evaluate | underground.1000 |    424.4 μs |  0.58 μs |  0.51 μs |
|    Compose | underground.1000 |  2,542.0 μs | 21.44 μs | 17.91 μs |
|     Render | underground.1000 |    494.6 μs |  1.95 μs |  1.72 μs |
