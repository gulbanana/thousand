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
|       **Batch** |  **connectors.1000** | **11.290 ms** | **0.1091 ms** | **0.1021 ms** |
| Interactive |  connectors.1000 | 10.631 ms | 0.0984 ms | 0.0920 ms |
|       **Batch** |      **tetris.1000** |  **5.736 ms** | **0.0707 ms** | **0.0661 ms** |
| Interactive |      tetris.1000 |  4.872 ms | 0.0270 ms | 0.0252 ms |
|       **Batch** | **underground.1000** | **17.010 ms** | **0.0485 ms** | **0.0405 ms** |
| Interactive | underground.1000 | 16.854 ms | 0.0760 ms | 0.0710 ms |
