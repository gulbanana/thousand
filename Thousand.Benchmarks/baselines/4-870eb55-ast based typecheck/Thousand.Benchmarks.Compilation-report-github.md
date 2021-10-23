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
|       **Batch** |  **connectors.1000** | **15.392 ms** | **0.2675 ms** | **0.2502 ms** |
| Interactive |  connectors.1000 | 14.214 ms | 0.2838 ms | 0.2654 ms |
|       **Batch** |      **tetris.1000** |  **7.513 ms** | **0.0179 ms** | **0.0159 ms** |
| Interactive |      tetris.1000 |  6.571 ms | 0.0192 ms | 0.0170 ms |
|       **Batch** | **underground.1000** | **22.227 ms** | **0.0891 ms** | **0.0834 ms** |
| Interactive | underground.1000 | 21.953 ms | 0.0964 ms | 0.0902 ms |
