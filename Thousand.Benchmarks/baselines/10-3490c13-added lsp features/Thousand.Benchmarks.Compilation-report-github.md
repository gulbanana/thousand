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
|       **Batch** |  **connectors.1000** |  **8.543 ms** | **0.0491 ms** | **0.0436 ms** |
| Interactive |  connectors.1000 |  8.617 ms | 0.0555 ms | 0.0519 ms |
|       **Batch** |      **tetris.1000** |  **4.503 ms** | **0.0253 ms** | **0.0211 ms** |
| Interactive |      tetris.1000 |  4.357 ms | 0.0173 ms | 0.0153 ms |
|       **Batch** | **underground.1000** | **14.583 ms** | **0.0826 ms** | **0.0732 ms** |
| Interactive | underground.1000 | 16.546 ms | 0.1861 ms | 0.1741 ms |
