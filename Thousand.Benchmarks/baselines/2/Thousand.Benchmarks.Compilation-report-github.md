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
|       **Batch** |  **connectors.1000** | **15.296 ms** | **0.0671 ms** | **0.0594 ms** |
| Interactive |  connectors.1000 | 15.343 ms | 0.0586 ms | 0.0548 ms |
|       **Batch** |      **tetris.1000** |  **8.057 ms** | **0.0635 ms** | **0.0594 ms** |
| Interactive |      tetris.1000 |  7.756 ms | 0.0319 ms | 0.0283 ms |
|       **Batch** | **underground.1000** | **23.216 ms** | **0.3638 ms** | **0.3225 ms** |
| Interactive | underground.1000 | 25.206 ms | 0.0661 ms | 0.0618 ms |
