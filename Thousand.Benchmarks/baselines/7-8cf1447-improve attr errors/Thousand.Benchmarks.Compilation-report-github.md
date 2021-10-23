``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
Frequency=14318180 Hz, Resolution=69.8413 ns, Timer=HPET
.NET SDK=6.0.100-preview.5.21302.13
  [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT


```
|      Method |            Input |      Mean |     Error |    StdDev |
|------------ |----------------- |----------:|----------:|----------:|
|       **Batch** |  **connectors.1000** | **13.273 ms** | **0.0727 ms** | **0.0680 ms** |
| Interactive |  connectors.1000 | 12.747 ms | 0.0329 ms | 0.0275 ms |
|       **Batch** |      **tetris.1000** |  **5.948 ms** | **0.0145 ms** | **0.0136 ms** |
| Interactive |      tetris.1000 |  5.169 ms | 0.0169 ms | 0.0150 ms |
|       **Batch** | **underground.1000** | **20.176 ms** | **0.0608 ms** | **0.0569 ms** |
| Interactive | underground.1000 | 19.455 ms | 0.0683 ms | 0.0639 ms |
