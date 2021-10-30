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
|       **Batch** |  **connectors.1000** | **10.741 ms** | **0.0417 ms** | **0.0390 ms** |
| Interactive |  connectors.1000 | 11.350 ms | 0.1425 ms | 0.1263 ms |
|       **Batch** |      **tetris.1000** |  **4.758 ms** | **0.0652 ms** | **0.0610 ms** |
| Interactive |      tetris.1000 |  4.808 ms | 0.0256 ms | 0.0240 ms |
|       **Batch** | **underground.1000** | **18.538 ms** | **0.1296 ms** | **0.1212 ms** |
| Interactive | underground.1000 | 20.768 ms | 0.3242 ms | 0.3032 ms |
