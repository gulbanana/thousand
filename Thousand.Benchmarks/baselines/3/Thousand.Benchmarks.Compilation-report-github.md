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
|       **Batch** |  **connectors.1000** | **17.008 ms** | **0.1293 ms** | **0.1080 ms** |
| Interactive |  connectors.1000 | 15.848 ms | 0.0826 ms | 0.0773 ms |
|       **Batch** |      **tetris.1000** |  **9.081 ms** | **0.1253 ms** | **0.1172 ms** |
| Interactive |      tetris.1000 |  7.803 ms | 0.1519 ms | 0.1420 ms |
|       **Batch** | **underground.1000** | **25.326 ms** | **0.0672 ms** | **0.0561 ms** |
| Interactive | underground.1000 | 25.482 ms | 0.4836 ms | 0.5939 ms |
