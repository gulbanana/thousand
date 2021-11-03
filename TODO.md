TO DO
=====

* bug: using diamond as a container seems to be broken somehow
* bug: (line) anchors and offsets probably don't combine correctly
* bug: end-padding is broken when scaling - see nested-scaling.1000
* syntax: class->style maybe
* syntax: should alists use lineseps instead of commas? should they use : instead of =?
* syntax: path names (perhaps use [] for anon)
* syntax: class contents placeholder inserts - right now it's all prepended (but how would this work with a classlist?)
* syntax: import files?
* syntax: import templates (not the same as above! applies to the stdlib)
* syntax: templates within scopes
* feature: line labels (composition+, align vs justify, shorthand syntax)
* feature: object/label offset from position (parsed/styled)
* feature: drop shadows (object/line, though different)
* feature: customisable arrowheads
* feature: track-span (maybe)
* feature: -bottom/-top/etc for 4-way attributes
* feature: reverse flow - this would also start the grid at the far side of its containing box
* feature: gradients, certainly for fills and maybe strokes
* extension: download the language server automatically instead of requiring people to dotnet tool install --global Thousand.LSP
* extension: basic browser entry point.. maybe. it wouldn't do much without the language server
* extension: improve previewer - easier to turn it off, close/open when parent closes/opens, etc
* extension: preview *features* - export, zoom?
* extension: escaped strings don't tokenize properly
* language server: object name completions behaviour is not very good, due to syntax ambiguity
* language server: bad behaviour when typing the - of  line (and when handling bad tokens in general!)
* website: fix monaco errors failing to clear
* website: docs - generated for attrs? or just handwrite it all?
* website: web components lib (e.g. make it easy to do a lot of previews in the docs)
* website: attempt wasm. this does make font support an issue, and might require the latest skia, but it seems possible
* website: get some of the analysis features in - monaco supports a lot of it
* triangle text placement is bad visually. special case to move either the text or the shape?
* anchors should be close to each other instead of strictly clockwise
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text... alternatively, just rename min-width to width and caveat it
* consider line caps (butt etc) 
* regularity control (a square is a regular rectangle) (not so sure about this, it makes the code simpler but the api more complex...). otoh autoregularising shapes confuse me (diamond vs rhombus)
* do a defaults pass, picking values which produce a nice appearance without customisation - font-size should definitely be 16 probably
* clean up the samples, with said defaults
* image embedding!
* in theory, GenerationService needs queueing or other concurrency control mechanisms. now that it uses svg it's fast, but surely there are some race conditions
* reconsider name of space/gutter (css calls it gap)
* runtime colour names
* document all attributes