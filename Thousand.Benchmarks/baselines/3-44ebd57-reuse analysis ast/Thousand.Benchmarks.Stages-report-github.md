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
| **Preprocess** |  **connectors.1000** |  **9,235.7 μs** | **115.15 μs** | **107.71 μs** |
|  Typecheck |  connectors.1000 |  4,374.9 μs |  51.26 μs |  47.95 μs |
|   Evaluate |  connectors.1000 |    125.1 μs |   0.78 μs |   0.73 μs |
|    Compose |  connectors.1000 |  1,212.6 μs |  23.40 μs |  26.95 μs |
|     Render |  connectors.1000 |    149.3 μs |   2.87 μs |   2.69 μs |
| **Preprocess** |      **tetris.1000** |  **5,273.5 μs** |   **5.20 μs** |   **4.86 μs** |
|  Typecheck |      tetris.1000 |  1,708.5 μs |   2.86 μs |   2.67 μs |
|   Evaluate |      tetris.1000 |    204.5 μs |   1.03 μs |   0.97 μs |
|    Compose |      tetris.1000 |    266.4 μs |   1.18 μs |   1.05 μs |
|     Render |      tetris.1000 |    162.7 μs |   0.27 μs |   0.24 μs |
| **Preprocess** | **underground.1000** | **15,473.6 μs** |  **27.19 μs** |  **25.43 μs** |
|  Typecheck | underground.1000 |  4,828.3 μs |  10.08 μs |   9.43 μs |
|   Evaluate | underground.1000 |    407.9 μs |   2.25 μs |   2.11 μs |
|    Compose | underground.1000 |  2,496.7 μs |  12.08 μs |  10.71 μs |
|     Render | underground.1000 |    508.4 μs |   1.64 μs |   1.53 μs |
