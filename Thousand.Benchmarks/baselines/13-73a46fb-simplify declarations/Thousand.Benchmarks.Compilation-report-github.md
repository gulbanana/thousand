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
|       **Batch** |  **connectors.1000** |  **8.724 ms** | **0.0625 ms** | **0.0554 ms** |
| Interactive |  connectors.1000 |  9.257 ms | 0.0721 ms | 0.0674 ms |
|       **Batch** |      **tetris.1000** |  **3.754 ms** | **0.0354 ms** | **0.0314 ms** |
| Interactive |      tetris.1000 |  3.865 ms | 0.0224 ms | 0.0209 ms |
|       **Batch** | **underground.1000** | **14.213 ms** | **0.0927 ms** | **0.0867 ms** |
| Interactive | underground.1000 | 16.712 ms | 0.2099 ms | 0.1964 ms |
