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
|       **Batch** |  **connectors.1000** |  **8.916 ms** | **0.0189 ms** | **0.0177 ms** |
| Interactive |  connectors.1000 |  8.534 ms | 0.0315 ms | 0.0279 ms |
|       **Batch** |      **tetris.1000** |  **4.807 ms** | **0.0146 ms** | **0.0137 ms** |
| Interactive |      tetris.1000 |  4.307 ms | 0.0287 ms | 0.0255 ms |
|       **Batch** | **underground.1000** | **15.452 ms** | **0.1596 ms** | **0.1415 ms** |
| Interactive | underground.1000 | 15.443 ms | 0.0486 ms | 0.0406 ms |
