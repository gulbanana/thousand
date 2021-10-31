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
|       **Batch** |  **connectors.1000** |  **8.996 ms** | **0.1063 ms** | **0.0942 ms** |
| Interactive |  connectors.1000 |  9.568 ms | 0.0662 ms | 0.0619 ms |
|       **Batch** |      **tetris.1000** |  **3.879 ms** | **0.0245 ms** | **0.0229 ms** |
| Interactive |      tetris.1000 |  4.070 ms | 0.0608 ms | 0.0507 ms |
|       **Batch** | **underground.1000** | **14.611 ms** | **0.0946 ms** | **0.0885 ms** |
| Interactive | underground.1000 | 16.887 ms | 0.1409 ms | 0.1176 ms |
