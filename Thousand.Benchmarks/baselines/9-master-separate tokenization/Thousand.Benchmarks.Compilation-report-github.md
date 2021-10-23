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
|       **Batch** |  **connectors.1000** |  **9.072 ms** | **0.1379 ms** | **0.1290 ms** |
| Interactive |  connectors.1000 |  8.783 ms | 0.1627 ms | 0.1522 ms |
|       **Batch** |      **tetris.1000** |  **4.905 ms** | **0.0117 ms** | **0.0104 ms** |
| Interactive |      tetris.1000 |  4.453 ms | 0.0731 ms | 0.0684 ms |
|       **Batch** | **underground.1000** | **15.630 ms** | **0.0376 ms** | **0.0333 ms** |
| Interactive | underground.1000 | 16.133 ms | 0.2029 ms | 0.1798 ms |
