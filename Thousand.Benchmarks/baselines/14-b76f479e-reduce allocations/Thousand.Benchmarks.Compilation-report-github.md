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
|       **Batch** |  **connectors.1000** |  **7.720 ms** | **0.0227 ms** | **0.0212 ms** |
| Interactive |  connectors.1000 |  8.197 ms | 0.0277 ms | 0.0259 ms |
|       **Batch** |      **tetris.1000** |  **3.277 ms** | **0.0041 ms** | **0.0038 ms** |
| Interactive |      tetris.1000 |  3.434 ms | 0.0076 ms | 0.0068 ms |
|       **Batch** | **underground.1000** | **13.108 ms** | **0.0235 ms** | **0.0219 ms** |
| Interactive | underground.1000 | 14.962 ms | 0.0478 ms | 0.0399 ms |
