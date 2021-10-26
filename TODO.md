TO DO
=====

* bug: fix LSP getting stuck (content modified)
* bug: use fill-opacity for SVG transparency (https://gitlab.com/inkscape/inbox/-/issues/1195)
* bug: using diamond as a container seems to be broken somehow
* bug: (line) anchors and offsets probably don't combine correctly
* syntax: possible big redo with class->style, possible scope changes - alists should be line scopes; clists should be available for lines and *maybe* the only form. = is : and : is basedon, etc
* syntax: path names (perhaps use [] for anon)
* syntax: class contents placeholder inserts - right now it's all prepended (but how would this work with a classlist?)
* syntax: import files?
* syntax: import templates (not the same as above! applies to the stdlib)
* feature: line labels (+ add font to line, + align vs justify)
* feature: object offset from position 
* feature: drop shadows (object/line, though different)
* feature: customisable arrowheads
* feature: track-span (maybe)
* feature: -bottom/-top/etc for 4-way attributes
* feature: reverse flow - this would also start the grid at the far side of its containing box
* feature: *parent* flow - instead of defaulting to row, default to "row unless i contain rows"
* extension: download the language server automatically instead of requiring people to dotnet tool install --global Thousand.LSP
* extension: basic browser entry point.. maybe. it wouldn't do much without the language server
* website: fix monaco errors failing to clear
* website: docs - generated for attrs? or just handwrite it all?
* website: web components lib (e.g. make it easy to do a lot of previews in the docs)
* website: attempt wasm. this does make font support an issue, and might require the latest skia, but it seems possible
* website: get some of the analysis features in - monaco supports a lot of it
* language server: inline doc (hovers for attribute keys?)
* language server: completion features 
* language server: definition/declaration features
* language server: outline provider
* triangle text placement is bad visually. special case to move either the text or the shape?
* anchors should be close to each other instead of strictly clockwise
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text... alternatively, just rename min-width to width and caveat it
* reevaluate padding algorithm - is it correct to ignore padding when there is no (unanchored) content? we could disable padding for shape=none instead. the current situation may be ok, i just need to think about it
* consider line caps (butt etc) 
* regularity control (a square is a regular rectangle) (not so sure about this, it makes the code simpler but the api more complex...). otoh autoregularising shapes confuse me (diamond vs rhombus)
* do a defaults pass, picking values which produce a nice appearance without customisation
* image embedding!
* in theory, GenerationService needs queueing or other concurrency control mechanisms. now that it uses svg it's fast, but surely there are some race conditions
* reconsider space/gutter/gap
* runtime colour names
* position -> shared -> entity?
