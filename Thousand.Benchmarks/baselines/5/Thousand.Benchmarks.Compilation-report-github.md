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
|       **Batch** |  **connectors.1000** | **13.184 ms** | **0.1040 ms** | **0.0973 ms** |
| Interactive |  connectors.1000 | 12.561 ms | 0.1097 ms | 0.1027 ms |
|       **Batch** |      **tetris.1000** |  **6.721 ms** | **0.0428 ms** | **0.0400 ms** |
| Interactive |      tetris.1000 |  5.875 ms | 0.0600 ms | 0.0561 ms |
|       **Batch** | **underground.1000** | **19.949 ms** | **0.2790 ms** | **0.2609 ms** |
| Interactive | underground.1000 | 19.511 ms | 0.1230 ms | 0.1151 ms |
