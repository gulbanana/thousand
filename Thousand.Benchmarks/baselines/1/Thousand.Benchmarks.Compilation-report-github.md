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
|       **Batch** |  **connectors.1000** | **15.507 ms** | **0.0808 ms** | **0.0756 ms** |
| Interactive |  connectors.1000 | 15.459 ms | 0.0820 ms | 0.0767 ms |
|       **Batch** |      **tetris.1000** |  **7.929 ms** | **0.0456 ms** | **0.0427 ms** |
| Interactive |      tetris.1000 |  7.615 ms | 0.0215 ms | 0.0201 ms |
|       **Batch** | **underground.1000** | **22.556 ms** | **0.1055 ms** | **0.0987 ms** |
| Interactive | underground.1000 | 24.630 ms | 0.1320 ms | 0.1170 ms |
